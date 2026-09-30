using DontCallMe.UI;
using UnityEditor;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Tools → Don't Call Me → UI → Capture UI Tour: enters Play mode and screenshots every panel,
    /// app and the demo call and chat into Temp/UITour (see <see cref="UITour"/>).
    /// </summary>
    [InitializeOnLoad]
    public static class UITourMenu
    {
        const string Pending = "DCM.UITour.Pending";

        static UITourMenu()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false))
                    return;
                SessionState.SetBool(Pending, false);
                Start();
            };
        }

        [MenuItem("Tools/Don't Call Me/UI/Capture UI Tour")]
        public static void Capture()
        {
            if (EditorApplication.isPlaying)
            {
                Start();
                return;
            }
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        static void Start()
        {
            var host = Object.FindAnyObjectByType<UIManager>();
            if (host == null)
            {
                Debug.LogError("[UITour] No UIManager in the scene.");
                return;
            }
            var tour = host.gameObject.GetComponent<UITour>() ?? host.gameObject.AddComponent<UITour>();
            tour.Begin();
        }
    }
}
