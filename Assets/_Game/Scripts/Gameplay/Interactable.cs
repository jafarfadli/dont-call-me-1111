using System.Collections.Generic;
using DontCallMe.UI;
using UnityEngine;

namespace DontCallMe.Gameplay
{
    /// <summary>
    /// Marks an object the player can click: which panel it opens and what the prompt says. While
    /// the room can be used it wears a glowing outline (drawn by the ink pass), and a warm wash
    /// under the cursor; the <see cref="Interactor"/> sets both.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public PanelId panel;
        public string prompt = "Look";
        [Tooltip("Parts whose name contains this are left out of the highlight (a trailing cable).")]
        public string plain = "";

        /// <summary>Every enabled interactable in the scene.</summary>
        public static readonly List<Interactable> All = new List<Interactable>();

        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        static readonly Color GlowColor = new Color(1f, 0.74f, 0.3f);

        Renderer[] renderers;
        MaterialPropertyBlock block;
        bool outlined;
        float wash;

        void OnEnable() => All.Add(this);

        void OnDisable()
        {
            All.Remove(this);
            SetHighlight(false, 0f);
        }

        Renderer[] Renderers
        {
            get
            {
                if (renderers == null)
                {
                    var list = new List<Renderer>(GetComponentsInChildren<Renderer>());
                    if (!string.IsNullOrEmpty(plain))
                        list.RemoveAll(r => r.name.Contains(plain));
                    renderers = list.ToArray();
                }
                return renderers;
            }
        }

        /// <summary>
        /// The outline on or off, and how much warm light to add (0 = as painted). Other overrides on
        /// the renderers (the day's newspaper print) stay.
        /// </summary>
        public void SetHighlight(bool outline, float addedLight)
        {
            if (outline == outlined && Mathf.Approximately(addedLight, wash))
                return;
            outlined = outline;
            wash = addedLight;
            block ??= new MaterialPropertyBlock();
            var colour = GlowColor * addedLight;
            colour.a = outline ? 1f : 0f;
            foreach (var r in Renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(block);
                block.SetColor(HighlightId, colour);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>The object's box in the world (for pointing at it).</summary>
        public Bounds WorldBounds
        {
            get
            {
                var bounds = new Bounds(transform.position, Vector3.zero);
                bool any = false;
                foreach (var r in Renderers)
                {
                    if (r == null)
                        continue;
                    if (any)
                        bounds.Encapsulate(r.bounds);
                    else
                        bounds = r.bounds;
                    any = true;
                }
                return bounds;
            }
        }
    }
}
