using UnityEngine;

namespace DontCallMe.Audio
{
    /// <summary>
    /// The scene's music: a main loop and an optional tension layer of the same length that
    /// fades in with <see cref="SetTension"/> (investigation pressure). Keeps playing, ducked,
    /// while the game is paused. Follows the Music volume setting.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioClip main;
        [SerializeField] AudioClip tension;
        [SerializeField] bool playOnStart = true;
        [SerializeField, Range(0f, 1f)] float level = 0.8f;
        [SerializeField] float fadeInSeconds = 2.5f;

        AudioSource mainSource;
        AudioSource tensionSource;
        float fade;
        float fadeTarget;
        float fadeSpeed = 1f;
        float tensionLevel;
        float tensionTarget;
        float duck = 1f;
        float duckTarget = 1f;

        public static MusicPlayer Current { get; private set; }

        void Awake()
        {
            Current = this;
            mainSource = MakeSource();
            tensionSource = MakeSource();
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            if (playOnStart && main != null)
                Play(main, tension, fadeInSeconds);
        }

        AudioSource MakeSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true;
            s.volume = 0f;
            return s;
        }

        /// <summary>Starts a main loop and an optional tension layer in sync, fading in.</summary>
        public void Play(AudioClip mainClip, AudioClip tensionClip, float fadeSeconds)
        {
            mainSource.Stop();
            tensionSource.Stop();
            mainSource.clip = mainClip;
            tensionSource.clip = tensionClip;
            double at = AudioSettings.dspTime + 0.1;
            if (mainClip != null)
                mainSource.PlayScheduled(at);
            if (tensionClip != null)
                tensionSource.PlayScheduled(at);
            fade = 0f;
            fadeTarget = 1f;
            fadeSpeed = 1f / Mathf.Max(0.01f, fadeSeconds);
            tensionLevel = tensionTarget = 0f;
        }

        /// <summary>Keeps the scene's loop silent until <see cref="PlayMain"/>: a reveal that needs quiet first.</summary>
        public void Hold()
        {
            playOnStart = false;
            fade = fadeTarget = 0f;
            if (mainSource != null)
            {
                mainSource.Stop();
                tensionSource.Stop();
            }
        }

        /// <summary>Starts the scene's own loop, fading in.</summary>
        public void PlayMain(float fadeSeconds)
        {
            if (main != null)
                Play(main, tension, fadeSeconds);
        }

        public void Stop(float fadeSeconds)
        {
            fadeTarget = 0f;
            fadeSpeed = 1f / Mathf.Max(0.01f, fadeSeconds);
        }

        /// <summary>0 = calm, 1 = full tension layer.</summary>
        public void SetTension(float t) => tensionTarget = Mathf.Clamp01(t);

        public void Duck(bool on) => duckTarget = on ? 0.35f : 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fade = Mathf.MoveTowards(fade, fadeTarget, fadeSpeed * dt);
            tensionLevel = Mathf.MoveTowards(tensionLevel, tensionTarget, 0.3f * dt);
            duck = Mathf.MoveTowards(duck, duckTarget, 2f * dt);
            float master = GameSettings.Music * level * fade * duck;
            bool layered = tensionSource.clip != null;
            mainSource.volume = master * (layered ? Mathf.Lerp(1f, 0.6f, tensionLevel) : 1f);
            tensionSource.volume = master * tensionLevel;
            if (fade <= 0f && fadeTarget <= 0f && (mainSource.isPlaying || tensionSource.isPlaying))
            {
                mainSource.Stop();
                tensionSource.Stop();
            }
        }
    }
}
