using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Gameplay
{
    /// <summary>
    /// Looks under the mouse cursor for an <see cref="Interactable"/> anywhere in the room, shows its
    /// prompt next to the cursor and opens its panel on click or E. Furniture never blocks the
    /// pointer: everything that glows can be pointed at. Only what is within <see cref="Reach"/>
    /// opens; something farther shows its prompt with "walk closer", so the player gets up and
    /// walks over (the cork board is by the door, the rest on the desk). Clicks that ended a view
    /// drag, and clicks on the UI, are ignored. While the room can be used, every interactable
    /// wears a pulsing outline; the one under the cursor (and the one a tutorial step points at)
    /// also lights up.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        /// <summary>How far from the eyes an object can be used, in metres: an arm's length and a step.</summary>
        public const float Reach = 2f;
        /// <summary>How far the pointer finds objects to name (anywhere in the room).</summary>
        const float Sight = 10f;

        [SerializeField] Camera view;
        [SerializeField] UIManager ui;
        [SerializeField] FirstPersonController player;
        [SerializeField] float reach = Reach;
        [SerializeField] LayerMask mask = ~0;

        [Header("Highlight (added light, 0..1 of the highlight colour)")]
        [SerializeField] float hoverLight = 0.14f;
        [SerializeField] float spotlightLight = 0.2f;

        static readonly int PulseId = Shader.PropertyToID("_DCM_HighlightPulse");

        Interactable hovered;
        bool hoveredFar;
        // Clicked from too far: the player walks over and it opens on arrival.
        Interactable walkingTo;
        readonly RaycastHit[] hits = new RaycastHit[16];

        /// <summary>A panel's object to draw the eye to (the tutorial sets it); None for no spotlight.</summary>
        public PanelId Spotlight { get; set; }

        public Interactable Hovered => hovered;

        /// <summary>Whether the object that opens <paramref name="panel"/> is close enough to use from where the player stands.</summary>
        public bool InReach(PanelId panel)
        {
            if (view == null)
                return true;
            foreach (var it in Interactable.All)
                if (it.panel == panel && InReach(it))
                    return true;
            return false;
        }

        /// <summary>Whether the object that opens <paramref name="panel"/> is in front of the player, in the view.</summary>
        public bool InView(PanelId panel)
        {
            if (view == null)
                return true;
            foreach (var it in Interactable.All)
            {
                if (it.panel != panel || !it.isActiveAndEnabled)
                    continue;
                Vector3 v = view.WorldToViewportPoint(it.WorldBounds.center);
                if (v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.05f && v.y < 0.95f)
                    return true;
            }
            return false;
        }

        bool InReach(Interactable it)
        {
            if (it == null || !it.isActiveAndEnabled)
                return false;
            Vector3 eye = view.transform.position;
            foreach (var col in it.GetComponentsInChildren<Collider>())
                if (col.enabled && Vector3.Distance(eye, col.ClosestPoint(eye)) <= reach)
                    return true;
            return false;
        }

        void CancelWalk()
        {
            if (walkingTo != null && player != null)
                player.StopWalking();
            walkingTo = null;
        }

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
            CancelWalk();
            hovered = null;
            foreach (var it in Interactable.All)
                it.SetHighlight(false, 0f);
        }

        void Update()
        {
            if (ui == null || view == null)
                return;
            bool usable = !ui.InputLocked;
            if (walkingTo != null && !usable)
                CancelWalk();
            if (walkingTo != null)
            {
                // A click sent the player over: open the panel once it is in reach.
                if (InReach(walkingTo))
                {
                    var target = walkingTo;
                    CancelWalk();
                    Clear();
                    ui.OpenPanel(target.panel);
                    Glow(false);
                    return;
                }
                if (player == null || !player.AutoWalking)
                {
                    // A key or a drag took over, or something stood in the way.
                    bool blocked = player != null && !player.InputLocked && walkingTo == hovered;
                    walkingTo = null;
                    if (blocked)
                        ui.NudgePrompt();
                }
            }
            if (!usable || Mouse.current == null || ui.IsPointerOverUI())
            {
                Clear();
            }
            else
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                var ray = view.ScreenPointToRay(pointer);
                hovered = Nearest(ray, out float distance);
                // The same test as a click's (the nearest point of the object, not where the ray hit it).
                hoveredFar = hovered != null && distance > reach && !InReach(hovered);
                if (hovered == null)
                {
                    ui.HidePrompt();
                }
                else
                {
                    bool canWalk = player != null && player.CanWalk;
                    ui.ShowPrompt(hovered.prompt, pointer, !hoveredFar ? PromptReach.Near : canWalk ? PromptReach.Walk : PromptReach.TooFar);
                    bool click = Mouse.current.leftButton.wasReleasedThisFrame && (player == null || !player.PressWasDrag);
                    bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
                    // An opened panel covers the room from this frame on.
                    if ((click || key) && Use(hovered))
                        usable = false;
                }
            }
            Glow(usable);
        }

        /// <summary>
        /// What a click on <paramref name="it"/> does: opens its panel when it is in reach (true), else
        /// walks the player over to open it on arrival, or shakes the prompt when the player cannot walk.
        /// </summary>
        public bool Use(Interactable it)
        {
            if (it == null || ui == null)
                return false;
            if (InReach(it))
            {
                CancelWalk();
                Clear();
                ui.OpenPanel(it.panel);
                return true;
            }
            if (player != null && player.CanWalk)
            {
                walkingTo = it;
                player.WalkToward(it.WorldBounds.center);
            }
            else
            {
                ui.NudgePrompt();
            }
            return false;
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
                    light = Mathf.Max(light, hoveredFar ? hoverLight * 0.5f : hoverLight);
                it.SetHighlight(usable, light);
            }
        }

        /// <summary>
        /// The closest interactable along the ray, and how far it is. Other colliders on the way (the
        /// furniture's walking boxes, props) are passed over, so a glowing object is never dead to the pointer.
        /// </summary>
        Interactable Nearest(Ray ray, out float distance)
        {
            int count = Physics.RaycastNonAlloc(ray, hits, Mathf.Max(reach, Sight), mask, QueryTriggerInteraction.Collide);
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
            distance = bestDistance;
            return best;
        }

        void Clear()
        {
            if (hovered == null)
                return;
            hovered = null;
            hoveredFar = false;
            ui.HidePrompt();
        }
    }
}
