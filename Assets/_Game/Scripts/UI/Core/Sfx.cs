using System;
using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.UI
{
    /// <summary>
    /// Placeholder sound effects synthesised at start-up (ringtone, vibration, message pop, typing,
    /// heartbeat, timer tick...), so the UI templates give feedback before the real SFX exist.
    /// Replace a clip by assigning it in the inspector under the same name.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public const string Click = "click";
        public const string Ring = "ring";
        public const string Buzz = "buzz";
        public const string Pop = "pop";
        public const string Type = "type";
        public const string Answer = "answer";
        public const string HangUp = "hangup";
        public const string Heart = "heart";
        public const string Tick = "tick";
        public const string Paper = "paper";
        public const string Success = "success";
        public const string Error = "error";
        public const string Whoosh = "whoosh";
        public const string Stamp = "stamp";

        [Serializable]
        public struct Override
        {
            public string name;
            public AudioClip clip;
        }

        [SerializeField] List<Override> overrides = new List<Override>();
        [SerializeField, Range(0f, 1f)] float volume = 0.6f;

        static Sfx instance;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource oneShot;
        AudioSource loop;
        const int Rate = 44100;
        System.Random noise = new System.Random(7);

        void Awake()
        {
            instance = this;
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            loop = gameObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            Build();
            foreach (var o in overrides)
                if (o.clip != null && !string.IsNullOrEmpty(o.name))
                    clips[o.name] = o.clip;
        }

        public static void Play(string name, float gain = 1f)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip))
                return;
            instance.oneShot.PlayOneShot(clip, gain * instance.volume);
        }

        public static void Loop(string name, float gain = 1f)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip))
                return;
            if (instance.loop.clip == clip && instance.loop.isPlaying)
                return;
            instance.loop.clip = clip;
            instance.loop.volume = gain * instance.volume;
            instance.loop.Play();
        }

        public static void StopLoop()
        {
            if (instance != null)
                instance.loop.Stop();
        }

        // ---------------------------------------------------------------- synthesis

        void Build()
        {
            clips[Click] = Make(0.06f, t => Sine(t, 1500) * Env(t, 0.001f, 0.05f) * 0.25f + Noise() * Env(t, 0.001f, 0.012f) * 0.15f);
            clips[Pop] = Make(0.12f, t => Sine(t, 700 + 3000 * t) * Env(t, 0.004f, 0.1f) * 0.45f);
            clips[Type] = Make(0.03f, t => Noise() * Env(t, 0.001f, 0.02f) * 0.18f);
            clips[Tick] = Make(0.05f, t => Sine(t, 1250) * Env(t, 0.001f, 0.04f) * 0.35f);
            clips[Answer] = Make(0.32f, t => (t < 0.14f ? Sine(t, 660) : Sine(t, 990)) * Env(t, 0.005f, 0.3f) * 0.4f);
            clips[Success] = Make(0.42f, t => Sine(t, new[] { 523f, 659f, 784f, 1047f }[Mathf.Min(3, (int)(t / 0.09f))]) * Env(t, 0.004f, 0.4f) * 0.35f);
            clips[Error] = Make(0.34f, t => Square(t, t < 0.15f ? 300 : 220) * Env(t, 0.004f, 0.32f) * 0.18f);
            clips[Whoosh] = Make(0.28f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.28f) * 0.18f);
            clips[Paper] = Make(0.34f, t => Noise() * Mathf.Pow(Mathf.Sin(Mathf.PI * t / 0.34f), 2) * (0.5f + 0.5f * Mathf.Sin(t * 90f)) * 0.22f);
            clips[HangUp] = Make(0.9f, t => (t % 0.3f < 0.18f ? Sine(t, 480) : 0f) * 0.3f);
            clips[Heart] = Make(0.95f, t => Thump(t) + Thump(t - 0.24f) * 0.8f);
            clips[Stamp] = Make(0.28f, t => Thump(t) * 0.8f + Noise() * Env(t, 0.001f, 0.06f) * 0.4f);
            clips[Buzz] = Make(0.8f, t => t < 0.45f ? Saw(t, 150) * (0.6f + 0.4f * Mathf.Sin(t * 190f)) * 0.16f : 0f);
            // A cheerful retro ringtone: square-ish melody, then a pause.
            float[] notes = { 659f, 784f, 880f, 784f, 659f, 587f, 659f, 0f };
            clips[Ring] = Make(2.2f, t =>
            {
                int i = (int)(t / 0.16f);
                if (i >= notes.Length || notes[i] <= 0f)
                    return 0f;
                float local = t - i * 0.16f;
                float n = notes[i];
                return (Sine(t, n) * 0.7f + Sine(t, n * 3f) * 0.18f + Square(t, n) * 0.08f) * Env(local, 0.005f, 0.15f) * 0.42f
                       + Saw(t, 150) * 0.03f;
            });
        }

        AudioClip Make(float seconds, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Square(float t, float hz) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz * t));
        static float Saw(float t, float hz) => 2f * (t * hz - Mathf.Floor(t * hz + 0.5f));
        float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

        static float Env(float t, float attack, float release)
        {
            if (t < 0f)
                return 0f;
            if (t < attack)
                return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / Mathf.Max(release, 1e-4f));
        }

        static float Thump(float t)
        {
            if (t < 0f)
                return 0f;
            return Mathf.Sin(2f * Mathf.PI * (52f + 40f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 16f) * 0.9f;
        }
    }
}
