using System.Collections.Generic;
using System.Linq;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Gameplay;
using DontCallMe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;
using RoomArt = DontCallMe.Editor.Art.RoomArtPipeline;

namespace DontCallMe.Editor.Scenes
{
    /// <summary>
    /// Builds the title scene (Home) and the next-morning scene (End) around the same generated
    /// bedroom, and puts the three scenes in the build in order: Home, Room, End.
    /// </summary>
    public static class ScenePipeline
    {
        const string HomePath = "Assets/_Game/Scenes/Home.unity";
        const string RoomPath = "Assets/_Game/Scenes/Room.unity";
        const string EndPath = "Assets/_Game/Scenes/End.unity";
        const string PanelSettingsPath = "Assets/_Game/UI/Settings/PS_Game.asset";
        const string SkinPath = "Assets/_Game/UI/Settings/UISkin.asset";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/Don't Call Me/Scenes/Build Home and End Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BuildHome();
            BuildEnd();
            SetBuildOrder();
            EditorSceneManager.OpenScene(RoomPath);
        }

        [MenuItem("Tools/Don't Call Me/Scenes/Set Build Order")]
        public static void SetBuildOrder()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(HomePath, true),
                new EditorBuildSettingsScene(RoomPath, true),
                new EditorBuildSettingsScene(EndPath, true),
            };
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != HomePath && s.path != RoomPath && s.path != EndPath));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[ScenePipeline] Build order: Home, Room, End");
        }

        public static void BuildHome()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RoomArt.BuildEnvironment(false);
            // A slow drift across the room at dusk, the sunbeam and the window in view.
            var cam = RoomArt.BuildShowCamera("TitleCamera", new Vector3(1.45f, -1.55f, 1.6f), new Vector3(-1.0f, 0.9f, 1.0f), 60f);
            AddDrift(cam, new Vector3(1.45f, -1.55f, 1.6f), new Vector3(-1.0f, 0.9f, 1.0f),
                     new Vector3(1.2f, -1.1f, 1.5f), new Vector3(-1.4f, 0.5f, 1.05f), 50f);
            AddEventSystem();
            var ui = AddDocument("TitleUI");
            var home = ui.AddComponent<TitleScreen>();
            SetRef(home, "skin", AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath));
            AddMusic("home_lofi");
            EditorSceneManager.SaveScene(scene, HomePath);
        }

        public static void BuildEnd()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RoomArt.BuildEnvironment(true);
            // The desk the next morning, where the paper lies.
            var cam = RoomArt.BuildShowCamera("EndCamera", new Vector3(0.7f, 0.25f, 1.5f), new Vector3(0.95f, 1.62f, 0.8f), 55f);
            AddDrift(cam, new Vector3(0.7f, 0.25f, 1.5f), new Vector3(0.95f, 1.62f, 0.8f),
                     new Vector3(0.95f, 0.3f, 1.45f), new Vector3(0.8f, 1.65f, 0.85f), 60f);
            AddEventSystem();
            var ui = AddDocument("EndUI");
            var end = ui.AddComponent<EndScreen>();
            SetRef(end, "skin", AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath));
            SetRef(end, "previewDay", AssetDatabase.LoadAssetAtPath<DayData>(DontCallMe.Editor.UI.ContentBuilder.DayPath));
            AddMusic("morning");
            EditorSceneManager.SaveScene(scene, EndPath);
        }

        static void AddDrift(Camera cam, Vector3 aPos, Vector3 aTarget, Vector3 bPos, Vector3 bTarget, float seconds)
        {
            var drift = cam.gameObject.AddComponent<TitleCamera>();
            Vector3 pa = RoomArt.FromBlender(aPos), pb = RoomArt.FromBlender(bPos);
            var ra = Quaternion.LookRotation(RoomArt.FromBlender(aTarget) - pa, Vector3.up).eulerAngles;
            var rb = Quaternion.LookRotation(RoomArt.FromBlender(bTarget) - pb, Vector3.up).eulerAngles;
            drift.SetPoses(pa, ra, pb, rb, seconds);
            EditorUtility.SetDirty(drift);
        }

        static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions != null)
                module.actionsAsset = actions;
        }

        static GameObject AddDocument(string name)
        {
            var go = new GameObject(name);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            go.AddComponent<Sfx>();
            return go;
        }

        static void AddMusic(string track)
        {
            var go = new GameObject("Music");
            var music = go.AddComponent<MusicPlayer>();
            var so = new SerializedObject(music);
            so.FindProperty("main").objectReferenceValue = DontCallMe.Editor.Audio.AudioPipeline.Music(track);
            so.FindProperty("playOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
