#!/usr/bin/env node

/**
 * Simulate poker hands by sending SQS messages to the holdem-processor queue.
 *
 * This script sends fake "process this table" messages to the SQS queue,
 * which the holdem-processor Lambda consumes. Each message triggers the
 * processor to advance the hand state machine by one step.
 *
 * Run after `docker compose --profile engine up`.
 *
 * Usage:
 *   node scripts/simulate-hands.js              # Send 16 messages (full hand)
 *   node scripts/simulate-hands.js --count 5    # Send 5 messages
 *   node scripts/simulate-hands.js --table 2    # Process table 2
 *   node scripts/simulate-hands.js --loop       # Continuously send messages
 */

'use strict';

const { SQSClient, SendMessageCommand } = require('@aws-sdk/client-sqs');

const SQS_ENDPOINT = process.env.SQS_ENDPOINT || 'http://localhost:9324';
const QUEUE_URL = process.env.SQS_HOLDEM_QUEUE_URL || 'http://localhost:9324/000000000000/holdem-processor-queue';

const client = new SQSClient({
  region: 'us-east-1',
  endpoint: SQS_ENDPOINT,
  credentials: { accessKeyId: 'local', secretAccessKey: 'local' },
});

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function sendMessage(tableId) {
  const body = JSON.stringify({
    tableId,
    gameType: 'texas',
    timestamp: Date.now(),
  });

  try {
    const result = await client.send(
      new SendMessageCommand({
        QueueUrl: QUEUE_URL,
        MessageBody: body,
      })
    );
    console.log(`Sent message for table ${tableId} — MessageId: ${result.MessageId}`);
    return result;
  } catch (err) {
    console.error(`Failed to send message: ${err.message}`);
    throw err;
  }
}

async function main() {
  const args = process.argv.slice(2);
  const tableId = parseInt(getArg(args, '--table') || '1', 10);
  const count = parseInt(getArg(args, '--count') || '16', 10);
  const loop = args.includes('--loop');
  const delay = parseInt(getArg(args, '--delay') || '1000', 10);

  console.log(`Simulating hands for table ${tableId}`);
  console.log(`SQS endpoint: ${SQS_ENDPOINT}`);
  console.log(`Queue URL: ${QUEUE_URL}`);
  console.log('');

  if (loop) {
    console.log('Loop mode — sending messages every second. Ctrl+C to stop.\n');
    let i = 0;
    while (true) {
      await sendMessage(tableId);
      i++;
      if (i % 16 === 0) {
        console.log(`\n--- Hand ${i / 16} complete. Starting next hand... ---\n`);
      }
      await sleep(delay);
    }
  } else {
    console.log(`Sending ${count} messages (one per hand step)...\n`);
    for (let i = 0; i < count; i++) {
      await sendMessage(tableId);
      await sleep(delay);
    }
    console.log(`\nDone! Sent ${count} messages.`);
  }
}

function getArg(args, flag) {
  const idx = args.indexOf(flag);
  return idx !== -1 && idx + 1 < args.length ? args[idx + 1] : null;
}

main().catch((err) => {
  console.error('Simulation failed:', err.message);
  process.exit(1);
});
