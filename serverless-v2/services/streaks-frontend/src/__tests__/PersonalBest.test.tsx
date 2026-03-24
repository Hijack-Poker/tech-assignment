import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import PersonalBest from '../components/PersonalBest';

describe('PersonalBest', () => {
  it('displays best login and play streak values', () => {
    render(
      <PersonalBest
        bestLoginStreak={45}
        bestPlayStreak={22}
        currentLoginStreak={10}
        currentPlayStreak={5}
      />
    );

    expect(screen.getByTestId('personal-best')).toBeInTheDocument();
    expect(screen.getByText(/Best login streak:/)).toBeInTheDocument();
    expect(screen.getByText('45 days')).toBeInTheDocument();
    expect(screen.getByText(/Best play streak:/)).toBeInTheDocument();
    expect(screen.getByText('22 days')).toBeInTheDocument();
  });

  it('displays the trophy icon', () => {
    render(
      <PersonalBest
        bestLoginStreak={10}
        bestPlayStreak={5}
        currentLoginStreak={3}
        currentPlayStreak={2}
      />
    );

    expect(screen.getByTestId('trophy-icon')).toBeInTheDocument();
  });

  it('shows visual indicator when current login streak equals best', () => {
    render(
      <PersonalBest
        bestLoginStreak={45}
        bestPlayStreak={22}
        currentLoginStreak={45}
        currentPlayStreak={5}
      />
    );

    expect(screen.getByTestId('login-best-indicator')).toBeInTheDocument();
    expect(screen.queryByTestId('play-best-indicator')).not.toBeInTheDocument();
  });

  it('shows visual indicator when current play streak equals best', () => {
    render(
      <PersonalBest
        bestLoginStreak={45}
        bestPlayStreak={22}
        currentLoginStreak={10}
        currentPlayStreak={22}
      />
    );

    expect(screen.queryByTestId('login-best-indicator')).not.toBeInTheDocument();
    expect(screen.getByTestId('play-best-indicator')).toBeInTheDocument();
  });

  it('shows both indicators when both current streaks equal best', () => {
    render(
      <PersonalBest
        bestLoginStreak={45}
        bestPlayStreak={22}
        currentLoginStreak={45}
        currentPlayStreak={22}
      />
    );

    expect(screen.getByTestId('login-best-indicator')).toBeInTheDocument();
    expect(screen.getByTestId('play-best-indicator')).toBeInTheDocument();
  });

  it('does not show indicators when current streaks are zero', () => {
    render(
      <PersonalBest
        bestLoginStreak={0}
        bestPlayStreak={0}
        currentLoginStreak={0}
        currentPlayStreak={0}
      />
    );

    expect(screen.queryByTestId('login-best-indicator')).not.toBeInTheDocument();
    expect(screen.queryByTestId('play-best-indicator')).not.toBeInTheDocument();
  });

  it('does not show indicators when current streaks are below best', () => {
    render(
      <PersonalBest
        bestLoginStreak={45}
        bestPlayStreak={22}
        currentLoginStreak={10}
        currentPlayStreak={5}
      />
    );

    expect(screen.queryByTestId('login-best-indicator')).not.toBeInTheDocument();
    expect(screen.queryByTestId('play-best-indicator')).not.toBeInTheDocument();
  });
});
