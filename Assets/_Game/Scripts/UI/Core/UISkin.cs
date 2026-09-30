using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.UI
{
    /// <summary>
    /// Every UI texture by name (frames, icons, portraits, cards). Filled by the UI pipeline from
    /// Assets/_Game/UI/Sprites, so code can ask for "pt_mom" or "app_bank" without paths.
    /// </summary>
    [CreateAssetMenu(menuName = "Don't Call Me/UI Skin")]
    public class UISkin : ScriptableObject
    {
        public List<Texture2D> textures = new List<Texture2D>();

        Dictionary<string, Texture2D> byName;

        public static UISkin Current { get; set; }

        public Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (byName == null || byName.Count != textures.Count)
            {
                byName = new Dictionary<string, Texture2D>();
                foreach (var t in textures)
                    if (t != null)
                        byName[t.name] = t;
            }
            return byName.TryGetValue(name, out var tex) ? tex : null;
        }

        public static Texture2D Tex(string name) => Current != null ? Current.Get(name) : null;
    }
}
