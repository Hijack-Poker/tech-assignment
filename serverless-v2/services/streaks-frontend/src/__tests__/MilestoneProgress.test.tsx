import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import MilestoneProgress, { getPreviousMilestoneDays } from '../components/MilestoneProgress';
import type { NextMilestone } from '../types/streaks.types';

describe('MilestoneProgress', () => {
  it('renders login milestone with days remaining badge', () => {
    const nextMilestone: NextMilestone = { days: 14, reward: 400, daysRemaining: 2 };

    render(
      <MilestoneProgress type="login" currentStreak={12} nextMilestone={nextMilestone} />
    );

    expect(screen.getByTestId('milestone-progress-login')).toBeInTheDocument();
    expect(screen.getByText('Next Login Milestone')).toBeInTheDocument();
    expect(screen.getByText('2 days left')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-progress-bar-login')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-message-login')).toHaveTextContent('14-day milestone');
    expect(screen.getByText('400 bonus points')).toBeInTheDocument();
  });

  it('renders play milestone with days remaining badge', () => {
    const nextMilestone: NextMilestone = { days: 7, reward: 300, daysRemaining: 2 };

    render(
      <MilestoneProgress type="play" currentStreak={5} nextMilestone={nextMilestone} />
    );

    expect(screen.getByTestId('milestone-progress-play')).toBeInTheDocument();
    expect(screen.getByText('Next Play Milestone')).toBeInTheDocument();
    expect(screen.getByText('2 days left')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-progress-bar-play')).toBeInTheDocument();
    expect(screen.getByTestId('milestone-message-play')).toHaveTextContent('7-day milestone');
    expect(screen.getByText('300 bonus points')).toBeInTheDocument();
  });

  it('shows max milestone message when nextMilestone is null', () => {
    render(
      <MilestoneProgress type="login" currentStreak={95} nextMilestone={null} />
    );

    expect(screen.getByTestId('milestone-max-login')).toHaveTextContent('All milestones reached!');
    expect(screen.queryByTestId('milestone-progress-bar-login')).not.toBeInTheDocument();
  });

  it('shows max milestone message for play streak', () => {
    render(
      <MilestoneProgress type="play" currentStreak={100} nextMilestone={null} />
    );

    expect(screen.getByTestId('milestone-max-play')).toHaveTextContent('All milestones reached!');
  });

  it('renders trophy icon', () => {
    const nextMilestone: NextMilestone = { days: 3, reward: 50, daysRemaining: 1 };

    render(
      <MilestoneProgress type="login" currentStreak={2} nextMilestone={nextMilestone} />
    );

    expect(screen.getByTestId('trophy-icon')).toBeInTheDocument();
  });

  it('renders first milestone progress correctly (previous = 0)', () => {
    const nextMilestone: NextMilestone = { days: 3, reward: 100, daysRemaining: 1 };

    render(
      <MilestoneProgress type="play" currentStreak={2} nextMilestone={nextMilestone} />
    );

    expect(screen.getByText('1 days left')).toBeInTheDocument();
    expect(screen.getByText('3-day milestone')).toBeInTheDocument();
  });
});

describe('getPreviousMilestoneDays', () => {
  it('returns 0 for the first milestone (3 days)', () => {
    expect(getPreviousMilestoneDays(3)).toBe(0);
  });

  it('returns 3 for the 7-day milestone', () => {
    expect(getPreviousMilestoneDays(7)).toBe(3);
  });

  it('returns 7 for the 14-day milestone', () => {
    expect(getPreviousMilestoneDays(14)).toBe(7);
  });

  it('returns 14 for the 30-day milestone', () => {
    expect(getPreviousMilestoneDays(30)).toBe(14);
  });

  it('returns 30 for the 60-day milestone', () => {
    expect(getPreviousMilestoneDays(60)).toBe(30);
  });

  it('returns 60 for the 90-day milestone', () => {
    expect(getPreviousMilestoneDays(90)).toBe(60);
  });

  it('returns 0 for unknown milestone days', () => {
    expect(getPreviousMilestoneDays(5)).toBe(0);
  });
});
