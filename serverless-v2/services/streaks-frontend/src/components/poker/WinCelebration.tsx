import { useEffect, useRef, useCallback } from 'react';
import { Box } from '@mui/system';
import gsap from 'gsap';

const CHIP_COLORS = ['#FFD700', '#e53935', '#1E88E5', '#4ADE80', '#FF6B35', '#E040FB'];
const MONEY_EMOJIS = ['💰', '💵', '🤑', '💎', '🏆', '👑', '🎰', '💲'];

interface WinCelebrationProps {
  active: boolean;
  amount?: number;
  winnerName?: string;
  onComplete?: () => void;
}

function WinCelebration({ active, amount, winnerName, onComplete }: WinCelebrationProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  const stableOnComplete = useCallback(() => {
    onComplete?.();
  }, [onComplete]);

  useEffect(() => {
    if (!active || !containerRef.current) return;

    const container = containerRef.current;
    container.innerHTML = '';

    // Poker chips flying out
    const chips: HTMLDivElement[] = [];
    for (let i = 0; i < 40; i++) {
      const el = document.createElement('div');
      const isEmoji = i < 10;
      if (isEmoji) {
        el.textContent = MONEY_EMOJIS[i % MONEY_EMOJIS.length];
        el.style.fontSize = `${18 + Math.random() * 14}px`;
      } else {
        // Poker chip style
        const size = 8 + Math.random() * 10;
        el.style.width = `${size}px`;
        el.style.height = `${size}px`;
        el.style.backgroundColor = CHIP_COLORS[Math.floor(Math.random() * CHIP_COLORS.length)];
        el.style.borderRadius = '50%';
        el.style.border = '2px solid rgba(255,255,255,0.7)';
        el.style.boxShadow = '0 1px 3px rgba(0,0,0,0.4)';
      }
      el.style.position = 'absolute';
      el.style.left = '50%';
      el.style.top = '50%';
      el.style.opacity = '0';
      el.style.pointerEvents = 'none';
      container.appendChild(el);
      chips.push(el);
    }

    // Winner banner
    const banner = document.createElement('div');
    const displayText = winnerName ? `${winnerName} WINS!` : 'WINNER!';
    banner.textContent = displayText;
    banner.style.cssText = `
      position: absolute; left: 50%; top: 40%; transform: translate(-50%, -50%) scale(0);
      font-size: 42px; font-weight: 900; color: #FFD700;
      text-shadow: 0 0 20px rgba(255,215,0,0.6), 0 2px 4px rgba(0,0,0,0.5);
      pointer-events: none; white-space: nowrap; letter-spacing: 3px;
      font-family: inherit;
    `;
    container.appendChild(banner);

    // Amount display
    const amountEl = document.createElement('div');
    amountEl.textContent = amount ? `+$${amount.toFixed(2)}` : '';
    amountEl.style.cssText = `
      position: absolute; left: 50%; top: 52%; transform: translate(-50%, -50%) scale(0);
      font-size: 32px; font-weight: 900; color: #4ADE80;
      text-shadow: 0 0 16px rgba(74,222,128,0.6), 0 2px 4px rgba(0,0,0,0.5);
      pointer-events: none; white-space: nowrap;
      font-family: inherit;
    `;
    container.appendChild(amountEl);

    // Gold ring burst
    const ring = document.createElement('div');
    ring.style.cssText = `
      position: absolute; left: 50%; top: 45%; width: 0; height: 0;
      border: 4px solid rgba(255,215,0,0.7); border-radius: 50%;
      transform: translate(-50%, -50%); pointer-events: none;
    `;
    container.appendChild(ring);

    const ring2 = document.createElement('div');
    ring2.style.cssText = `
      position: absolute; left: 50%; top: 45%; width: 0; height: 0;
      border: 2px solid rgba(74,222,128,0.5); border-radius: 50%;
      transform: translate(-50%, -50%); pointer-events: none;
    `;
    container.appendChild(ring2);

    const tl = gsap.timeline({
      onComplete: () => {
        container.innerHTML = '';
        stableOnComplete();
      },
    });

    // Ring burst
    tl.to(ring, { width: 350, height: 350, opacity: 0, duration: 0.7, ease: 'power2.out' }, 0);
    tl.to(ring2, { width: 500, height: 500, opacity: 0, duration: 0.9, ease: 'power2.out' }, 0.1);

    // Banner punch in
    tl.fromTo(banner,
      { scale: 0, rotation: -5 },
      { scale: 1, rotation: 0, duration: 0.5, ease: 'back.out(2)' },
      0.1
    );

    // Amount slides up
    if (amount) {
      tl.fromTo(amountEl,
        { scale: 0, y: 20 },
        { scale: 1, y: 0, duration: 0.4, ease: 'back.out(2)' },
        0.35
      );
    }

    // Hold, then fade out
    tl.to(banner, { scale: 1.15, opacity: 0, duration: 0.5, ease: 'power2.in' }, 1.4);
    tl.to(amountEl, { scale: 1.1, opacity: 0, duration: 0.5, ease: 'power2.in' }, 1.4);

    // Chips explosion — upward arc like chips being tossed
    chips.forEach((el, i) => {
      const angle = (i / chips.length) * Math.PI * 2 + (Math.random() - 0.5) * 0.6;
      const velocity = 120 + Math.random() * 200;
      const xDist = Math.cos(angle) * velocity;
      const yDist = Math.sin(angle) * velocity - 120; // strong upward bias

      tl.to(el, { opacity: 1, duration: 0.05 }, 0.05 + Math.random() * 0.2);

      tl.to(el, {
        x: xDist,
        y: yDist,
        rotation: (Math.random() - 0.5) * 540,
        duration: 1 + Math.random() * 0.5,
        ease: 'power2.out',
      }, 0.05 + Math.random() * 0.2);

      // Gravity
      tl.to(el, {
        y: `+=${150 + Math.random() * 250}`,
        opacity: 0,
        duration: 0.7 + Math.random() * 0.3,
        ease: 'power1.in',
      }, 0.7 + Math.random() * 0.3);
    });

    return () => {
      tl.kill();
      container.innerHTML = '';
    };
  }, [active, amount, winnerName, stableOnComplete]);

  return (
    <Box
      ref={containerRef}
      sx={{
        position: 'fixed',
        inset: 0,
        pointerEvents: 'none',
        zIndex: 9999,
        overflow: 'hidden',
      }}
    />
  );
}

export default WinCelebration;
