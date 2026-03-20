const http = require("http");
const BROADCAST_URL = process.env.BROADCAST_URL || "http://cash-game-broadcast.railway.internal:3032/broadcast";
const PORT = process.env.PORT || 4010;

const server = http.createServer((req, res) => {
  let body = "";
  req.on("data", chunk => body += chunk);
  req.on("end", () => {
    try {
      const event = JSON.parse(body);
      console.log("[EventBridge Mock] Event received:", event.Entries?.[0]?.DetailType || "unknown");
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ FailedEntryCount: 0, Entries: [{ EventId: "mock-" + Date.now() }] }));
      // Forward TABLE_UPDATE events to broadcast service
      for (const entry of (event.Entries || [])) {
        if (entry.DetailType === "TABLE_UPDATE" && entry.Detail) {
          const detail = typeof entry.Detail === "string" ? entry.Detail : JSON.stringify(entry.Detail);
          const postData = JSON.stringify({ detail: JSON.parse(detail) });
          const url = new URL(BROADCAST_URL);
          const fwdReq = http.request({
            hostname: url.hostname, port: url.port, path: url.pathname,
            method: "POST", headers: { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(postData) }
          }, (fwdRes) => {
            console.log("[EventBridge Mock] Forwarded to broadcast:", fwdRes.statusCode);
          });
          fwdReq.on("error", (e) => console.error("[EventBridge Mock] Forward error:", e.message));
          fwdReq.write(postData);
          fwdReq.end();
        }
      }
    } catch (e) {
      res.writeHead(400);
      res.end(JSON.stringify({ error: "Invalid JSON" }));
    }
  });
});
server.listen(PORT, "0.0.0.0", () => console.log(`[EventBridge Mock] Listening on :${PORT}`));
