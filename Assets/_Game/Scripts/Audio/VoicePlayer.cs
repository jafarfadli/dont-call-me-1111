using UnityEngine;

namespace DontCallMe.Audio
{
    /// <summary>
    /// Plays the voice of whoever is on the line. One line at a time; pauses with the game
    /// (AudioListener.pause) and follows the Voice volume setting. Created on first use.
    /// </summary>
    public class VoicePlayer : MonoBehaviour
    {
        static VoicePlayer instance;
        AudioSource source;

        public static bool IsSpeaking => instance != null && instance.source.isPlaying;

        static VoicePlayer Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameObject("VoicePlayer").AddComponent<VoicePlayer>();
                return instance;
            }
        }

        void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = false;
            Apply();
            GameSettings.Changed += Apply;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= Apply;
            if (instance == this)
                instance = null;
        }

        void Apply() => source.volume = GameSettings.Voice;

        /// <summary>Plays the clip and returns how long it lasts (0 without a clip).</summary>
        public static float Play(AudioClip clip)
        {
            if (clip == null)
                return 0f;
            var v = Instance;
            v.source.Stop();
            v.source.clip = clip;
            v.source.Play();
            return clip.length;
        }

        public static void Stop()
        {
            if (instance != null)
                instance.source.Stop();
        }
    }
}
