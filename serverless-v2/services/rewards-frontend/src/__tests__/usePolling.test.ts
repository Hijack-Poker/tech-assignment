import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePolling } from '../hooks/usePolling';

describe('usePolling', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('calls the callback immediately on mount', () => {
    const cb = vi.fn();
    renderHook(() => usePolling(cb, 5000));
    expect(cb).toHaveBeenCalledTimes(1);
  });

  it('calls the callback at each interval', () => {
    const cb = vi.fn();
    renderHook(() => usePolling(cb, 1000));
    expect(cb).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1000);
    expect(cb).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(1000);
    expect(cb).toHaveBeenCalledTimes(3);
  });

  it('does not call when disabled', () => {
    const cb = vi.fn();
    renderHook(() => usePolling(cb, 1000, false));
    expect(cb).not.toHaveBeenCalled();

    vi.advanceTimersByTime(5000);
    expect(cb).not.toHaveBeenCalled();
  });

  it('cleans up the interval on unmount', () => {
    const cb = vi.fn();
    const { unmount } = renderHook(() => usePolling(cb, 1000));
    expect(cb).toHaveBeenCalledTimes(1);

    unmount();
    vi.advanceTimersByTime(5000);
    expect(cb).toHaveBeenCalledTimes(1);
  });

  it('restarts polling when enabled changes from false to true', () => {
    const cb = vi.fn();
    const { rerender } = renderHook(
      ({ enabled }) => usePolling(cb, 1000, enabled),
      { initialProps: { enabled: false } },
    );
    expect(cb).not.toHaveBeenCalled();

    rerender({ enabled: true });
    expect(cb).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1000);
    expect(cb).toHaveBeenCalledTimes(2);
  });
});
