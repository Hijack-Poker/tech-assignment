import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

// Mock GSAP — it relies on real DOM computed styles which jsdom doesn't provide.
// For gsap.to(), immediately invoke the onUpdate callback with the final value
// so that animated counters settle to their target in tests.
vi.mock('gsap', () => ({
  default: {
    fromTo: vi.fn(),
    to: vi.fn((target: Record<string, unknown>, vars: Record<string, unknown>) => {
      // Simulate immediate completion: set the target property to the final value
      // and call onUpdate so the component renders the correct number.
      if (vars && typeof vars.onUpdate === 'function') {
        if (typeof vars.val === 'number') {
          target.val = vars.val;
        }
        (vars.onUpdate as () => void)();
      }
    }),
    set: vi.fn(),
    timeline: vi.fn(() => ({
      fromTo: vi.fn().mockReturnThis(),
      to: vi.fn().mockReturnThis(),
      play: vi.fn(),
    })),
    registerPlugin: vi.fn(),
  },
}));
