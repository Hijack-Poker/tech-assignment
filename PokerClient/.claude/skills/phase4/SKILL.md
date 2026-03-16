---
name: phase4
description: Build all core view scripts — TableView, SeatView, CardView, CommunityCardsView, HudView. After this phase, the table renders any TableResponse correctly.
---

# Phase 4: Core Views

Read CLAUDE.md before starting. Phase 3 (scene skeleton) must be complete.

## Goal
Every FR-1, FR-3, and FR-4 requirement is visible on screen. Inject a sample state and the full table renders correctly — seats, cards, community cards, HUD, pot.

## Key Rules (from CLAUDE.md)
- All views subscribe to `TableStateManager.OnTableStateChanged` in `OnEnable`, unsubscribe in `OnDisable`
- Full redraw on every state change — NO delta patching
- `showCards = game.IsShowdown || player.IsWinner`
- Community cards: use `game.CommunityCards.Count` directly
- Use TextMeshProUGUI (never legacy Text)

---

## File 1: `Assets/Scripts/UI/TableView.cs`

Responsibilities:
- Subscribes to `TableStateManager.OnTableStateChanged`
- Holds serialized references to 6 `SeatView` instances (indexed by seat number 1-6)
- On state change: iterates players, calls `SeatView.Render(player, game)` for matching seat
- Clears/hides seat views with no matching player

```csharp
using System.Linq;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;

namespace HijackPoker.UI
{
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private SeatView[] _seatViews; // index 0 = seat 1, index 5 = seat 6
        [SerializeField] private CommunityCardsView _communityCardsView;

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            // Clear all seats first
            foreach (var seat in _seatViews)
                seat.Clear();

            // Render each player into their seat
            foreach (var player in state.Players)
            {
                int idx = player.Seat - 1; // seat 1 = index 0
                if (idx >= 0 && idx < _seatViews.Length)
                    _seatViews[idx].Render(player, state.Game);
            }

            // Render community cards
            _communityCardsView.Refresh(state.Game.CommunityCards);
        }
    }
}
```

---

## File 2: `Assets/Scripts/UI/SeatView.cs`

Responsibilities:
- Renders a single seat's full state
- Shows: name, stack, bet, action, D/SB/BB badges, fold dim, allin highlight, 2 hole cards

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class SeatView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stackText;
        [SerializeField] private TextMeshProUGUI _betText;
        [SerializeField] private TextMeshProUGUI _actionText;

        [Header("Badges")]
        [SerializeField] private GameObject _dealerBadge;
        [SerializeField] private GameObject _sbBadge;
        [SerializeField] private GameObject _bbBadge;

        [Header("Cards")]
        [SerializeField] private CardView _card1;
        [SerializeField] private CardView _card2;

        [Header("Visual")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _borderImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Winner")]
        [SerializeField] private TextMeshProUGUI _handRankText;
        [SerializeField] private TextMeshProUGUI _winningsText;

        private static readonly Color NormalColor = new Color(0.13f, 0.13f, 0.2f);
        private static readonly Color AllInColor = new Color(0.8f, 0.6f, 0f);

        public void Render(PlayerState player, GameState game)
        {
            gameObject.SetActive(true);

            // Text
            _nameText.text = player.Username;
            _stackText.text = MoneyFormatter.Format(player.Stack);
            _betText.text = player.Bet > 0 ? MoneyFormatter.Format(player.Bet) : "";
            _actionText.text = player.Action?.ToUpper() ?? "";

            // Badges
            _dealerBadge.SetActive(player.Seat == game.DealerSeat);
            _sbBadge.SetActive(player.Seat == game.SmallBlindSeat);
            _bbBadge.SetActive(player.Seat == game.BigBlindSeat);

            // Fold dim
            _canvasGroup.alpha = player.IsFolded ? 0.4f : 1f;

            // All-in highlight
            _borderImage.color = player.IsAllIn ? AllInColor : Color.clear;

            // Cards
            bool showCards = game.IsShowdown || player.IsWinner;
            if (player.HasCards && player.Cards.Count >= 2)
            {
                _card1.SetCard(player.Cards[0], showCards);
                _card2.SetCard(player.Cards[1], showCards);
            }
            else
            {
                _card1.SetEmpty();
                _card2.SetEmpty();
            }

            // Winner info
            bool isWinner = player.IsWinner;
            _handRankText.text = isWinner && !string.IsNullOrEmpty(player.HandRank) ? player.HandRank : "";
            _winningsText.text = isWinner ? MoneyFormatter.FormatGain(player.Winnings) : "";
            _winningsText.color = Color.green;
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }
    }
}
```

---

## File 3: `Assets/Scripts/UI/CardView.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _suitText;

        private static readonly Color FaceUpColor  = new Color(1f, 0.98f, 0.94f); // cream
        private static readonly Color FaceDownColor = new Color(0.1f, 0.15f, 0.35f); // dark navy
        private static readonly Color EmptyColor    = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        private static readonly Color RedColor      = new Color(0.85f, 0.1f, 0.1f);
        private static readonly Color BlackColor    = new Color(0.05f, 0.05f, 0.05f);

        public void SetCard(string cardCode, bool faceUp)
        {
            gameObject.SetActive(true);
            if (faceUp)
            {
                var (rank, suit) = CardUtils.ParseCard(cardCode);
                _background.color = FaceUpColor;
                _rankText.text = rank;
                _suitText.text = CardUtils.GetSuitSymbol(suit);
                Color textColor = CardUtils.IsRedSuit(suit) ? RedColor : BlackColor;
                _rankText.color = textColor;
                _suitText.color = textColor;
            }
            else
            {
                _background.color = FaceDownColor;
                _rankText.text = "";
                _suitText.text = "?";
                _suitText.color = Color.white;
            }
        }

        public void SetEmpty()
        {
            gameObject.SetActive(true);
            _background.color = EmptyColor;
            _rankText.text = "";
            _suitText.text = "";
        }
    }
}
```

---

## File 4: `Assets/Scripts/UI/CommunityCardsView.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace HijackPoker.UI
{
    public class CommunityCardsView : MonoBehaviour
    {
        [SerializeField] private CardView[] _slots; // exactly 5

        public void Refresh(List<string> cards)
        {
            int count = cards?.Count ?? 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < count)
                    _slots[i].SetCard(cards[i], faceUp: true);
                else
                    _slots[i].SetEmpty();
            }
        }
    }
}
```

---

## File 5: `Assets/Scripts/UI/HudView.cs`

```csharp
using UnityEngine;
using TMPro;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TableStateManager _stateManager;
        [SerializeField] private TextMeshProUGUI _phaseLabel;
        [SerializeField] private TextMeshProUGUI _handNumberText;
        [SerializeField] private TextMeshProUGUI _actionText;
        [SerializeField] private TextMeshProUGUI _potText;

        private void OnEnable() => _stateManager.OnTableStateChanged += OnStateChanged;
        private void OnDisable() => _stateManager.OnTableStateChanged -= OnStateChanged;

        private void OnStateChanged(TableResponse state)
        {
            var game = state.Game;
            _phaseLabel.text = _stateManager.GetStepLabel(game.HandStep);
            _handNumberText.text = $"Hand #{game.GameNo}";
            _potText.text = $"Pot: {MoneyFormatter.Format(game.Pot)}";

            if (game.Move > 0)
            {
                _actionText.text = $"Seat {game.Move} to act";
                _actionText.gameObject.SetActive(true);
            }
            else
            {
                _actionText.gameObject.SetActive(false);
            }
        }
    }
}
```

---

## Wire Up in Scene (via MCP)

After writing all scripts:
1. Add `TableView` component to `TableFelt` GameObject, wire `_stateManager` and 6 `SeatView` references
2. Add `HudView` component to `HUDPanel`, wire all TMP text references
3. For each Seat anchor: add `SeatView` component, create child GameObjects for name/stack/bet/action texts, badges, and 2 `CardView` children
4. Wire `CommunityCardsView` in `CenterGroup` with 5 `CardView` slots
5. Use `mcp__unity__manage_components` and `mcp__unity__manage_script` to attach and wire

## Acceptance Criteria
- [ ] Inject sample `TableResponse` via Inspector debug → table renders correctly
- [ ] Folded player seat is visibly dimmed (alpha 0.4)
- [ ] All-in player has gold border
- [ ] Face-down cards show at steps < 12 (dark background + "?")
- [ ] Face-up cards show rank + suit symbol + correct color at steps >= 12
- [ ] Community cards: 0 shown at step 5, 3 at step 6, 4 at step 8, 5 at step 10
- [ ] D/SB/BB badges show on correct seats
- [ ] Phase label shows "Dealing Flop" at step 6
- [ ] Hand number shows "Hand #3"
- [ ] Pot text shows formatted amount

## Next Phase
→ `/phase5`
