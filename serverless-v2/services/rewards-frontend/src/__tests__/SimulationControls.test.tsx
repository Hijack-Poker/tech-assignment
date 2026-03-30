import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { theme } from '../theme';
import SimulationControls from '../components/SimulationControls';

function renderControls(enabled: boolean, onToggle = vi.fn()) {
  return render(
    <ThemeProvider theme={theme}>
      <SimulationControls enabled={enabled} onToggle={onToggle} />
    </ThemeProvider>,
  );
}

describe('SimulationControls', () => {
  it('renders the toggle label', () => {
    renderControls(false);
    expect(screen.getByText('Simulate Activity')).toBeInTheDocument();
  });

  it('does not show LIVE indicator when disabled', () => {
    renderControls(false);
    expect(screen.queryByText('LIVE')).not.toBeInTheDocument();
  });

  it('shows LIVE indicator when enabled', () => {
    renderControls(true);
    expect(screen.getByText('LIVE')).toBeInTheDocument();
  });

  it('renders a switch input', () => {
    renderControls(false);
    const inputs = screen.getAllByRole('checkbox');
    expect(inputs.length).toBeGreaterThanOrEqual(1);
  });

  it('switch reflects enabled state', () => {
    const { container } = renderControls(true);
    const input = container.querySelector('input[type="checkbox"]') as HTMLInputElement;
    expect(input.checked).toBe(true);
  });

  it('switch is unchecked when disabled', () => {
    renderControls(false);
    const input = screen.getAllByRole('checkbox')[0] as HTMLInputElement;
    expect(input.checked).toBe(false);
  });
});
