#!/usr/bin/env python3
"""Serves a Unity WebGL build with correct MIME types, Content-Encoding for
.gz files, and reverse-proxies /api and /ws to backend services."""

import http.server
import os
import select
import socket as _socket
import sys
import urllib.request
import urllib.error
from urllib.parse import urlparse

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8090
DIRECTORY = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Builds", "WebGL")

API_UPSTREAM = os.environ.get("API_UPSTREAM", "http://localhost:3030")
WS_UPSTREAM = os.environ.get("WS_UPSTREAM", "http://localhost:3032")


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    # ── Reverse proxy for /api/ and /ws ──────────────────────────────────

    def _is_proxy_path(self):
        return self.path.startswith("/api/") or self.path == "/api"

    def _is_ws_upgrade(self):
        return (
            (self.path == "/ws" or self.path.startswith("/ws?"))
            and self.headers.get("Upgrade", "").lower() == "websocket"
        )

    def _proxy_websocket(self):
        """Relay a WebSocket upgrade to the broadcast service via raw TCP."""
        parsed = urlparse(WS_UPSTREAM)
        upstream_host = parsed.hostname or "localhost"
        upstream_port = parsed.port or 3032

        upstream = _socket.socket(_socket.AF_INET, _socket.SOCK_STREAM)
        try:
            upstream.connect((upstream_host, upstream_port))
        except Exception as e:
            self.send_response(502)
            self.send_header("Content-Type", "text/plain")
            self.end_headers()
            self.wfile.write(f"WebSocket upstream unavailable: {e}".encode())
            return

        # Reconstruct the HTTP upgrade request for the upstream
        raw = f"GET /ws HTTP/1.1\r\n"
        for key, val in self.headers.items():
            if key.lower() == "host":
                raw += f"Host: {upstream_host}:{upstream_port}\r\n"
            else:
                raw += f"{key}: {val}\r\n"
        raw += "\r\n"
        upstream.sendall(raw.encode())

        # Relay bytes between client and upstream until one side closes
        client = self.request
        client.setblocking(False)
        upstream.setblocking(False)
        try:
            while True:
                rlist, _, xlist = select.select(
                    [client, upstream], [], [client, upstream], 60
                )
                if xlist:
                    break
                if not rlist:  # select timeout
                    continue
                for s in rlist:
                    other = upstream if s is client else client
                    data = s.recv(65536)
                    if not data:
                        return
                    other.sendall(data)
        except Exception:
            pass
        finally:
            upstream.close()
            self.close_connection = True

    def _proxy(self):
        # Strip /api prefix and forward to upstream
        upstream_path = self.path[len("/api"):]  # e.g. "/table/1"
        if not upstream_path:
            upstream_path = "/"
        url = f"{API_UPSTREAM}{upstream_path}"

        # Read request body if present
        content_length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_length) if content_length > 0 else None

        req = urllib.request.Request(url, data=body, method=self.command)
        # Forward content-type
        if self.headers.get("Content-Type"):
            req.add_header("Content-Type", self.headers["Content-Type"])

        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                resp_body = resp.read()
                self.send_response(resp.status)
                for key, val in resp.getheaders():
                    if key.lower() not in ("transfer-encoding", "connection"):
                        self.send_header(key, val)
                self.send_header("Access-Control-Allow-Origin", "*")
                self.end_headers()
                self.wfile.write(resp_body)
        except urllib.error.HTTPError as e:
            resp_body = e.read()
            self.send_response(e.code)
            self.send_header("Content-Type", "application/json")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(resp_body)
        except urllib.error.URLError as e:
            self.send_response(502)
            self.send_header("Content-Type", "text/plain")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(f"Upstream unavailable: {e.reason}".encode())

    def do_GET(self):
        if self._is_ws_upgrade():
            self._proxy_websocket()
        elif self._is_proxy_path():
            self._proxy()
        else:
            super().do_GET()

    def do_POST(self):
        if self._is_proxy_path():
            self._proxy()
        else:
            self.send_response(405)
            self.end_headers()

    def do_OPTIONS(self):
        self.send_response(204)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()

    # ── Static file serving with Unity WebGL headers ─────────────────────

    def end_headers(self):
        path = self.translate_path(self.path)
        if path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        # CORS for local dev
        self.send_header("Access-Control-Allow-Origin", "*")
        # Disable caching for local dev
        self.send_header("Cache-Control", "no-cache, no-store, must-revalidate")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm") or path.endswith(".wasm.gz"):
            return "application/wasm"
        if path.endswith(".js") or path.endswith(".js.gz"):
            return "application/javascript"
        if path.endswith(".data") or path.endswith(".data.gz"):
            return "application/octet-stream"
        return super().guess_type(path)


print(f"Serving Unity WebGL from {DIRECTORY} on http://localhost:{PORT}")
print(f"  API proxy: /api/* -> {API_UPSTREAM}")
print(f"  WS upstream: {WS_UPSTREAM} (connect via ws://localhost:{PORT}/ws)")
http.server.ThreadingHTTPServer(("", PORT), UnityWebGLHandler).serve_forever()
