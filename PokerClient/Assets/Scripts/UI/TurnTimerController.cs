using System;
using System.Collections;
using UnityEngine;
using HijackPoker.Managers;
using HijackPoker.Models;
using HijackPoker.Utils;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages the per-seat turn timer ring, including auto-fold on timeout.
    /// </summary>
    public class TurnTimerController : MonoBehaviour
    {
        private SeatView[] _seatViews;
        private Func<int, int> _seatToViewIndex;
        private GameManager _gameManager;
        private TableStateManager _stateManager;
        private TableAudioController _audio;

        private Coroutine _turnTimerRoutine;
        private int _turnTimerSeat = -1;
        private int _turnTimerStep = -1;
        private int _turnTimerGameNo = -1;
        private bool _hasPlayedLowTimeWarning;

        public void Initialize(SeatView[] seatViews, Func<int, int> seatToViewIndex,
                               GameManager gameManager, TableStateManager stateManager,
                               TableAudioController audio)
        {
            _seatViews = seatViews;
            _seatToViewIndex = seatToViewIndex;
            _gameManager = gameManager;
            _stateManager = stateManager;
            _audio = audio;
        }

        public void UpdateTurnTimer(TableResponse state)
        {
            if (state == null || state.Game == null) return;

            bool isBettingStep = PokerConstants.IsBettingStep(state.Game.HandStep);
            int moveSeat = state.Game.Move;

            if (!isBettingStep || moveSeat <= 0)
            {
                StopTurnTimer();
                return;
            }

            bool changed = _turnTimerRoutine == null ||
                           _turnTimerSeat != moveSeat ||
                           _turnTimerStep != state.Game.HandStep ||
                           _turnTimerGameNo != state.Game.GameNo;
            if (!changed) return;

            StopTurnTimer();
            _turnTimerSeat = moveSeat;
            _turnTimerStep = state.Game.HandStep;
            _turnTimerGameNo = state.Game.GameNo;
            _hasPlayedLowTimeWarning = false;
            _audio?.PlayTurnStartSound();
            _turnTimerRoutine = StartCoroutine(TurnTimerCoroutine(_turnTimerSeat, _turnTimerStep, _turnTimerGameNo));
        }

        private IEnumerator TurnTimerCoroutine(int seat, int step, int gameNo)
        {
            float remaining = PokerConstants.TurnDurationSeconds;
            while (remaining > 0f)
            {
                float normalized = remaining / PokerConstants.TurnDurationSeconds;
                ShowTurnTimerOnSeat(seat, normalized);

                if (!_hasPlayedLowTimeWarning && remaining <= PokerConstants.LowTimeWarningSeconds)
                {
                    _hasPlayedLowTimeWarning = true;
                    _audio?.PlayTimeRemainingSound();
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            ShowTurnTimerOnSeat(seat, 0f);

            var current = _stateManager != null ? _stateManager.CurrentState : null;
            bool stillSameTurn = current != null &&
                                 current.Game != null &&
                                 current.Game.GameNo == gameNo &&
                                 current.Game.HandStep == step &&
                                 current.Game.Move == seat &&
                                 PokerConstants.IsBettingStep(current.Game.HandStep);
            if (stillSameTurn && _gameManager != null)
            {
                _ = _gameManager.AdvanceStepAsync("fold", 0f);
            }
        }

        public void StopTurnTimer()
        {
            if (_turnTimerRoutine != null)
            {
                StopCoroutine(_turnTimerRoutine);
                _turnTimerRoutine = null;
            }
            ClearTurnTimers();
            _audio?.StopTimeRemainingSound();
            _turnTimerSeat = -1;
            _turnTimerStep = -1;
            _turnTimerGameNo = -1;
            _hasPlayedLowTimeWarning = false;
        }

        private void ShowTurnTimerOnSeat(int seat, float normalized)
        {
            for (int i = 0; i < _seatViews.Length; i++)
            {
                if (_seatViews[i] == null) continue;
                bool active = i == _seatToViewIndex(seat);
                _seatViews[i].SetTurnTimer(active, normalized);
            }
        }

        private void ClearTurnTimers()
        {
            if (_seatViews == null) return;
            for (int i = 0; i < _seatViews.Length; i++)
            {
                if (_seatViews[i] == null) continue;
                _seatViews[i].SetTurnTimer(false, 0f);
            }
        }
    }
}
