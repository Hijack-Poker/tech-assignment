import { useEffect, useRef } from 'react';

/**
 * Calls `callback` immediately and then every `intervalMs` milliseconds.
 * Cleans up on unmount or when dependencies change.
 * Pass `enabled = false` to pause polling.
 */
export function usePolling(callback: () => void, intervalMs: number, enabled = true) {
  const savedCallback = useRef(callback);

  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  useEffect(() => {
    if (!enabled) return;

    savedCallback.current();
    const id = setInterval(() => savedCallback.current(), intervalMs);
    return () => clearInterval(id);
  }, [intervalMs, enabled]);
}
