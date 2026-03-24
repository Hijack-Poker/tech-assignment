import { Box, Typography } from '@mui/material';
import { keyframes } from '@mui/system';
import CardGroup from '../cards/CardGroup';
import PlayerSeat from '../player/PlayerSeat';
import type { TableState } from '../../../types/poker.types';

interface PokerTableProps {
  tableState: TableState;
  timerProgress?: number;
  timeLeft?: number;
  heroPlayerId?: string | number | null;
  heroDisplayName?: string | null;
}

// Positions around the table, hero at bottom-center, rest clockwise
const ORDERED_POSITIONS = [
  { top: 340, left: 315 },  // 0: bottom center (hero)
  { top: 340, left: 515 },  // 1: bottom-right
  { top: 170, left: 630 },  // 2: right
  { top: -10, left: 465 },  // 3: top-right
  { top: -10, left: 185 },  // 4: top-left
  { top: 170, left: 55 },   // 5: left
];

const CANVAS_W = 700;
const CANVAS_H = 440;

const cardSlide = keyframes`
  0% { transform: translateY(-20px); opacity: 0; }
  100% { transform: translateY(0); opacity: 1; }
`;

function PokerTable({ tableState, timerProgress, timeLeft, heroPlayerId, heroDisplayName }: PokerTableProps) {
  const { game, players } = tableState;
  const hasCards = game.communityCards && game.communityCards.length > 0;

  // Hero is always seat 1 (first player at the table in local demo)
  const heroSeat = 1;
  const heroIndex = players.findIndex((p) => p.seat === heroSeat);
  const orderedPlayers = heroIndex >= 0
    ? [...players.slice(heroIndex), ...players.slice(0, heroIndex)]
    : players;

  return (
    <Box
      data-testid="poker-table"
      sx={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
    >
      <Box
        sx={{
          position: 'relative',
          width: CANVAS_W,
          height: CANVAS_H,
          flexShrink: 0,
          transform: {
            xs: 'scale(0.52)',
            sm: 'scale(0.7)',
            md: 'scale(0.82)',
            lg: 'scale(0.95)',
            xl: 'scale(1)',
          },
          transformOrigin: 'center center',
        }}
      >
        {/* Table felt */}
        <Box
          sx={{
            position: 'absolute',
            top: 55,
            left: 80,
            width: 540,
            height: 330,
            borderRadius: '50%',
            background: 'radial-gradient(ellipse at 40% 40%, #2d7a4a, #1b5c35, #134528)',
            border: '8px solid #3d2a1a',
            boxShadow: '0 0 50px rgba(0,0,0,0.6), inset 0 0 30px rgba(0,0,0,0.3), 0 0 0 12px #2a1a0a',
          }}
        >
          {/* Community cards — animated */}
          <Box
            sx={{
              position: 'absolute',
              top: '42%',
              left: '50%',
              transform: 'translate(-50%, -50%)',
              display: 'flex',
              gap: 0.5,
              animation: hasCards ? `${cardSlide} 0.5s ease-out` : 'none',
            }}
          >
            {hasCards ? (
              <CardGroup cards={game.communityCards} totalSlots={5} size="small" />
            ) : (
              <CardGroup cards={[]} totalSlots={5} size="small" />
            )}
          </Box>

          {/* Pot / Blinds */}
          <Box
            data-testid="pot-display"
            sx={{
              position: 'absolute',
              top: '58%',
              left: '50%',
              transform: 'translateX(-50%)',
              bgcolor: 'rgba(0,0,0,0.55)',
              borderRadius: 1.5,
              px: 1.5,
              py: 0.3,
            }}
          >
            <Typography sx={{ color: '#FFD700', fontWeight: 700, fontSize: 13, whiteSpace: 'nowrap' }}>
              {game.pot > 0 ? `POT: $${game.pot.toFixed(2)}` : `Blinds: $${game.smallBlind}/$${game.bigBlind}`}
            </Typography>
          </Box>
        </Box>

        {/* Player seats */}
        {orderedPlayers.map((player, idx) => {
          const pos = ORDERED_POSITIONS[idx];
          if (!pos) return null;
          const isActing = game.move === player.seat && player.status === '1' && game.stepName.includes('BETTING');
          const isHero = player.seat === heroSeat;
          const displayPlayer = isHero && heroDisplayName
            ? { ...player, username: heroDisplayName }
            : player;
          return (
            <Box
              key={player.seat}
              sx={{
                position: 'absolute',
                top: pos.top,
                left: pos.left,
                transform: 'translate(-50%, 0)',
                zIndex: isHero ? 15 : isActing ? 10 : 2,
                transition: 'z-index 0s',
              }}
            >
              <PlayerSeat
                player={displayPlayer}
                game={game}
                timerProgress={isActing ? timerProgress : undefined}
                timeLeft={isActing ? timeLeft : undefined}
                isHero={isHero}
              />
            </Box>
          );
        })}
      </Box>
    </Box>
  );
}

export default PokerTable;
