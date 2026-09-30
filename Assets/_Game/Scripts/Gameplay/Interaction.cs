using DontCallMe.Player;
using DontCallMe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Gameplay
{
    /// <summary>Marks an INT_* object: which panel it opens and what the prompt says.</summary>
    public class Interactable : MonoBehaviour
    {
        public PanelId panel;
        public string prompt = "Look";
    }

    /// <summary>
    /// Looks under the mouse cursor (up to 2.5 m) for an <see cref="Interactable"/>, shows its prompt
    /// next to the cursor and opens its panel on click or E. Clicks that ended a view drag, and
    /// clicks on the UI, are ignored.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] UIManager ui;
        [SerializeField] FirstPersonController player;
        [SerializeField] float reach = 2.5f;
        [SerializeField] LayerMask mask = ~0;

        Interactable hovered;

        void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<Camera>();
            if (player == null)
                player = GetComponent<FirstPersonController>();
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
        }

        void Update()
        {
            if (ui == null || view == null || Mouse.current == null)
                return;
            if (ui.InputLocked || ui.IsPointerOverUI())
            {
                Clear();
                return;
            }
            Vector2 pointer = Mouse.current.position.ReadValue();
            var ray = view.ScreenPointToRay(pointer);
            Interactable hit = null;
            if (Physics.Raycast(ray, out var info, reach, mask, QueryTriggerInteraction.Collide))
                hit = info.collider.GetComponentInParent<Interactable>();
            if (hit != hovered)
                hovered = hit;
            if (hovered == null)
            {
                ui.HidePrompt();
                return;
            }
            ui.ShowPrompt(hovered.prompt, pointer);

            bool click = Mouse.current.leftButton.wasReleasedThisFrame && (player == null || !player.PressWasDrag);
            bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (click || key)
            {
                ui.HidePrompt();
                ui.OpenPanel(hovered.panel);
            }
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
