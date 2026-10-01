using DontCallMe.Data;
using DontCallMe.Flow;
using DontCallMe.UI;
using UnityEditor;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Tools → Don't Call Me → UI → Capture UI Tour: enters Play mode and screenshots every panel,
    /// app and the demo call and chat into Temp/UITour (see <see cref="UITour"/>). The tour reads
    /// English labels, so it runs in English and puts the player's language back afterwards. Its
    /// checks are written for Day 1's first case ("the protected account"), so it asks for that one.
    /// </summary>
    [InitializeOnLoad]
    public static class UITourMenu
    {
        const string Pending = "DCM.UITour.Pending";
        const string PreviousLanguage = "DCM.UITour.Language";

        static UITourMenu()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(PreviousLanguage, -1) >= 0)
                {
                    Loc.Current = (Lang)SessionState.GetInt(PreviousLanguage, 0);
                    SessionState.EraseInt(PreviousLanguage);
                }
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
            if (Loc.Current != Lang.En)
            {
                SessionState.SetInt(PreviousLanguage, (int)Loc.Current);
                Loc.Current = Lang.En;
            }
            SessionState.SetBool(Pending, true);
            GameRun.ForceNextVariant("scam");
            EditorApplication.isPlaying = true;
        }

        static void Start()
        {
            var host = Object.FindAnyObjectByType<UIManager>();
            if (host == null)
            {
                // No day started, so nothing took the variant asked for: don't leave it for the next real day.
                GameRun.TakeForcedVariant();
                Debug.LogError("[UITour] No UIManager in the scene. Open the Room scene first.");
                return;
            }
            var tour = host.gameObject.GetComponent<UITour>() ?? host.gameObject.AddComponent<UITour>();
            tour.Begin();
        }
    }
}
