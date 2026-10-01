using System;
using System.Collections.Generic;
using DontCallMe.Audio;
using UnityEngine;

namespace DontCallMe.UI
{
    /// <summary>
    /// Placeholder sound effects synthesised at start-up (ringtone, vibration, message pop, typing,
    /// heartbeat, timer tick...), so the UI templates give feedback before the real SFX exist.
    /// Replace a clip by assigning it in the inspector under the same name. One-shots keep playing
    /// while the game is paused (menu clicks); the loop (the ringtone) pauses with it.
    /// Both follow the SFX volume setting.
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
        public const string Suspense = "suspense";
        public const string Roll = "roll";
        public const string Reveal = "reveal";
        public const string RevealGood = "reveal_good";
        public const string RevealBad = "reveal_bad";

        /// <summary>How long the suspense clip (the long drumroll) builds before the reveal lands on its last sample.</summary>
        public const float SuspenseSeconds = 3.2f;
        /// <summary>How long the short drumroll before the verdict runs.</summary>
        public const float RollSeconds = 0.95f;
        /// <summary>When the heartbeats under the long drumroll fall (seconds): closer and closer together.</summary>
        static readonly float[] SuspenseBeats = { 0.1f, 0.9f, 1.58f, 2.14f, 2.58f, 2.9f };

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
        float loopGain = 1f;
        const int Rate = 44100;
        System.Random noise = new System.Random(7);

        void Awake()
        {
            instance = this;
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            oneShot.ignoreListenerPause = true;
            loop = gameObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            GameSettings.Changed += ApplyLoopVolume;
            Build();
            foreach (var o in overrides)
                if (o.clip != null && !string.IsNullOrEmpty(o.name))
                    clips[o.name] = o.clip;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyLoopVolume;
            if (instance == this)
                instance = null;
        }

        void ApplyLoopVolume() => loop.volume = loopGain * volume * GameSettings.Sfx;

        public static void Play(string name, float gain = 1f)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip))
                return;
            instance.oneShot.PlayOneShot(clip, gain * instance.volume * GameSettings.Sfx);
        }

        public static void Loop(string name, float gain = 1f)
        {
            if (instance == null || !instance.clips.TryGetValue(name, out var clip))
                return;
            if (instance.loop.clip == clip && instance.loop.isPlaying)
                return;
            instance.loop.clip = clip;
            instance.loopGain = gain;
            instance.ApplyLoopVolume();
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
            BuildReveal();
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

        /// <summary>
        /// The next morning's reveal: a snare drumroll that quickens and swells over a low drone and
        /// a heartbeat and cuts off, the hit when the truth is stamped (a boom, a rimshot and a
        /// cymbal), a short roll before the verdict, and a bright or a sinking phrase for a right or
        /// a wrong call.
        /// </summary>
        void BuildReveal()
        {
            const float len = SuspenseSeconds;
            var longRoll = Snare(len, 14f, 30f, 0.1f, 1f, 1.6f);
            clips[Suspense] = Make(len, t =>
            {
                float rise = t / len;
                // Two low voices a little apart, so the drone throbs; it swells to the end.
                float drone = (Sine(t, 55f) + Sine(t, 58.3f) * 0.8f) * (0.04f + 0.11f * rise * rise);
                float heart = 0f;
                foreach (float b in SuspenseBeats)
                    heart += Beat(t - b) + Beat(t - b - 0.17f) * 0.7f;
                // A clean cut just before the hit.
                float cut = Mathf.Clamp01((len - t) / 0.03f);
                return (longRoll(t) * 0.75f + drone + heart * 0.26f) * cut;
            });
            var shortRoll = Snare(RollSeconds, 22f, 32f, 0.3f, 1f, 1.3f);
            clips[Roll] = Make(RollSeconds, t => shortRoll(t) * 0.6f * Mathf.Clamp01((RollSeconds - t) / 0.03f));
            var rim = HighNoise(0.16f);
            var cymbal = HighNoise(0.55f);
            clips[Reveal] = Make(1.8f, t =>
                Mathf.Sin(2f * Mathf.PI * (38f + 64f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 3.4f) * 0.5f
                + rim() * Env(t, 0.001f, 0.12f) * 0.34f
                + cymbal() * Mathf.Exp(-t * 3f) * 0.15f
                + (Sine(t, 110f) + Sine(t, 164.8f) * 0.6f) * Mathf.Exp(-t * 2.4f) * 0.06f);
            float[] bright = { 523.25f, 659.25f, 783.99f, 1046.5f };
            clips[RevealGood] = Make(1.7f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < bright.Length; i++)
                {
                    float u = t - i * 0.08f;
                    if (u >= 0f)
                        sum += (Sine(u, bright[i]) + Sine(u, bright[i] * 2f) * 0.22f) * Mathf.Exp(-u * 3f) * Mathf.Min(1f, u / 0.004f);
                }
                return sum * 0.19f;
            });
            clips[RevealBad] = Make(1.9f, t =>
            {
                float u = t - 0.26f;
                float first = Low(t, 174.6f) * Env(t, 0.006f, 0.3f);
                float second = u >= 0f ? Low(u, 123.5f) * Mathf.Exp(-u * 1.9f) * Mathf.Min(1f, u / 0.006f) : 0f;
                return (first * 0.8f + second) * 0.24f + Thump(t) * 0.45f + Thump(u) * 0.55f;
            });
        }

        /// <summary>
        /// A snare roll, sample by sample: the strokes go from <paramref name="fromRate"/> to
        /// <paramref name="toRate"/> a second, hands alternating, and the level from
        /// <paramref name="fromLevel"/> to <paramref name="toLevel"/>. Call it with rising times.
        /// </summary>
        Func<float, float> Snare(float seconds, float fromRate, float toRate, float fromLevel, float toLevel, float curve)
        {
            var wires = HighNoise(0.16f);
            return t =>
            {
                float rise = Mathf.Clamp01(t / seconds);
                float rate = Mathf.Lerp(fromRate, toRate, rise);
                // How many strokes so far (the rate's integral), and the time since the last one.
                float strokes = fromRate * t + (toRate - fromRate) * t * t / (2f * seconds);
                float since = (strokes - Mathf.Floor(strokes)) / rate;
                float hand = ((int)strokes & 1) == 0 ? 1f : 0.8f;
                float head = Mathf.Sin(2f * Mathf.PI * 185f * since) * Mathf.Exp(-since * 65f);
                float stroke = (wires() * 0.5f + head * 0.6f) * Mathf.Exp(-since * 50f) * hand;
                return stroke * Mathf.Lerp(fromLevel, toLevel, Mathf.Pow(rise, curve));
            };
        }

        /// <summary>Noise with the lows taken out (snare wires, a cymbal): the higher <paramref name="amount"/>, the thinner.</summary>
        Func<float> HighNoise(float amount)
        {
            float low = 0f;
            return () =>
            {
                float n = Noise();
                low += (n - low) * amount;
                return n - low;
            };
        }

        /// <summary>One half of a heartbeat: the low thump with a little body above it, so small speakers carry it.</summary>
        static float Beat(float t)
        {
            if (t < 0f)
                return 0f;
            return Thump(t) + Mathf.Sin(2f * Mathf.PI * (104f + 70f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 20f) * 0.32f;
        }

        /// <summary>A dull low voice with a second one slightly off, for the sinking phrase.</summary>
        static float Low(float t, float hz) => Sine(t, hz) + Sine(t, hz * 1.008f) * 0.55f + Sine(t, hz * 3f) * 0.2f + Sine(t, hz * 5f) * 0.07f;

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
