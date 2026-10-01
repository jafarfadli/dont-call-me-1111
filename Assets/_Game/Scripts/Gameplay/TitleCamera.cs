using UnityEngine;

namespace DontCallMe.Gameplay
{
    /// <summary>
    /// A slow cinematic drift for the title and end screens: eases back and forth between two
    /// poses with a faint handheld sway.
    /// </summary>
    public class TitleCamera : MonoBehaviour
    {
        [SerializeField] Vector3 fromPosition;
        [SerializeField] Vector3 fromEuler;
        [SerializeField] Vector3 toPosition;
        [SerializeField] Vector3 toEuler;
        [SerializeField] float seconds = 36f;
        [SerializeField] float sway = 0.25f;

        float t;

        public void SetPoses(Vector3 aPos, Vector3 aEuler, Vector3 bPos, Vector3 bEuler, float period)
        {
            fromPosition = aPos;
            fromEuler = aEuler;
            toPosition = bPos;
            toEuler = bEuler;
            seconds = period;
        }

        void LateUpdate()
        {
            t += Time.unscaledDeltaTime / Mathf.Max(1f, seconds);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t * 2f, 1f));
            transform.position = Vector3.Lerp(fromPosition, toPosition, k);
            var euler = new Vector3(Mathf.LerpAngle(fromEuler.x, toEuler.x, k), Mathf.LerpAngle(fromEuler.y, toEuler.y, k), 0f);
            float time = Time.unscaledTime;
            euler.x += Mathf.Sin(time * 0.37f) * sway;
            euler.y += Mathf.Sin(time * 0.23f + 1.3f) * sway;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
