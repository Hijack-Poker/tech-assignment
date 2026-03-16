---
name: phase6
description: Add DOTween animations — winner gold pulse, card flip at showdown, pot/stack number tweens, phase label punch, button shake on error. Visual polish phase.
---

# Phase 6: Animations + Polish

Read CLAUDE.md before starting. Phase 5 must be complete (app fully playable).

## Goal
All "Should Have" animation requirements met using DOTween. The table feels alive — smooth transitions, clear winner moments, satisfying card reveals.

## DOTween Setup Reminder
- `using DG.Tweening;` must be available
- DOTween must be initialized: add `DOTween.Init()` in `GameManager.Awake()` if not already

---

## 1. Winner Gold Pulse — modify `SeatView.cs`

Add DOTween winner animation to `SeatView.Render()`:

```csharp
// In SeatView.cs — add field:
private Tweener _winnerPulseTween;

// In Render(), after setting winner info:
if (_winnerPulseTween != null) _winnerPulseTween.Kill();

if (player.IsWinner)
{
    // Pulse background gold
    _backgroundImage.color = NormalColor;
    _winnerPulseTween = _backgroundImage
        .DOColor(new Color(1f, 0.84f, 0f), 0.4f)
        .SetLoops(6, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);

    // Animate winnings text appearance
    _winningsText.transform.localScale = Vector3.zero;
    _winningsText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
}
else
{
    _backgroundImage.color = NormalColor;
}

// In Clear():
if (_winnerPulseTween != null) { _winnerPulseTween.Kill(); _winnerPulseTween = null; }
_backgroundImage.color = NormalColor;
```

---

## 2. Card Flip Animation at Showdown — modify `CardView.cs`

When `faceUp` changes from false to true, animate the flip:

```csharp
// In CardView.cs — add field:
private bool _wasFaceDown = true;

// Modify SetCard():
public void SetCard(string cardCode, bool faceUp)
{
    gameObject.SetActive(true);

    bool shouldFlip = _wasFaceDown && faceUp;
    _wasFaceDown = !faceUp;

    if (shouldFlip)
    {
        // Flip animation: scale X to 0, swap content, scale X back to 1
        transform.DOScaleX(0f, 0.12f).SetEase(Ease.InSine).OnComplete(() =>
        {
            ApplyFaceUpVisuals(cardCode);
            transform.DOScaleX(1f, 0.12f).SetEase(Ease.OutSine);
        });
    }
    else
    {
        if (faceUp) ApplyFaceUpVisuals(cardCode);
        else ApplyFaceDownVisuals();
    }
}

private void ApplyFaceUpVisuals(string cardCode)
{
    var (rank, suit) = CardUtils.ParseCard(cardCode);
    _background.color = FaceUpColor;
    _rankText.text = rank;
    _suitText.text = CardUtils.GetSuitSymbol(suit);
    Color textColor = CardUtils.IsRedSuit(suit) ? RedColor : BlackColor;
    _rankText.color = textColor;
    _suitText.color = textColor;
}

private void ApplyFaceDownVisuals()
{
    _background.color = FaceDownColor;
    _rankText.text = "";
    _suitText.text = "?";
    _suitText.color = Color.white;
}
```

---

## 3. Pot + Stack Number Tweens

### In `HudView.cs` — pot tween:
```csharp
// Add field:
private float _displayedPot;

// Replace pot text line in OnStateChanged:
DOVirtual.Float(_displayedPot, state.Game.Pot, 0.4f, value =>
{
    _displayedPot = value;
    _potText.text = $"Pot: {MoneyFormatter.Format(value)}";
}).SetEase(Ease.OutCubic);
```

### In `SeatView.cs` — stack tween:
```csharp
// Add field:
private float _displayedStack;

// Replace stack text line in Render():
DOVirtual.Float(_displayedStack, player.Stack, 0.4f, value =>
{
    _displayedStack = value;
    _stackText.text = MoneyFormatter.Format(value);
}).SetEase(Ease.OutCubic);
```

---

## 4. Phase Label Punch — modify `HudView.cs`

```csharp
// In OnStateChanged(), after setting _phaseLabel.text:
_phaseLabel.transform.DOKill();
_phaseLabel.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 8, 0.5f);
```

---

## 5. Button Shake on API Error — modify `StatusBarView.cs`

When status starts with "Error:", shake the status bar text:

```csharp
// In OnStatusChanged(), inside the else (error) block:
_statusText.transform.DOShakePosition(0.3f, new Vector3(5f, 0f, 0f), 20, 0f);
```

Also in `ControlsView.cs`, animate the Next Step button on error:
- Subscribe to `TableStateManager.OnConnectionStatusChanged`
- On error: `_nextStepButton.transform.DOShakePosition(0.3f, new Vector3(5f, 0f, 0f), 20, 0f)`

---

## 6. DOTween Init — modify `GameManager.cs`

```csharp
// Add in Awake():
private void Awake()
{
    DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly);
}
```

---

## 7. Cleanup on Scene Change

In any MonoBehaviour that uses DOTween, kill tweens on destroy:

```csharp
private void OnDestroy()
{
    DOTween.Kill(transform); // kills all tweens on this transform
}
```

---

## Acceptance Criteria
- [ ] Winner seat pulses gold at steps 13-15
- [ ] Winnings text pops in with scale bounce
- [ ] Hole cards flip from face-down to face-up at step 12 transition
- [ ] Pot amount counts up smoothly when it changes
- [ ] Stack amount counts down/up smoothly after payouts at step 14
- [ ] Phase label punches on every step change
- [ ] Status bar text shakes on API error
- [ ] No DOTween errors in console
- [ ] No null reference exceptions from killed tweens

## Next Phase
→ `/phase7`
