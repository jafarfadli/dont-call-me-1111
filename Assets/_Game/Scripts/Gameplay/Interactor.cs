using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Gameplay
{
    /// <summary>
    /// Looks under the mouse cursor for an <see cref="Interactable"/> anywhere in the room, shows its
    /// prompt next to the cursor and opens its panel on click or E. Furniture never blocks the
    /// pointer: everything that glows can be clicked. Clicks that ended a view drag, and
    /// clicks on the UI, are ignored. While the room can be used, every interactable wears a
    /// pulsing outline; the one under the cursor (and the one a tutorial step points at) also
    /// lights up.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] UIManager ui;
        [SerializeField] FirstPersonController player;
        [SerializeField] float reach = 6f;
        [SerializeField] LayerMask mask = ~0;

        [Header("Highlight (added light, 0..1 of the highlight colour)")]
        [SerializeField] float hoverLight = 0.14f;
        [SerializeField] float spotlightLight = 0.2f;

        static readonly int PulseId = Shader.PropertyToID("_DCM_HighlightPulse");

        Interactable hovered;
        readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>A panel's object to draw the eye to (the tutorial sets it); None for no spotlight.</summary>
        public PanelId Spotlight { get; set; }

        public Interactable Hovered => hovered;

        void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<Camera>();
            if (player == null)
                player = GetComponent<FirstPersonController>();
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
        }

        void OnDisable()
        {
            hovered = null;
            foreach (var it in Interactable.All)
                it.SetHighlight(false, 0f);
        }

        void Update()
        {
            if (ui == null || view == null)
                return;
            bool usable = !ui.InputLocked;
            if (!usable || Mouse.current == null || ui.IsPointerOverUI())
            {
                Clear();
            }
            else
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                var ray = view.ScreenPointToRay(pointer);
                hovered = Nearest(ray);
                if (hovered == null)
                {
                    ui.HidePrompt();
                }
                else
                {
                    ui.ShowPrompt(hovered.prompt, pointer);
                    bool click = Mouse.current.leftButton.wasReleasedThisFrame && (player == null || !player.PressWasDrag);
                    bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
                    if (click || key)
                    {
                        var target = hovered;
                        Clear();
                        ui.OpenPanel(target.panel);
                        // The panel covers the room from this frame on.
                        usable = false;
                    }
                }
            }
            Glow(usable);
        }

        /// <summary>
        /// The outline on everything clickable (the ink pass pulses it), plus light on the object under
        /// the cursor and a blinking light on the spotlit one.
        /// </summary>
        void Glow(bool usable)
        {
            Shader.SetGlobalFloat(PulseId, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.4f));
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6.5f);
            foreach (var it in Interactable.All)
            {
                float light = 0f;
                if (usable && Spotlight != PanelId.None && it.panel == Spotlight)
                    light = spotlightLight * blink;
                if (usable && it == hovered)
                    light = Mathf.Max(light, hoverLight);
                it.SetHighlight(usable, light);
            }
        }

        /// <summary>
        /// The closest interactable along the ray. Other colliders on the way (the furniture's
        /// walking boxes, props) are passed over, so a glowing object is never dead to the click.
        /// </summary>
        Interactable Nearest(Ray ray)
        {
            int count = Physics.RaycastNonAlloc(ray, hits, reach, mask, QueryTriggerInteraction.Collide);
            Interactable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= bestDistance)
                    continue;
                var it = hits[i].collider.GetComponentInParent<Interactable>();
                if (it == null || !it.isActiveAndEnabled)
                    continue;
                best = it;
                bestDistance = hits[i].distance;
            }
            return best;
        }

        void Clear()
        {
            if (hovered == null)
                return;
            hovered = null;
            ui.HidePrompt();
        }
    }
}
