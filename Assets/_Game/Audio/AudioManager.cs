using UnityEngine;

namespace CivVSCiv
{
    /// <summary>
    /// Generates all game audio procedurally at runtime — no external audio files needed.
    /// Creates AudioClips via AudioClip.Create + SetData with synthesized waveforms.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private AudioSource _source;

        [Header("Volume")]
        [SerializeField] private float _masterVolume = 0.5f;
        [SerializeField] private float _sfxVolume = 0.8f;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D sound
        }

        // ----------------------------------------------------------------
        // Public sound triggers
        // ----------------------------------------------------------------

        /// <summary>Short high beep for UI clicks and unit selection.</summary>
        public void PlayUIClick()
        {
            var clip = CreateTone(440f, 0.05f, 0.4f);
            _source.PlayOneShot(clip, _sfxVolume * _masterVolume);
            Destroy(clip, clip.length + 0.1f);
        }

        /// <summary>Quick whoosh for unit movement.</summary>
        public void PlayUnitMove()
        {
            var clip = CreateSweep(300f, 600f, 0.12f, 0.3f);
            _source.PlayOneShot(clip, _sfxVolume * _masterVolume);
            Destroy(clip, clip.length + 0.1f);
        }

        /// <summary>Low rumble and clash for combat.</summary>
        public void PlayCombat()
        {
            // Mix: noise burst + low tone
            var noiseClip = CreateNoise(0.25f, 0.5f);
            var toneClip = CreateTone(80f, 0.25f, 0.6f);
            _source.PlayOneShot(noiseClip, _sfxVolume * _masterVolume * 0.4f);
            _source.PlayOneShot(toneClip, _sfxVolume * _masterVolume * 0.5f);
            Destroy(noiseClip, noiseClip.length + 0.1f);
            Destroy(toneClip, toneClip.length + 0.1f);
        }

        /// <summary>Ascending tones for city construction.</summary>
        public void PlayCityBuild()
        {
            var clip = CreateSweep(350f, 700f, 0.3f, 0.4f);
            _source.PlayOneShot(clip, _sfxVolume * _masterVolume);
            Destroy(clip, clip.length + 0.1f);
        }

        /// <summary>Triumphant fanfare for victory.</summary>
        public void PlayVictory()
        {
            // C major chord: C4 (262Hz), E4 (330Hz), G4 (392Hz)
            float[] chord = { 262f, 330f, 392f };
            var clip = CreateChord(chord, 0.8f, 0.35f);
            _source.PlayOneShot(clip, _sfxVolume * _masterVolume);
            Destroy(clip, clip.length + 0.1f);
        }

        /// <summary>Soft chime for turn start.</summary>
        public void PlayTurnStart()
        {
            var clip = CreateTone(500f, 0.15f, 0.3f);
            _source.PlayOneShot(clip, _sfxVolume * _masterVolume);
            Destroy(clip, clip.length + 0.1f);
        }

        // ----------------------------------------------------------------
        // Procedural audio generators
        // ----------------------------------------------------------------

        /// <summary>Creates a pure sine wave tone.</summary>
        private static AudioClip CreateTone(float frequency, float duration, float amplitude)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Apply a quick fade-in/out to avoid clicks
                float envelope = 1f;
                float fadeLen = Mathf.Min(0.005f, duration * 0.1f);
                if (t < fadeLen)
                    envelope = t / fadeLen;
                else if (t > duration - fadeLen)
                    envelope = (duration - t) / fadeLen;

                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
            }

            var clip = AudioClip.Create("Tone_" + frequency, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Creates a white noise burst.</summary>
        private static AudioClip CreateNoise(float duration, float amplitude)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f;
                float fadeLen = Mathf.Min(0.005f, duration * 0.1f);
                if (t < fadeLen)
                    envelope = t / fadeLen;
                else if (t > duration - fadeLen)
                    envelope = (duration - t) / fadeLen;

                samples[i] = (Random.value * 2f - 1f) * amplitude * envelope;
            }

            var clip = AudioClip.Create("Noise", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Creates a frequency sweep (e.g. whoosh effect).</summary>
        private static AudioClip CreateSweep(float startFreq, float endFreq, float duration, float amplitude)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);

                float envelope = 1f;
                float fadeLen = Mathf.Min(0.008f, duration * 0.15f);
                if (t < fadeLen)
                    envelope = t / fadeLen;
                else if (t > duration - fadeLen)
                    envelope = (duration - t) / fadeLen;

                phase += 2f * Mathf.PI * freq / sampleRate;
                samples[i] = Mathf.Sin(phase) * amplitude * envelope;
            }

            var clip = AudioClip.Create("Sweep_" + startFreq + "_" + endFreq, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Creates a chord from multiple frequencies mixed together.</summary>
        private static AudioClip CreateChord(float[] frequencies, float duration, float amplitude)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float invCount = 1f / frequencies.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f;
                float fadeLen = Mathf.Min(0.01f, duration * 0.1f);
                if (t < fadeLen)
                    envelope = t / fadeLen;
                else if (t > duration - fadeLen)
                    envelope = (duration - t) / fadeLen;

                float sample = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t);
                }
                samples[i] = sample * invCount * amplitude * envelope;
            }

            var clip = AudioClip.Create("Chord", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
