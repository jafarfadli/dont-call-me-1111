using System;
using UnityEngine;

namespace DontCallMe.Audio
{
    /// <summary>
    /// Player options that outlive a scene: music, sound effect and voice volume, and how fast
    /// dragging turns the view. Stored in PlayerPrefs; listeners get <see cref="Changed"/>.
    /// </summary>
    public static class GameSettings
    {
        const string MusicKey = "dcm.volume.music";
        const string SfxKey = "dcm.volume.sfx";
        const string VoiceKey = "dcm.volume.voice";
        const string LookKey = "dcm.look.sensitivity";

        public const float MinLook = 0.3f;
        public const float MaxLook = 2.5f;

        public static event Action Changed;

        static bool loaded;
        static float music = 0.7f;
        static float sfx = 0.8f;
        static float voice = 1f;
        static float look = 1f;

        public static float Music { get { Load(); return music; } set => Set(ref music, Mathf.Clamp01(value), MusicKey); }
        public static float Sfx { get { Load(); return sfx; } set => Set(ref sfx, Mathf.Clamp01(value), SfxKey); }
        public static float Voice { get { Load(); return voice; } set => Set(ref voice, Mathf.Clamp01(value), VoiceKey); }

        /// <summary>Multiplier on the drag-to-look speed, 1 = default.</summary>
        public static float LookSensitivity { get { Load(); return look; } set => Set(ref look, Mathf.Clamp(value, MinLook, MaxLook), LookKey); }

        static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            music = PlayerPrefs.GetFloat(MusicKey, music);
            sfx = PlayerPrefs.GetFloat(SfxKey, sfx);
            voice = PlayerPrefs.GetFloat(VoiceKey, voice);
            look = PlayerPrefs.GetFloat(LookKey, look);
        }

        static void Set(ref float field, float value, string key)
        {
            Load();
            if (Mathf.Approximately(field, value))
                return;
            field = value;
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
