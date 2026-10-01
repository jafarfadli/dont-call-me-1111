using System.Collections;
using DontCallMe.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Player
{
    /// <summary>
    /// First-person walking: WASD or left stick to move, Shift to walk faster.
    /// Looking is click-and-drag: hold a mouse button (or touch) and drag to turn the view;
    /// a plain click leaves the view alone so it can select things. The right stick turns the view directly.
    /// Panels lock it with <see cref="SetInputLocked"/>. A day starts with the player seated at the desk
    /// (<see cref="Sit"/>): the view is lower, turns within a range and nothing walks until
    /// <see cref="StandUp"/>. The drag speed follows the Look sensitivity setting.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] InputActionReference move;
        [SerializeField] InputActionReference look;
        [SerializeField] InputActionReference drag;
        [SerializeField] InputActionReference sprint;

        [Header("View")]
        [SerializeField] Transform cameraPivot;
        [Tooltip("Degrees the view turns per pixel dragged.")]
        [SerializeField, Range(0.05f, 1f)] float dragSensitivity = 0.3f;
        [Tooltip("Pixels to drag before the view starts turning, so a click does not nudge it.")]
        [SerializeField, Range(0f, 30f)] float dragDeadZone = 10f;
        [Tooltip("Off: the view turns the way you drag. On: you drag the scene, like a photo on a phone.")]
        [SerializeField] bool invertDrag;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField] float stickSensitivity = 160f;
        [SerializeField, Range(40f, 89f)] float pitchLimit = 80f;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 1.7f;
        [SerializeField] float fastSpeed = 2.9f;
        [SerializeField] float acceleration = 10f;
        [SerializeField] float gravity = -18f;

        [Header("Head bob")]
        [SerializeField, Range(0f, 0.04f)] float bobHeight = 0.014f;
        [SerializeField, Range(0f, 0.04f)] float bobSway = 0.008f;
        [Tooltip("Metres walked per full bob cycle (two steps).")]
        [SerializeField] float strideLength = 1.3f;

        [Header("Seated")]
        [Tooltip("How much lower the eyes are when seated.")]
        [SerializeField] float seatedDrop = 0.42f;
        [Tooltip("How far the seated view turns left or right of the desk, in degrees.")]
        [SerializeField] float seatedYawRange = 80f;

        CharacterController body;
        Vector3 horizontalVelocity;
        float verticalSpeed;
        float yaw;
        float pitch;
        float bobPhase;
        float bobWeight;
        Vector3 pivotRest;
        bool inputLocked;
        bool seated;
        float seatYaw;
        float heightOffset;
        Coroutine standing;

        bool dragging;
        bool pressOnUI;
        bool dragTurning;
        Vector2 dragTravel;
        Vector2 dragStartPosition;
        int skipDeltaFrames;

        public bool InputLocked => inputLocked;
        public bool IsSeated => seated;

        /// <summary>Set by the UI: true while the pointer is over a UI element, so a press there never turns the view.</summary>
        public static System.Func<bool> PointerOverUI;

        /// <summary>
        /// True once the current (or last) mouse press turned the view, until the next press.
        /// A click handler checks it on release so the end of a drag does not select anything.
        /// </summary>
        public bool PressWasDrag { get; private set; }

        /// <summary>Stops walking and looking (for 2D panels).</summary>
        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (locked)
                EndDrag();
        }

        void Awake()
        {
            body = GetComponent<CharacterController>();
            pivotRest = cameraPivot.localPosition;
            yaw = transform.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
        }

        void OnEnable()
        {
            move.action.Enable();
            look.action.Enable();
            drag.action.Enable();
            sprint.action.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDisable()
        {
            EndDrag();
        }

        void Update()
        {
            if (!inputLocked)
                Look();
            Move(!inputLocked && !seated && standing == null);
            HeadBob();
        }

        /// <summary>Sits the player at the desk: feet on the floor at the chair, facing <paramref name="yawDegrees"/>.</summary>
        public void Sit(Vector3 feet, float yawDegrees, float pitchDegrees = 12f)
        {
            if (standing != null)
                StopCoroutine(standing);
            standing = null;
            EndDrag();
            seated = true;
            body.enabled = false;
            horizontalVelocity = Vector3.zero;
            verticalSpeed = 0f;
            transform.position = feet;
            yaw = seatYaw = yawDegrees;
            pitch = pitchDegrees;
            heightOffset = -seatedDrop;
            Turn(Vector2.zero);
        }

        /// <summary>Stands up and steps back to <paramref name="feet"/>, then walking works again.</summary>
        public void StandUp(Vector3 feet, float seconds = 0.9f)
        {
            if (!seated || standing != null)
                return;
            standing = StartCoroutine(Stand(feet, seconds));
        }

        IEnumerator Stand(Vector3 feet, float seconds)
        {
            Vector3 from = transform.position;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.05f, seconds));
                float k = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(from, feet, k);
                heightOffset = Mathf.Lerp(-seatedDrop, 0f, k);
                yield return null;
            }
            seated = false;
            body.enabled = true;
            standing = null;
        }

        void Look()
        {
            Vector2 delta = look.action.ReadValue<Vector2>();
            bool fromPointer = look.action.activeControl == null || look.action.activeControl.device is Pointer;

            if (!fromPointer)
            {
                Turn(delta * (stickSensitivity * Time.deltaTime));
                return;
            }

            bool held = drag.action.IsPressed();
            if (!held)
                pressOnUI = false;
            if (held && !dragging && !pressOnUI)
            {
                // A press that starts on the UI belongs to the UI until it is released.
                if (PointerOverUI != null && PointerOverUI())
                {
                    pressOnUI = true;
                    return;
                }
                BeginDrag();
            }
            else if (!held && dragging)
                EndDrag();
            if (!dragging)
                return;

            if (skipDeltaFrames > 0)
            {
                skipDeltaFrames--;
                return;
            }

            if (!dragTurning)
            {
                // How far the pointer got from the press, not how much it wobbled on the way.
                dragTravel += delta;
                if (dragTravel.magnitude < dragDeadZone)
                    return;
                dragTurning = true;
                PressWasDrag = true;
                Turn(delta * DragDegreesPerPixel);
                // Hide and pin the cursor so the drag never stops at the screen edge.
                // Locking recentres the cursor, so ignore the jump that follows.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                skipDeltaFrames = 2;
                return;
            }

            Turn(delta * DragDegreesPerPixel);
        }

        float DragDegreesPerPixel => dragSensitivity * GameSettings.LookSensitivity * (invertDrag ? -1f : 1f);

        void Turn(Vector2 degrees)
        {
            yaw += degrees.x;
            if (seated)
                yaw = seatYaw + Mathf.Clamp(Mathf.DeltaAngle(seatYaw, yaw), -seatedYawRange, seatedYawRange);
            pitch = Mathf.Clamp(pitch - degrees.y, -pitchLimit, pitchLimit);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void BeginDrag()
        {
            dragging = true;
            dragTurning = false;
            PressWasDrag = false;
            dragTravel = Vector2.zero;
            dragStartPosition = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        }

        void EndDrag()
        {
            bool wasTurning = dragTurning;
            dragging = false;
            dragTurning = false;
            if (!wasTurning)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Put the cursor back where the drag started instead of the screen centre.
            if (Mouse.current != null)
                Mouse.current.WarpCursorPosition(dragStartPosition);
        }

        void Move(bool canControl)
        {
            if (!body.enabled)
                return;
            Vector2 input = canControl ? Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f) : Vector2.zero;
            float speed = canControl && sprint.action.IsPressed() ? fastSpeed : walkSpeed;
            Vector3 target = (transform.right * input.x + transform.forward * input.y) * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * Time.deltaTime);

            if (body.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            else
                verticalSpeed += gravity * Time.deltaTime;

            body.Move((horizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        void HeadBob()
        {
            float speed = body.enabled ? new Vector3(body.velocity.x, 0f, body.velocity.z).magnitude : 0f;
            bool walking = body.enabled && body.isGrounded && speed > 0.1f;
            bobWeight = Mathf.MoveTowards(bobWeight, walking ? Mathf.Clamp01(speed / walkSpeed) : 0f, 4f * Time.deltaTime);
            if (walking)
                bobPhase += speed / Mathf.Max(strideLength, 0.1f) * 2f * Mathf.PI * Time.deltaTime;
            float lift = Mathf.Abs(Mathf.Sin(bobPhase)) * bobHeight * bobWeight;
            float sway = Mathf.Sin(bobPhase) * bobSway * bobWeight;
            cameraPivot.localPosition = pivotRest + new Vector3(sway, lift + heightOffset, 0f);
        }
    }
}
