var WebSocketPlugin = {
    $sockets: {},
    $nextId: { value: 1 },
    $messageQueue: {},

    WebSocket_Create: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        var id = nextId.value++;
        messageQueue[id] = [];
        try {
            var ws = new WebSocket(url);
            ws.onopen = function() {
                console.log("[WS] Connected: " + id);
            };
            ws.onmessage = function(evt) {
                if (messageQueue[id]) {
                    messageQueue[id].push(evt.data);
                }
            };
            ws.onerror = function(evt) {
                console.warn("[WS] Error on socket " + id + " (will use REST fallback)");
            };
            ws.onclose = function(evt) {
                console.log("[WS] Closed: " + id + " code=" + evt.code);
            };
            sockets[id] = ws;
        } catch (e) {
            console.error("[WS] Create failed: " + e.message);
            return -1;
        }
        return id;
    },

    WebSocket_GetState: function(id) {
        var ws = sockets[id];
        if (!ws) return 3; // CLOSED
        return ws.readyState;
    },

    WebSocket_Send: function(id, msgPtr) {
        var ws = sockets[id];
        if (!ws || ws.readyState !== 1) return;
        var msg = UTF8ToString(msgPtr);
        ws.send(msg);
    },

    WebSocket_Receive: function(id, bufferPtr, bufferSize) {
        var queue = messageQueue[id];
        if (!queue || queue.length === 0) return 0;
        var msg = queue.shift();
        var bytes = lengthBytesUTF8(msg) + 1;
        if (bytes > bufferSize) {
            // Message too large, put it back
            queue.unshift(msg);
            return -bytes;
        }
        stringToUTF8(msg, bufferPtr, bufferSize);
        return bytes - 1; // return length without null terminator
    },

    WebSocket_Close: function(id) {
        var ws = sockets[id];
        if (ws) {
            ws.close();
            delete sockets[id];
            delete messageQueue[id];
        }
    }
};

autoAddDeps(WebSocketPlugin, '$sockets');
autoAddDeps(WebSocketPlugin, '$nextId');
autoAddDeps(WebSocketPlugin, '$messageQueue');
mergeInto(LibraryManager.library, WebSocketPlugin);
