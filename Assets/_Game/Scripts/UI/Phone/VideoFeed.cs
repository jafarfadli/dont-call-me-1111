using System;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The caller's face on a video call (<see cref="VideoCall"/>): pixel frames that blink, move
    /// their lips while the caller speaks and wave a hand in front of the face when asked. A faked
    /// face gives itself away to a careful eye: its lips trail the voice by <see cref="FakeLag"/>
    /// seconds, its edges shimmer now and then, and a hand passing in front of it does not cover it.
    /// <see cref="Face"/> is the region over the face, which the call screen makes circleable.
    /// </summary>
    public class VideoFeed
    {
        /// <summary>How far a faked face's lips trail the voice, in seconds.</summary>
        public const float FakeLag = 0.6f;
        /// <summary>A wave: the hand crosses the face and back, twice.</summary>
        public const float WaveSeconds = 2.8f;
        static readonly int[] WaveFrames = { 1, 2, 3, 2, 1, 2, 3, 2, 1 };

        public VisualElement Root { get; }
        public VisualElement Face { get; }

        /// <summary>The wave is over; its telling frame (<see cref="WaveFrame"/>), for the notebook.</summary>
        public event Action<string> Waved;

        readonly VideoCall video;
        float talkFrom = -1f, talkTo = -1f;
        float waveFrom = -1f;
        bool waveOver = true;
        float nextBlink, blinkUntil;
        float nextGlitch, glitchUntil;
        string shown;

        public VideoFeed(VideoCall video)
        {
            this.video = video;
            Root = UIKit.Div("video");
            Root.pickingMode = PickingMode.Ignore;
            Face = UIKit.Div("video__face");
            Root.Add(Face);
            nextBlink = Time.time + 2f;
            nextGlitch = Time.time + 3f;
            Show(video.face);
            Root.schedule.Execute(Tick).Every(40);
        }

        bool Faked => video.faked;

        /// <summary>The wave's telling frame: the hand halfway across the face (covering it, or vanishing behind a pasted one).</summary>
        public string WaveFrame => video.face + (Faked ? "_fake_wave1" : "_wave1");

        /// <summary>The caller starts a line that lasts <paramref name="seconds"/>.</summary>
        public void Talk(float seconds)
        {
            float lag = Faked ? FakeLag : 0f;
            talkFrom = Time.time + lag;
            talkTo = Time.time + Mathf.Max(0.4f, seconds) + lag;
        }

        /// <summary>The voice stopped (the line ended or was skipped): the lips stop too, a faked face late.</summary>
        public void Hush()
        {
            float end = Time.time + (Faked ? FakeLag : 0f);
            if (talkTo > end)
                talkTo = end;
        }

        /// <summary>Something the caller does on camera (<see cref="ConvLine.act"/>).</summary>
        public void Act(string act)
        {
            if (act != "wave")
                return;
            waveFrom = Time.time + 0.3f;
            waveOver = false;
        }

        void Tick()
        {
            float t = Time.time;
            string frame = video.face;
            if (!waveOver && t >= waveFrom && t < waveFrom + WaveSeconds)
            {
                int i = WaveFrames[Mathf.Min(WaveFrames.Length - 1, (int)((t - waveFrom) / WaveSeconds * WaveFrames.Length))];
                frame = video.face + (Faked ? "_fake_wave" : "_wave") + i;
            }
            else
            {
                if (!waveOver && t >= waveFrom + WaveSeconds)
                {
                    waveOver = true;
                    Waved?.Invoke(WaveFrame);
                }
                if (t >= talkFrom && t < talkTo)
                {
                    // An uneven mouth: open, wide, open, closed...
                    int step = (int)(t / 0.11f);
                    int k = (step * 7 + step / 3) % 5;
                    frame = k < 2 ? video.face + "_talk" : k < 4 ? video.face + "_talk2" : video.face;
                }
                else if (t < blinkUntil)
                {
                    frame = video.face + "_blink";
                }
                if (t >= nextBlink)
                {
                    blinkUntil = t + 0.14f;
                    nextBlink = t + UnityEngine.Random.Range(2.4f, 5f);
                }
                if (Faked)
                {
                    if (t >= nextGlitch)
                    {
                        glitchUntil = t + 0.12f;
                        nextGlitch = t + UnityEngine.Random.Range(3.5f, 6.5f);
                    }
                    if (t < glitchUntil)
                        frame = video.face + "_fake_glitch";
                }
            }
            Show(frame);
        }

        void Show(string frame)
        {
            if (frame == shown)
                return;
            shown = frame;
            UIKit.SetImage(Root, UISkin.Tex(frame) ?? UISkin.Tex(video.face));
        }
    }
}
