using UnityEngine;

namespace DontCallMe.Rendering
{
    /// <summary>
    /// Pushes the comic look's global shader values used by DontCallMe/Toon:
    /// a shadowless window-bounce fill band, banded SSAO contact shadows and hatching scale.
    /// The fill shines along this transform's forward axis.
    /// </summary>
    [ExecuteAlways]
    public sealed class DCMLook : MonoBehaviour
    {
        [Header("Window fill")]
        [SerializeField] Color fillColor = new Color(0.98f, 0.88f, 0.74f);
        [SerializeField, Range(0f, 2f)] float fillIntensity = 0.55f;
        [SerializeField, Range(-1f, 1f)] float fillThreshold = -0.1f;
        [SerializeField, Range(0.001f, 0.5f)] float fillSoftness = 0.04f;

        [Header("Contact shadows (banded SSAO)")]
        [SerializeField, Range(0.05f, 1f)] float aoThreshold = 0.45f;
        [SerializeField, Range(0.001f, 0.3f)] float aoSoftness = 0.09f;

        [Header("Hatching")]
        [SerializeField, Range(0.001f, 2f)] float hatchScale = 1f;

        static readonly int FillDirId = Shader.PropertyToID("_DCM_FillDir");
        static readonly int FillColorId = Shader.PropertyToID("_DCM_FillColor");
        static readonly int ShadeParamsId = Shader.PropertyToID("_DCM_ShadeParams");
        static readonly int HatchId = Shader.PropertyToID("_DCM_HatchGlobal");

        void OnEnable() => Apply();

        void OnValidate() => Apply();

        void Update()
        {
            if (!transform.hasChanged)
                return;
            transform.hasChanged = false;
            Apply();
        }

        void OnDisable()
        {
            Shader.SetGlobalVector(FillColorId, Vector4.zero);
        }

        void Apply()
        {
            Vector3 toLight = -transform.forward;
            Shader.SetGlobalVector(FillDirId, new Vector4(toLight.x, toLight.y, toLight.z, 1f));
            Shader.SetGlobalVector(FillColorId, (Vector4)(fillColor.linear * fillIntensity));
            Shader.SetGlobalVector(ShadeParamsId, new Vector4(fillThreshold, fillSoftness, aoThreshold, aoSoftness));
            Shader.SetGlobalFloat(HatchId, hatchScale);
        }
    }
}
