#!/usr/bin/env sh
set -eu

export DISPLAY=:99

Xvfb "$DISPLAY" \
    -screen 0 1280x1024x24 \
    -nolisten tcp \
    > /tmp/xvfb.log 2>&1 &
xvfb_pid=$!

cleanup() {
    kill "$xvfb_pid" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

sleep 1

if ! kill -0 "$xvfb_pid" 2>/dev/null; then
    cat /tmp/xvfb.log >&2
    exit 1
fi

exec "$@"
