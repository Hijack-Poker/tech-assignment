'use strict';

const http = require('http');
const { WebSocketServer } = require('ws');

const PORT = process.env.PORT || 3032;
const PROCESSOR_URL = process.env.PROCESSOR_URL || 'http://localhost:3030';

// In-memory subscriber tracking: tableId -> Set<ws>
const tableSubscribers = new Map();

// ─── HTTP Server ─────────────────────────────────────────────────────

function handleRequest(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'GET' && req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      service: 'cash-game-broadcast',
      status: 'ok',
      timestamp: new Date().toISOString(),
    }));
    return;
  }

  if (req.method === 'POST' && req.url === '/broadcast') {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
      handleBroadcast(body, res);
    });
    return;
  }

  res.writeHead(404);
  res.end('Not found');
}

async function handleBroadcast(body, res) {
  try {
    const event = JSON.parse(body);
    const detail = event.detail || event;
    const { tableId } = detail;

    if (!tableId) {
      res.writeHead(400, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: 'Missing tableId' }));
      return;
    }

    const subscribers = tableSubscribers.get(String(tableId));
    if (!subscribers || subscribers.size === 0) {
      console.log(`[Broadcast] No subscribers for table ${tableId}`);
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ message: 'No subscribers', tableId }));
      return;
    }

    // Fetch full table state from holdem-processor
    const tableState = await fetchTableState(tableId);
    if (!tableState) {
      console.log(`[Broadcast] Could not fetch table state for ${tableId}`);
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ message: 'Could not fetch table state', tableId }));
      return;
    }

    // Push to all subscribers
    const payload = JSON.stringify(tableState);
    let sentCount = 0;
    const stale = [];

    for (const ws of subscribers) {
      if (ws.readyState === 1) { // WebSocket.OPEN
        ws.send(payload);
        sentCount++;
      } else {
        stale.push(ws);
      }
    }

    // Clean up stale connections
    for (const ws of stale) {
      subscribers.delete(ws);
    }

    console.log(`[Broadcast] Sent table ${tableId} state to ${sentCount} clients`);
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ message: `Broadcast to ${sentCount}`, tableId }));
  } catch (err) {
    console.error('[Broadcast] Error:', err.message);
    res.writeHead(500, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: err.message }));
  }
}

async function fetchTableState(tableId) {
  try {
    const url = `${PROCESSOR_URL}/table/${tableId}`;
    const resp = await fetch(url);
    if (!resp.ok) {
      console.error(`[Fetch] ${url} returned ${resp.status}`);
      return null;
    }
    return await resp.json();
  } catch (err) {
    console.error(`[Fetch] Failed to get table ${tableId}: ${err.message}`);
    return null;
  }
}

// ─── WebSocket Server ────────────────────────────────────────────────

const server = http.createServer(handleRequest);
const wss = new WebSocketServer({ server });

wss.on('connection', (ws) => {
  console.log('[WS] Client connected');

  ws.on('message', (data) => {
    try {
      const msg = JSON.parse(data.toString());

      if (msg.action === 'subscribe' && msg.tableId != null) {
        const tableId = String(msg.tableId);

        // Remove from any previous subscription
        if (ws._subscribedTable) {
          const prev = tableSubscribers.get(ws._subscribedTable);
          if (prev) prev.delete(ws);
        }

        // Add to new subscription
        if (!tableSubscribers.has(tableId)) {
          tableSubscribers.set(tableId, new Set());
        }
        tableSubscribers.get(tableId).add(ws);
        ws._subscribedTable = tableId;

        console.log(`[WS] Client subscribed to table ${tableId} (${tableSubscribers.get(tableId).size} total)`);
        ws.send(JSON.stringify({ type: 'subscribed', tableId }));
      }
    } catch (err) {
      console.error('[WS] Bad message:', err.message);
    }
  });

  ws.on('close', () => {
    if (ws._subscribedTable) {
      const subs = tableSubscribers.get(ws._subscribedTable);
      if (subs) {
        subs.delete(ws);
        console.log(`[WS] Client disconnected from table ${ws._subscribedTable} (${subs.size} remaining)`);
      }
    }
  });
});

// ─── Start ───────────────────────────────────────────────────────────

server.listen(PORT, '0.0.0.0', () => {
  console.log(`[cash-game-broadcast] HTTP + WebSocket server on :${PORT}`);
  console.log(`[cash-game-broadcast] Processor URL: ${PROCESSOR_URL}`);
});
