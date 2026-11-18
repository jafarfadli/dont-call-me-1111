using System.Collections;
using System.Collections.Generic;
using DontCallMe.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Player
{
    /// <summary>
    /// First-person walking: WASD or left stick to move, Shift to walk faster, or a click on
    /// something out of reach (<see cref="WalkToward"/>: the player walks over, turning to face it,
    /// until the interactor finds it in reach; any key or drag takes over).
    /// Looking is click-and-drag: hold a mouse button (or touch) and drag to turn the view;
    /// a plain click leaves the view alone so it can select things. The right stick turns the view directly.
    /// The view follows the drag through a short ease (<c>lookSmoothing</c>), so uneven mouse
    /// deltas never judder; the drag's dead zone is caught up once it counts as a drag.
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
        [Tooltip("How fast the view catches up with the drag, per second (higher is snappier; 0 turns it off).")]
        [SerializeField, Range(0f, 60f)] float lookSmoothing = 24f;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 1.7f;
        [SerializeField] float fastSpeed = 2.9f;
        [SerializeField] float acceleration = 10f;
        [Tooltip("Slowing down: a little quicker than speeding up, so a released key does not slide.")]
        [SerializeField] float deceleration = 14f;
        [SerializeField] float gravity = -18f;
        [Tooltip("Degrees per second the view turns toward where a click sends the player.")]
        [SerializeField] float walkTurnSpeed = 220f;

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
        // Where the view is going (yaw, pitch) and where it is (shown*), eased toward it.
        float yaw;
        float pitch;
        float shownYaw;
        float shownPitch;
        // A click on something out of reach: walking there, through the points of a WalkPath.
        readonly List<Vector3> walkPoints = new List<Vector3>();
        int walkIndex = -1;
        Vector3 walkLook;
        float walkBest;
        float walkStuck;
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

        /// <summary>Whether the player can walk now: standing, not in a panel.</summary>
        public bool CanWalk => body != null && body.enabled && !seated && standing == null && !inputLocked;

        /// <summary>Walking to where a click sent the player (<see cref="WalkToward"/>).</summary>
        public bool AutoWalking => walkIndex >= 0;

        /// <summary>
        /// Walks toward <paramref name="point"/> (what was clicked), around the furniture
        /// (<see cref="WalkPath"/>) and facing the way, until <see cref="StopWalking"/>, a key or a
        /// drag, or the end of the way. Without a way it walks straight and stops where it is blocked.
        /// </summary>
        public void WalkToward(Vector3 point)
        {
            if (!CanWalk)
                return;
            Vector3 feet = transform.position;
            walkLook = point;
            point.y = feet.y;
            walkPoints.Clear();
            var path = WalkPath.Find(feet, point, body.radius, body);
            if (path != null && path.Count > 0)
                walkPoints.AddRange(path);
            else
                walkPoints.Add(point);
            walkIndex = 0;
            walkBest = float.MaxValue;
            walkStuck = 0f;
        }

        public void StopWalking() => walkIndex = -1;

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
            {
                EndDrag();
                StopWalking();
            }
        }

        void Awake()
        {
            body = GetComponent<CharacterController>();
            pivotRest = cameraPivot.localPosition;
            yaw = shownYaw = transform.eulerAngles.y;
            pitch = shownPitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
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
            ShowView(false);
            HeadBob();
        }

        /// <summary>Eases the shown view toward where the drag (or a walk) points it; <paramref name="snap"/> jumps there.</summary>
        void ShowView(bool snap)
        {
            float k = snap || lookSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-lookSmoothing * Time.unscaledDeltaTime);
            shownYaw += (yaw - shownYaw) * k;
            shownPitch += (pitch - shownPitch) * k;
            transform.rotation = Quaternion.Euler(0f, shownYaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(shownPitch, 0f, 0f);
        }

        /// <summary>Sits the player at the desk: feet on the floor at the chair, facing <paramref name="yawDegrees"/>.</summary>
        public void Sit(Vector3 feet, float yawDegrees, float pitchDegrees = 12f)
        {
            if (standing != null)
                StopCoroutine(standing);
            standing = null;
            EndDrag();
            StopWalking();
            seated = true;
            body.enabled = false;
            horizontalVelocity = Vector3.zero;
            verticalSpeed = 0f;
            transform.position = feet;
            yaw = seatYaw = yawDegrees;
            pitch = pitchDegrees;
            heightOffset = -seatedDrop;
            Turn(Vector2.zero);
            ShowView(true);
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
                StopWalking();
                // The view catches up with the whole way the pointer went, so the drag does not
                // stick at its start and then jump (the ease spreads it over a few frames).
                Turn(dragTravel * DragDegreesPerPixel);
                // Hide and pin the cursor so the drag never stops at the screen edge.
                // Locking recentres the cursor, so ignore the jump that follows.
                if (LockCursorOnDrag)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    skipDeltaFrames = 2;
                }
                return;
            }

            Turn(delta * DragDegreesPerPixel);
        }

        /// <summary>
        /// A browser only grants a pointer lock on a click, shows a notice each time and frees it on
        /// Esc (the game's Back key), so in a web build the cursor stays free while dragging.
        /// </summary>
#if UNITY_WEBGL && !UNITY_EDITOR
        static readonly bool LockCursorOnDrag = false;
#else
        static readonly bool LockCursorOnDrag = true;
#endif

        float DragDegreesPerPixel => dragSensitivity * GameSettings.LookSensitivity * (invertDrag ? -1f : 1f);

        void Turn(Vector2 degrees)
        {
            yaw += degrees.x;
            if (seated)
                yaw = seatYaw + Mathf.Clamp(Mathf.DeltaAngle(seatYaw, yaw), -seatedYawRange, seatedYawRange);
            pitch = Mathf.Clamp(pitch - degrees.y, -pitchLimit, pitchLimit);
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
            if (!wasTurning || !LockCursorOnDrag)
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
            // Keys take over from a click's walk.
            if (AutoWalking && (!canControl || input.sqrMagnitude > 0.01f))
                StopWalking();
            Vector3 target = AutoWalking ? AutoWalk() : (transform.right * input.x + transform.forward * input.y) * speed;
            float rate = target.sqrMagnitude > horizontalVelocity.sqrMagnitude ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * Time.deltaTime);

            if (body.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -2f;
            else
                verticalSpeed += gravity * Time.deltaTime;

            body.Move((horizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        /// <summary>
        /// The velocity toward the next point of a click's walk, turning the view to face the way. A
        /// point passed close by gives way to the next one; the walk ends at the last, or when half a
        /// second goes by without getting closer (something in the way).
        /// </summary>
        Vector3 AutoWalk()
        {
            bool last = walkIndex >= walkPoints.Count - 1;
            Vector3 to = walkPoints[walkIndex] - transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            // A turn is taken early only when the way on from here is free, so it never cuts a corner into the furniture.
            bool turn = !last && distance < 0.4f && (distance < 0.1f || WalkPath.IsClear(transform.position, walkPoints[walkIndex + 1], body.radius, body));
            if (turn || (last && distance < 0.12f))
            {
                if (last)
                {
                    StopWalking();
                    return Vector3.zero;
                }
                walkIndex++;
                walkBest = float.MaxValue;
                walkStuck = 0f;
                return AutoWalk();
            }
            // Face the way; on the last stretch, what was clicked, so it is in front when it opens.
            Vector3 look = to;
            if (last)
            {
                look = walkLook - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude < 0.01f)
                    look = to;
            }
            float facing = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, facing, walkTurnSpeed * Time.deltaTime);
            // Walk once the view roughly faces where it should, so the first steps turn rather than sidestep.
            float off = Mathf.Abs(Mathf.DeltaAngle(yaw, facing));
            if (distance < walkBest - 0.02f || off > 30f)
            {
                walkBest = Mathf.Min(walkBest, distance);
                walkStuck = 0f;
            }
            else if ((walkStuck += Time.deltaTime) > 0.5f)
            {
                StopWalking();
                return Vector3.zero;
            }
            return to / distance * walkSpeed * Mathf.Clamp01(1.2f - off / 75f);
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
