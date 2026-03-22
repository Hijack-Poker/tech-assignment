using UnityEngine;
using System.Reflection;
using uVegas.Demo;

namespace HijackPoker.UI
{
    /// <summary>
    /// Manages all table-level sound effects: shuffle, fold, chip bet, turn start,
    /// time remaining warning, and crowd clap.
    /// </summary>
    public class TableAudioController : MonoBehaviour
    {
        private AudioSource _sfxAudioSource;
        private AudioClip _turnStartSound;
        private AudioClip _timeRemainingSound;
        private AudioClip _dealShuffleSound;
        private AudioClip _foldSound;
        private AudioClip _chipBetSound;
        private AudioClip _crowdClapSound;

        private AudioSource _timeWarningAudioSource;
        private float _lastChipSoundTime = -10f;
        private const float ChipSoundCooldownSeconds = 0.06f;

        public void Initialize()
        {
            EnsureAudioRefs();
            TryResolveTurnStartSound();
            TryResolveDealAndFoldSounds();
        }

        public void EnsureAudioRefs()
        {
            if (_sfxAudioSource == null)
                _sfxAudioSource = GetComponent<AudioSource>();
            if (_sfxAudioSource == null)
                _sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        public void TryResolveTurnStartSound()
        {
            if (_turnStartSound != null) return;

            var cardThemeManager = FindObjectOfType<CardThemeManager>();
            if (cardThemeManager != null)
            {
                var clipField = typeof(CardThemeManager).GetField("winSound", BindingFlags.Instance | BindingFlags.NonPublic);
                var sourceField = typeof(CardThemeManager).GetField("sfxAudioSource", BindingFlags.Instance | BindingFlags.NonPublic);

                _turnStartSound = clipField?.GetValue(cardThemeManager) as AudioClip;
                if (_sfxAudioSource == null)
                    _sfxAudioSource = sourceField?.GetValue(cardThemeManager) as AudioSource;
            }

            if (_turnStartSound == null)
                _turnStartSound = Resources.Load<AudioClip>("Audio/win_01");
            if (_timeRemainingSound == null)
                _timeRemainingSound = Resources.Load<AudioClip>("Audio/time_remaning");
        }

        public void TryResolveDealAndFoldSounds()
        {
            if (_dealShuffleSound == null)
                _dealShuffleSound = Resources.Load<AudioClip>("Audio/card_shuffle_custom");
            if (_foldSound == null)
                _foldSound = Resources.Load<AudioClip>("Audio/hover_01");
            _chipBetSound = Resources.Load<AudioClip>("Audio/chips");
            _crowdClapSound = Resources.Load<AudioClip>("Audio/crowd-clap");
        }

        public void PlayTurnStartSound()
        {
            if (_turnStartSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_turnStartSound);
        }

        public void PlayTimeRemainingSound()
        {
            if (_timeRemainingSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;

            if (_timeWarningAudioSource == null)
            {
                _timeWarningAudioSource = gameObject.AddComponent<AudioSource>();
                _timeWarningAudioSource.playOnAwake = false;
            }
            _timeWarningAudioSource.clip = _timeRemainingSound;
            _timeWarningAudioSource.Play();
        }

        public void StopTimeRemainingSound()
        {
            if (_timeWarningAudioSource != null && _timeWarningAudioSource.isPlaying)
                _timeWarningAudioSource.Stop();
        }

        public void PlayDealShuffleSound()
        {
            if (_dealShuffleSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_dealShuffleSound);
        }

        public void PlayFoldSound()
        {
            if (_foldSound == null) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;
            _sfxAudioSource.PlayOneShot(_foldSound);
        }

        public void PlayChipBetSound()
        {
            if (_chipBetSound == null) return;
            if (Time.unscaledTime - _lastChipSoundTime < ChipSoundCooldownSeconds) return;
            EnsureAudioRefs();
            if (_sfxAudioSource == null) return;

            _lastChipSoundTime = Time.unscaledTime;
            _sfxAudioSource.PlayOneShot(_chipBetSound);
        }

        public void PlayCrowdClapSound()
        {
            if (_sfxAudioSource != null && _crowdClapSound != null)
                _sfxAudioSource.PlayOneShot(_crowdClapSound);
        }
    }
}
