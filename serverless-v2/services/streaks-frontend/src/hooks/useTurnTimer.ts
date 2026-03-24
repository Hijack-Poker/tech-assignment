import { useState, useEffect, useRef, useCallback } from 'react';

const TURN_DURATION = 15; // seconds

export function useTurnTimer(
  actingSeat: number | null,
  onTimeout: (seat: number) => void,
) {
  const [timeLeft, setTimeLeft] = useState(TURN_DURATION);
  const [timedOut, setTimedOut] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const seatRef = useRef<number | null>(null);
  const onTimeoutRef = useRef(onTimeout);
  onTimeoutRef.current = onTimeout;

  const clear = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  // Fire timeout callback outside of state updater
  useEffect(() => {
    if (timedOut && seatRef.current !== null) {
      const seat = seatRef.current;
      setTimedOut(false);
      onTimeoutRef.current(seat);
    }
  }, [timedOut]);

  useEffect(() => {
    clear();
    setTimedOut(false);

    if (actingSeat === null) {
      setTimeLeft(TURN_DURATION);
      return;
    }

    // Reset timer when acting seat changes
    seatRef.current = actingSeat;
    setTimeLeft(TURN_DURATION);

    intervalRef.current = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          clear();
          setTimedOut(true);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return clear;
  }, [actingSeat, clear]);

  const progress = (timeLeft / TURN_DURATION) * 100;

  return { timeLeft, progress, totalTime: TURN_DURATION };
}
