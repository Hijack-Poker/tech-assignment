using UnityEngine;

namespace HijackPoker.Managers
{
    public enum SoundType
    {
        ButtonClick,
        CardDeal,
        CardFlip,
        ChipClink,
        WinnerFanfare,
        Shuffle,
        CommunityCardReveal,
        FoldSwoosh,
        Sparkle,
        RingPulse,
        AllIn,
        Starburst,
        TimerWarning
    }

    /// <summary>
    /// Singleton audio manager that generates procedural sound effects and plays them
    /// on demand. Redesigned for warm, musical, "sparkly" sound design.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }

        private AudioSource _sfxSource;
        private AudioClip[] _clips;
        private bool _muted;

        // Per-sound volume balancing indexed by SoundType
        private static readonly float[] _volumes = {
            1.0f,  // ButtonClick
            1.0f,  // CardDeal
            1.0f,  // CardFlip
            1.1f,  // ChipClink
            1.3f,  // WinnerFanfare
            1.0f,  // Shuffle
            1.1f,  // CommunityCardReveal
            1.1f,  // FoldSwoosh
            0.7f,  // Sparkle
            0.7f,  // RingPulse
            1.1f,  // AllIn
            1.3f,  // Starburst
            0.5f,  // TimerWarning
        };

        // Per-sound pitch humanization ranges
        private static readonly float[] _pitchRange = {
            0.05f, // ButtonClick
            0.05f, // CardDeal
            0.05f, // CardFlip
            0.02f, // ChipClink (tonal — keep tight)
            0.02f, // WinnerFanfare (tonal)
            0.05f, // Shuffle
            0.05f, // CommunityCardReveal
            0.05f, // FoldSwoosh
            0.05f, // Sparkle
            0.05f, // RingPulse
            0.05f, // AllIn
            0.05f, // Starburst
            0.02f, // TimerWarning (tonal — keep tight)
        };

        public bool IsMuted => _muted;

        public static void Initialize(GameObject parent)
        {
            if (_instance != null) return;
            _instance = parent.AddComponent<AudioManager>();
            _instance.Setup();
        }

        private void Setup()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.6f;

            GenerateClips();
        }

        public void Play(SoundType type)
        {
            if (_muted || _clips == null) return;
            int idx = (int)type;
            if (idx >= 0 && idx < _clips.Length && _clips[idx] != null)
            {
                float range = idx < _pitchRange.Length ? _pitchRange[idx] : 0.05f;
                _sfxSource.pitch = 1f + UnityEngine.Random.Range(-range, range);
                _sfxSource.volume = 0.6f * (idx < _volumes.Length ? _volumes[idx] : 1f);
                _sfxSource.PlayOneShot(_clips[idx]);
            }
        }

        public void PlayWithDelay(SoundType type, float delay)
        {
            if (_muted) return;
            StartCoroutine(PlayDelayedCoroutine(type, delay));
        }

        private System.Collections.IEnumerator PlayDelayedCoroutine(SoundType type, float delay)
        {
            yield return new WaitForSeconds(delay);
            Play(type);
        }

        public void ToggleMute()
        {
            _muted = !_muted;
        }

        // ── Shared DSP helpers ──────────────────────────────────────

        /// <summary>ADSR envelope returning amplitude at normalized time position.</summary>
        private static float ADSR(float tNorm, float attack, float decay, float sustain, float release)
        {
            if (tNorm < attack)
                return tNorm / attack;
            float afterAttack = tNorm - attack;
            float decayLen = decay;
            if (afterAttack < decayLen)
                return 1f - (1f - sustain) * (afterAttack / decayLen);
            float sustainEnd = 1f - release;
            if (tNorm < sustainEnd)
                return sustain;
            return sustain * (1f - (tNorm - sustainEnd) / release);
        }

        /// <summary>Multi-tap delay reverb with 4 taps at prime-number offsets.</summary>
        private static void ApplyReverb(float[] data, int sr)
        {
            int tap1 = sr * 23 / 1000; // ~23ms
            int tap2 = sr * 37 / 1000; // ~37ms
            int tap3 = sr * 53 / 1000; // ~53ms
            int tap4 = sr * 71 / 1000; // ~71ms
            int n = data.Length;

            // Work on a copy to avoid feedback within the same pass
            float[] dry = new float[n];
            System.Array.Copy(data, dry, n);

            for (int i = 0; i < n; i++)
            {
                if (i >= tap1) data[i] += dry[i - tap1] * 0.25f;
                if (i >= tap2) data[i] += dry[i - tap2] * 0.18f;
                if (i >= tap3) data[i] += dry[i - tap3] * 0.12f;
                if (i >= tap4) data[i] += dry[i - tap4] * 0.08f;
            }
        }

        /// <summary>One-pole IIR low-pass filter.</summary>
        private static void LowPassFilter(float[] data, float cutoff, int sr)
        {
            float rc = 1f / (2f * Mathf.PI * cutoff);
            float dt = 1f / sr;
            float alpha = dt / (rc + dt);
            float prev = data[0];
            for (int i = 1; i < data.Length; i++)
            {
                prev = alpha * data[i] + (1f - alpha) * prev;
                data[i] = prev;
            }
        }

        /// <summary>Chorus effect — mixes signal with LFO-modulated delayed copy.</summary>
        private static void ApplyChorus(float[] data, int sr, float depth, float rate)
        {
            int n = data.Length;
            float[] dry = new float[n];
            System.Array.Copy(data, dry, n);

            int maxDelay = (int)(depth * sr) + 1;
            for (int i = maxDelay; i < n; i++)
            {
                float t = (float)i / sr;
                float lfo = (Mathf.Sin(2f * Mathf.PI * rate * t) + 1f) * 0.5f; // 0..1
                float delaySamples = lfo * depth * sr;
                int d0 = (int)delaySamples;
                float frac = delaySamples - d0;
                int idx0 = i - d0;
                int idx1 = i - d0 - 1;
                if (idx0 >= 0 && idx1 >= 0)
                {
                    float delayed = dry[idx0] * (1f - frac) + dry[idx1] * frac;
                    data[i] = dry[i] * 0.7f + delayed * 0.3f;
                }
            }
        }

        // ── Procedural clip generation — Pretty SFX ───────────────

        private void GenerateClips()
        {
            _clips = new AudioClip[System.Enum.GetValues(typeof(SoundType)).Length];
            int sr = 44100;

            _clips[(int)SoundType.ButtonClick] = GenClick(sr);
            _clips[(int)SoundType.CardDeal] = GenCardDeal(sr);
            _clips[(int)SoundType.CardFlip] = GenCardFlip(sr);
            _clips[(int)SoundType.ChipClink] = GenChipClink(sr);
            _clips[(int)SoundType.WinnerFanfare] = GenFanfare(sr);
            _clips[(int)SoundType.Shuffle] = GenShuffle(sr);
            _clips[(int)SoundType.CommunityCardReveal] = GenCommunityReveal(sr);
            _clips[(int)SoundType.FoldSwoosh] = GenFoldSwoosh(sr);
            _clips[(int)SoundType.Sparkle] = GenSparkle(sr);
            _clips[(int)SoundType.RingPulse] = GenRingPulse(sr);
            _clips[(int)SoundType.AllIn] = GenAllIn(sr);
            _clips[(int)SoundType.Starburst] = GenStarburst(sr);
            _clips[(int)SoundType.TimerWarning] = GenTimerWarning(sr);
        }

        /// <summary>"Soft Tap" — Layered 1200Hz + 1800Hz + 2400Hz harmonic with ADSR (40ms).</summary>
        private static AudioClip GenClick(int sr)
        {
            int n = sr * 40 / 1000;
            var clip = AudioClip.Create("Click", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = ADSR(tNorm, 0.05f, 0.25f, 0.3f, 0.7f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.5f
                           + Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.15f;
                // Softer sub-bass with smooth rolloff
                float subEnv = Mathf.Exp(-tNorm * 8f);
                float subBass = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.3f * subEnv;
                d[i] = (tone + subBass) * env * 0.3f;
            }
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Silky Swoosh" — LPF noise with tonal attack + multi-tap reverb (100ms).</summary>
        private static AudioClip GenCardDeal(int sr)
        {
            int n = sr * 100 / 1000;
            var clip = AudioClip.Create("CardDeal", n, 1, sr, false);
            var d = new float[n];
            var rng = new System.Random(42);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = ADSR(tNorm, 0.08f, 0.3f, 0.4f, 0.62f);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float toneAttack = tNorm < 0.15f ? Mathf.Sin(2f * Mathf.PI * 2000f * t) * (1f - tNorm / 0.15f) * 0.3f : 0f;
                d[i] = (noise * 0.6f + toneAttack) * env * 0.2f;
            }
            LowPassFilter(d, 3000f, sr);
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Crystal Snap" — Detuned harmonics with noise crackle + reverb (120ms).</summary>
        private static AudioClip GenCardFlip(int sr)
        {
            int n = sr * 120 / 1000;
            var clip = AudioClip.Create("CardFlip", n, 1, sr, false);
            var d = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = Mathf.Pow(1f - tNorm, 3f);
                // Detuned pairs for richness
                float tone = Mathf.Sin(2f * Mathf.PI * 1798f * t) * 0.25f
                           + Mathf.Sin(2f * Mathf.PI * 1802f * t) * 0.25f
                           + Mathf.Sin(2f * Mathf.PI * 3598f * t) * 0.12f
                           + Mathf.Sin(2f * Mathf.PI * 3602f * t) * 0.12f
                           + Mathf.Sin(2f * Mathf.PI * 2700f * t) * 0.15f;
                float noiseCrackle = tNorm < 0.06f
                    ? ((float)rng.NextDouble() * 2f - 1f) * 0.15f * (1f - tNorm / 0.06f)
                    : 0f;
                d[i] = (tone + noiseCrackle) * env * 0.25f;
            }
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Glass Chime" — C6+E6+G6+B6 chord with vibrato, chorus + extended tail (250ms).</summary>
        private static AudioClip GenChipClink(int sr)
        {
            int n = sr * 250 / 1000;
            var clip = AudioClip.Create("ChipClink", n, 1, sr, false);
            var d = new float[n];
            float[] freqs = { 1047f, 1319f, 1568f, 1976f }; // C6, E6, G6, B6 (5th harmonic)
            int[] offsets = { 0, sr * 5 / 1000, sr * 10 / 1000, sr * 14 / 1000 };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float globalEnv = Mathf.Exp(-tNorm * 2.5f);
                float sample = 0f;
                for (int j = 0; j < freqs.Length; j++)
                {
                    if (i < offsets[j]) continue;
                    float localT = (float)(i - offsets[j]) / sr;
                    float localEnv = Mathf.Exp(-(float)(i - offsets[j]) / n * 3.5f);
                    // Slightly increased vibrato depth
                    float vibrato = 1f + Mathf.Sin(2f * Mathf.PI * 6f * localT) * 0.004f;
                    float amp = j < 3 ? 0.3f : 0.15f;
                    sample += Mathf.Sin(2f * Mathf.PI * freqs[j] * vibrato * localT) * localEnv * amp;
                }
                d[i] = sample * globalEnv * 0.2f;
            }
            ApplyChorus(d, sr, 0.003f, 1.5f);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Celebration Chime" — Chord progression with triangle pad + warm reverb (1200ms).</summary>
        private static AudioClip GenFanfare(int sr)
        {
            int n = sr * 1200 / 1000;
            var clip = AudioClip.Create("Fanfare", n, 1, sr, false);
            var d = new float[n];

            // Chord 1: C5-E5-G5 (0-200ms)
            // Chord 2: G5-B5-D6 (200-400ms)
            // Chord 3: C6 resolution (400-700ms with sustain)
            float[][] chords = {
                new[] { 523.25f, 659.25f, 783.99f },
                new[] { 783.99f, 987.77f, 1174.66f },
                new[] { 1046.50f, 1046.50f * 2f }
            };
            int[] starts = { 0, sr * 200 / 1000, sr * 400 / 1000 };
            int[] lengths = { sr * 200 / 1000, sr * 200 / 1000, sr * 300 / 1000 };

            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < lengths[c]; i++)
                {
                    int idx = starts[c] + i;
                    if (idx >= n) break;
                    float t = (float)i / sr;
                    float localEnv = 1f - (float)i / lengths[c] * 0.6f;
                    float globalEnv = 1f - (float)idx / n * 0.3f;
                    float sample = 0f;
                    foreach (float freq in chords[c])
                    {
                        sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f;
                        sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f; // octave harmonic
                    }
                    sample /= chords[c].Length;
                    d[idx] += sample * localEnv * globalEnv * 0.2f;
                }
            }

            // Triangle wave pad layer underneath chords for warmth
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < lengths[c]; i++)
                {
                    int idx = starts[c] + i;
                    if (idx >= n) break;
                    float t = (float)i / sr;
                    float padEnv = ADSR((float)i / lengths[c], 0.15f, 0.2f, 0.6f, 0.65f);
                    float pad = 0f;
                    foreach (float freq in chords[c])
                    {
                        // Triangle wave: approximate with odd harmonics
                        float phase = freq * t;
                        pad += Mathf.Sin(2f * Mathf.PI * phase) * 1f;
                        pad -= Mathf.Sin(2f * Mathf.PI * phase * 3f) / 9f;
                        pad += Mathf.Sin(2f * Mathf.PI * phase * 5f) / 25f;
                    }
                    pad /= chords[c].Length;
                    d[idx] += pad * padEnv * 0.06f;
                }
            }

            // Shimmer layer: high-frequency sweep (extended)
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float shimmerFreq = Mathf.Lerp(8000f, 12000f, tNorm);
                float shimmerEnv = Mathf.Sin(Mathf.PI * tNorm) * 1.2f;
                d[i] += Mathf.Sin(2f * Mathf.PI * shimmerFreq * t) * shimmerEnv * 0.05f * 0.15f;
            }

            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Soft Riffle" — LPF noise + doubled micro-clicks with velocity variation (500ms).</summary>
        private static AudioClip GenShuffle(int sr)
        {
            int n = sr * 500 / 1000;
            var clip = AudioClip.Create("Shuffle", n, 1, sr, false);
            var d = new float[n];
            var rng = new System.Random(33);

            // Doubled micro-clicks (18 rapid blips) with velocity variation
            int clickCount = 18;
            int clickSpacing = sr * 15 / 1000;
            int clickLen = sr * 3 / 1000;
            for (int c = 0; c < clickCount; c++)
            {
                int start = sr * 50 / 1000 + c * clickSpacing;
                float velocity = 0.1f + (float)rng.NextDouble() * 0.12f;
                for (int i = 0; i < clickLen && start + i < n; i++)
                {
                    float t = (float)i / sr;
                    float env = 1f - (float)i / clickLen;
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * 1500f * t) * env * velocity;
                }
            }

            // Noise layer
            var noiseData = new float[n];
            for (int i = 0; i < n; i++)
            {
                float tNorm = (float)i / n;
                float env = Mathf.Sin(Mathf.PI * tNorm);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float centerFreq = 500f + 1500f * Mathf.Sin(Mathf.PI * tNorm);
                float t = (float)i / sr;
                float filtered = noise * Mathf.Sin(2f * Mathf.PI * centerFreq * t);
                noiseData[i] = filtered * env * 0.08f;
            }
            LowPassFilter(noiseData, 4000f, sr);
            for (int i = 0; i < n; i++)
                d[i] += noiseData[i];

            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Bright Reveal" — Wide sweep (600→2000Hz) + parallel high chime + reverb (120ms).</summary>
        private static AudioClip GenCommunityReveal(int sr)
        {
            int n = sr * 120 / 1000;
            var clip = AudioClip.Create("CommunityReveal", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;

                // Wider upward sweep 600->2000Hz
                float sweepT = Mathf.Clamp01(tNorm / 0.5f);
                float sweepFreq = Mathf.Lerp(600f, 2000f, sweepT);
                float sweepEnv = Mathf.Pow(1f - sweepT, 2f);
                float sweep = Mathf.Sin(2f * Mathf.PI * sweepFreq * t) * sweepEnv * 0.3f;

                // Bright chime harmonics + parallel high chime
                float chimeEnv = Mathf.Pow(1f - tNorm, 2.5f);
                float chime = (Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.12f
                             + Mathf.Sin(2f * Mathf.PI * 3600f * t) * 0.08f
                             + Mathf.Sin(2f * Mathf.PI * 4800f * t) * 0.05f) * chimeEnv;

                // Sub-bass impact (first 15ms)
                float subEnv = tNorm < 0.125f ? (1f - tNorm / 0.125f) : 0f;
                float sub = Mathf.Sin(2f * Mathf.PI * 100f * t) * subEnv * 0.25f;

                d[i] = (sweep + chime + sub) * 0.3f;
            }
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Gentle Whoosh" — LPF sweep noise with tonal undertone + reverb (150ms).</summary>
        private static AudioClip GenFoldSwoosh(int sr)
        {
            int n = sr * 150 / 1000;
            var clip = AudioClip.Create("FoldSwoosh", n, 1, sr, false);
            var d = new float[n];
            var rng = new System.Random(21);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * tNorm), 1.5f) * (1f - tNorm * 0.3f);
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float sweepFreq = Mathf.Lerp(600f, 150f, tNorm);
                float sweep = noise * Mathf.Sin(2f * Mathf.PI * sweepFreq * t);
                // Gentle tonal undertone
                float tonal = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(500f, 250f, tNorm) * t) * 0.15f;
                d[i] = (sweep * 0.7f + tonal) * env * 0.12f;
            }
            LowPassFilter(d, 2500f, sr);
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Tiny Chime" — Detuned pairs with chorus, extended to 80ms.</summary>
        private static AudioClip GenSparkle(int sr)
        {
            int n = sr * 80 / 1000;
            var clip = AudioClip.Create("Sparkle", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = ADSR(tNorm, 0.05f, 0.3f, 0.4f, 0.65f);
                // Detuned pairs for shimmer
                d[i] = (Mathf.Sin(2f * Mathf.PI * 3998f * t) * 0.15f
                       + Mathf.Sin(2f * Mathf.PI * 4002f * t) * 0.15f
                       + Mathf.Sin(2f * Mathf.PI * 5998f * t) * 0.10f
                       + Mathf.Sin(2f * Mathf.PI * 6002f * t) * 0.10f
                       + Mathf.Sin(2f * Mathf.PI * 7998f * t) * 0.05f
                       + Mathf.Sin(2f * Mathf.PI * 8002f * t) * 0.05f) * env * 0.12f;
            }
            ApplyChorus(d, sr, 0.002f, 2f);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Soft Resonance" — Richer overtone series with 5th/7th harmonics + chorus (300ms).</summary>
        private static AudioClip GenRingPulse(int sr)
        {
            int n = sr * 300 / 1000;
            var clip = AudioClip.Create("RingPulse", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = Mathf.Sin(Mathf.PI * tNorm) * Mathf.Exp(-tNorm * 2f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * 400f * t) * 0.45f
                       + Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.25f
                       + Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.12f
                       + Mathf.Sin(2f * Mathf.PI * 2000f * t) * 0.08f   // 5th harmonic
                       + Mathf.Sin(2f * Mathf.PI * 2800f * t) * 0.05f)  // 7th harmonic
                       * env * 0.15f;
            }
            ApplyChorus(d, sr, 0.003f, 1.2f);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Impact Chime" — Deeper sub (80Hz) + louder impact + reverb on tail (250ms).</summary>
        private static AudioClip GenAllIn(int sr)
        {
            int n = sr * 250 / 1000;
            var clip = AudioClip.Create("AllIn", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;

                // Deeper low impact at 80Hz (first 30ms)
                float impactEnv = tNorm < 0.12f ? (1f - tNorm / 0.12f) : 0f;
                float impact = Mathf.Sin(2f * Mathf.PI * 80f * t) * impactEnv * 0.55f;

                // Rising chime (C5->C6 over 100ms)
                float chimeT = Mathf.Clamp01(tNorm / 0.4f);
                float chimeFreq = Mathf.Lerp(523.25f, 1046.5f, chimeT);
                float chimeEnv = Mathf.Clamp01(1f - tNorm * 1.5f);
                float chime = Mathf.Sin(2f * Mathf.PI * chimeFreq * t) * chimeEnv * 0.25f;

                // High sustain
                float sustainEnv = Mathf.Exp(-tNorm * 3f);
                float sustain = Mathf.Sin(2f * Mathf.PI * 2000f * t) * sustainEnv * 0.1f;

                d[i] = (impact + chime + sustain) * 0.28f;
            }
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Sparkle Burst" — Pentatonic arpeggio with per-note reverb + detuning (150ms).</summary>
        private static AudioClip GenStarburst(int sr)
        {
            int n = sr * 150 / 1000;
            var clip = AudioClip.Create("Starburst", n, 1, sr, false);
            var d = new float[n];
            // Pentatonic scale: C6, D6, E6, G6, A6
            float[] freqs = { 2093f, 2349f, 2637f, 3136f, 3520f };
            int noteLen = sr * 18 / 1000;
            int noteGap = sr * 4 / 1000;
            for (int note = 0; note < freqs.Length; note++)
            {
                int start = note * (noteLen - noteGap);
                float detune = 1.5f; // Hz offset for detuning
                for (int i = 0; i < noteLen; i++)
                {
                    int idx = start + i;
                    if (idx >= n) break;
                    float t = (float)i / sr;
                    float env = Mathf.Exp(-(float)i / noteLen * 3f);
                    // Detuned pair for richness
                    d[idx] += (Mathf.Sin(2f * Mathf.PI * (freqs[note] - detune) * t) * 0.06f
                             + Mathf.Sin(2f * Mathf.PI * (freqs[note] + detune) * t) * 0.06f)
                             * env;
                }
            }
            ApplyReverb(d, sr);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>"Soft Tick" — Short gentle tick for timer warning (~30ms).</summary>
        private static AudioClip GenTimerWarning(int sr)
        {
            int n = sr * 30 / 1000;
            var clip = AudioClip.Create("TimerWarning", n, 1, sr, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float tNorm = (float)i / n;
                float env = Mathf.Pow(1f - tNorm, 4f);
                d[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * env * 0.12f;
            }
            clip.SetData(d, 0);
            return clip;
        }
    }
}
