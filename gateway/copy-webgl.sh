#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WEBGL_SRC="${SCRIPT_DIR}/../unity-client/Builds/WebGL"
WEBGL_DST="${SCRIPT_DIR}/webgl"

if [ ! -d "$WEBGL_SRC" ]; then
    echo "Error: WebGL build not found at $WEBGL_SRC"
    echo "Run 'cd unity-client && make build-webgl' first."
    exit 1
fi

rm -rf "$WEBGL_DST"
cp -r "$WEBGL_SRC" "$WEBGL_DST"

echo "Copied WebGL build to $WEBGL_DST"
ls -la "$WEBGL_DST"
