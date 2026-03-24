import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import StreakCounter, { getFlameScale, getFlameColor } from '../components/StreakCounter';

describe('StreakCounter', () => {
  it('renders login streak with flame icon and count', () => {
    render(<StreakCounter type="login" count={5} label="Login Streak" />);

    expect(screen.getByTestId('streak-counter-login')).toHaveTextContent('5');
    expect(screen.getByText('Login Streak')).toBeInTheDocument();
    expect(screen.getByTestId('flame-icon')).toBeInTheDocument();
  });

  it('renders play streak with cards icon and count', () => {
    render(<StreakCounter type="play" count={3} label="Play Streak" />);

    expect(screen.getByTestId('streak-counter-play')).toHaveTextContent('3');
    expect(screen.getByText('Play Streak')).toBeInTheDocument();
    expect(screen.getByTestId('cards-icon')).toBeInTheDocument();
  });

  it('renders zero streak count', () => {
    render(<StreakCounter type="login" count={0} label="Login Streak" />);

    expect(screen.getByTestId('streak-counter-login')).toHaveTextContent('0');
  });

  it('shows best streak when provided', () => {
    render(<StreakCounter type="login" count={5} label="Login Streak" best={45} />);

    expect(screen.getByText('Best: 45 days')).toBeInTheDocument();
  });
});

describe('getFlameScale', () => {
  it('returns 1.0 for streaks 1-7', () => {
    expect(getFlameScale(1)).toBe(1.0);
    expect(getFlameScale(7)).toBe(1.0);
  });

  it('returns 1.3 for streaks 8-14', () => {
    expect(getFlameScale(8)).toBe(1.3);
    expect(getFlameScale(14)).toBe(1.3);
  });

  it('returns 1.6 for streaks 15-30', () => {
    expect(getFlameScale(15)).toBe(1.6);
    expect(getFlameScale(30)).toBe(1.6);
  });

  it('returns 2.0 for streaks above 30', () => {
    expect(getFlameScale(31)).toBe(2.0);
    expect(getFlameScale(100)).toBe(2.0);
  });
});

describe('getFlameColor', () => {
  it('returns base color for low streaks', () => {
    expect(getFlameColor(5)).toBe('#FF9800');
  });

  it('returns more intense color for higher streaks', () => {
    expect(getFlameColor(10)).toBe('#FF9100');
    expect(getFlameColor(20)).toBe('#FF6D00');
    expect(getFlameColor(31)).toBe('#FF3D00');
  });
});
