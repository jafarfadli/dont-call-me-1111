using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DontCallMe.Data;
using DontCallMe.Flow;
using DontCallMe.Gameplay;
using DontCallMe.Player;
using DontCallMe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Turns the generated 2D art (Tools/ArtGen/ui_art.py) and the OFL fonts into UI assets
    /// (9-slice sprites, font assets, panel settings, the skin) and puts the game UI into the Room
    /// scene: UIDocument, event system, the call and day directors, the interactor and the four
    /// things the player can use (newspaper, calendar, desk drawer, computer).
    /// </summary>
    public static class UIPipeline
    {
        const string UIRoot = "Assets/_Game/UI";
        const string SpriteDir = UIRoot + "/Sprites";
        const string FontDir = UIRoot + "/Fonts";
        const string FontOut = FontDir + "/Generated";
        const string SettingsDir = UIRoot + "/Settings";
        const string PanelSettingsPath = SettingsDir + "/PS_Game.asset";
        const string TextSettingsPath = SettingsDir + "/PTS_Game.asset";
        const string ThemePath = SettingsDir + "/DCM_Theme.tss";
        const string SkinPath = SettingsDir + "/UISkin.asset";
        const string ScenePath = "Assets/_Game/Scenes/Room.unity";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        static string SlicesJson => Path.Combine(Application.dataPath, "_Game", "UI", "Sprites", "slices.json");

        [Serializable]
        class SliceEntry
        {
            public string name;
            public int[] border;
        }

        [Serializable]
        class SliceList
        {
            public SliceEntry[] slices;
        }

        [MenuItem("Tools/Don't Call Me/UI/Run UI Pipeline")]
        public static void RunAll()
        {
            ImportSprites();
            BuildFonts();
            BuildSettings();
            ContentBuilder.Build();
            SetupScene();
        }

        // ---------------------------------------------------------------- sprites

        [MenuItem("Tools/Don't Call Me/UI/1. Import Sprites")]
        public static void ImportSprites()
        {
            AssetDatabase.Refresh();
            var slices = new Dictionary<string, int[]>();
            if (File.Exists(SlicesJson))
                foreach (var s in JsonUtility.FromJson<SliceList>(File.ReadAllText(SlicesJson)).slices)
                    slices[s.name] = s.border;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                bool pixel = name.StartsWith("pt_") || name == "wallpaper";
                bool tile = name.StartsWith("tile_");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = 100;
                if (slices.TryGetValue(name, out var b))
                    importer.spriteBorder = new Vector4(b[0], b[3], b[2], b[1]);   // left, bottom, right, top
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- fonts

        static readonly string[] SerifFonts = { "DMSerifDisplay-Regular", "CrimsonText-Regular", "CrimsonText-Bold", "CrimsonText-Italic" };

        [MenuItem("Tools/Don't Call Me/UI/2. Build Font Assets")]
        public static void BuildFonts()
        {
            EnsureFolder(FontOut);
            var created = new Dictionary<string, FontAsset>();
            foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(FontOut))
                    continue;
                string name = Path.GetFileNameWithoutExtension(path);
                string outPath = $"{FontOut}/{name}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(outPath);
                if (existing != null)
                {
                    created[name] = existing;
                    continue;
                }
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                var fa = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, UnityEngine.TextCore.Text.AtlasPopulationMode.Dynamic, true);
                if (fa == null)
                {
                    Debug.LogError($"[UIPipeline] Could not create a font asset for {path}");
                    continue;
                }
                fa.name = name;
                AssetDatabase.CreateAsset(fa, outPath);
                if (fa.material != null)
                {
                    fa.material.name = name + " Material";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                }
                if (fa.atlasTextures != null)
                    for (int i = 0; i < fa.atlasTextures.Length; i++)
                    {
                        if (fa.atlasTextures[i] == null)
                            continue;
                        fa.atlasTextures[i].name = $"{name} Atlas {i}";
                        AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
                    }
                created[name] = fa;
            }
            // Fallbacks: Hangul, ₩ and symbols come from Nanum fonts.
            created.TryGetValue("NanumGothic-Regular", out var gothic);
            created.TryGetValue("NanumMyeongjo-Regular", out var myeongjo);
            foreach (var pair in created)
            {
                var list = new List<FontAsset>();
                if (SerifFonts.Contains(pair.Key) && myeongjo != null)
                    list.Add(myeongjo);
                if (gothic != null && pair.Value != gothic)
                    list.Add(gothic);
                pair.Value.fallbackFontAssetTable = list;
                EditorUtility.SetDirty(pair.Value);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIPipeline] {created.Count} font assets in {FontOut}");
        }

        // ---------------------------------------------------------------- panel settings and skin

        [MenuItem("Tools/Don't Call Me/UI/3. Build Panel Settings and Skin")]
        public static void BuildSettings()
        {
            EnsureFolder(SettingsDir);
            AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
                Debug.LogError($"[UIPipeline] Missing theme {ThemePath}");
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelSettingsPath);
            }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            ps.clearColor = false;
            ps.textSettings = BuildTextSettings();
            EditorUtility.SetDirty(ps);

            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }
            skin.textures = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null)
                .OrderBy(t => t.name)
                .ToList();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The default text settings, except that Korean wraps between words (어절) as Korean text
        /// should, not between any two syllables.
        /// </summary>
        static PanelTextSettings BuildTextSettings()
        {
            var text = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(TextSettingsPath);
            if (text == null)
            {
                text = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(text, TextSettingsPath);
            }
            var so = new SerializedObject(text);
            so.FindProperty("m_UnicodeLineBreakingRules.m_UseModernHangulLineBreakingRules").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        // ---------------------------------------------------------------- scene

        /// <summary>The things in the room the player can use, by the name of their object in the room model.</summary>
        static readonly (string obj, PanelId panel, string prompt, string plain)[] Interactables =
        {
            ("INT_Newspaper", PanelId.Newspaper, "Read the newspaper", ""),
            ("Board_Calendar", PanelId.Calendar, "Look at the calendar", ""),
            ("INT_Drawer", PanelId.Drawer, "Open the drawer", ""),
            // The charger cable trails off the desk: it stays out of the highlight.
            ("Laptop", PanelId.Computer, "Use the computer", "Charger"),
        };

        /// <summary>Room objects that were interactable before; they are scenery now.</summary>
        static readonly string[] Scenery = { "INT_Wallet", "INT_BulletinBoard", "INT_Rulebook" };

        [MenuItem("Tools/Don't Call Me/UI/4. Set Up Game UI in Room Scene")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Populate();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Adds the game UI to the open Room scene. The art pipeline calls this after it rebuilds the scene.</summary>
        public static void Populate()
        {
            foreach (string n in new[] { "GameUI", "EventSystem", "Flow" })
            {
                var old = GameObject.Find(n);
                if (old != null)
                    UnityEngine.Object.DestroyImmediate(old);
            }
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions != null)
                module.actionsAsset = actions;

            var flow = new GameObject("Flow");
            var director = flow.AddComponent<CallDirector>();
            var dayDirector = flow.AddComponent<DayDirector>();
            var music = flow.AddComponent<DontCallMe.Audio.MusicPlayer>();
            var mso = new SerializedObject(music);
            mso.FindProperty("playOnStart").boolValue = false;
            mso.FindProperty("level").floatValue = 0.7f;
            mso.ApplyModifiedPropertiesWithoutUndo();

            // Where the day starts: seated at the desk, and where the player stands up to. The chair
            // itself stands pushed back behind both, so the player can walk along the desk.
            var seat = new GameObject("Seat").transform;
            seat.SetParent(flow.transform);
            seat.SetPositionAndRotation(DontCallMe.Editor.Art.RoomArtPipeline.FromBlender(new Vector3(0.86f, 1.02f, 0f)), Quaternion.Euler(14f, 2f, 0f));
            var stand = new GameObject("StandSpot").transform;
            stand.SetParent(flow.transform);
            stand.position = DontCallMe.Editor.Art.RoomArtPipeline.FromBlender(new Vector3(0.86f, 0.82f, 0f));

            var uiGo = new GameObject("GameUI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            uiGo.AddComponent<Sfx>();
            var ui = uiGo.AddComponent<UIManager>();
            var so = new SerializedObject(ui);
            so.FindProperty("phoneContent").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PhoneContent>(ContentBuilder.PhonePath);
            so.FindProperty("directory").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WorldDirectory>(ContentBuilder.DirectoryPath);
            so.FindProperty("roomContent").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RoomContent>(ContentBuilder.RoomPath);
            so.FindProperty("skin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UISkin>(SkinPath);
            so.FindProperty("voices").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VoiceBank>(DontCallMe.Editor.Audio.AudioPipeline.BankPath);
            so.FindProperty("phoneAction").objectReferenceValue = FindAction("Player", "Phone");
            so.FindProperty("backAction").objectReferenceValue = FindAction("Player", "Back");
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("director").objectReferenceValue = director;
            so.ApplyModifiedPropertiesWithoutUndo();

            var dso = new SerializedObject(director);
            dso.FindProperty("ui").objectReferenceValue = ui;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var dayDso = new SerializedObject(dayDirector);
            dayDso.FindProperty("day").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DayData>(ContentBuilder.DayPath);
            dayDso.FindProperty("ui").objectReferenceValue = ui;
            dayDso.FindProperty("director").objectReferenceValue = director;
            dayDso.FindProperty("player").objectReferenceValue = player;
            dayDso.FindProperty("music").objectReferenceValue = music;
            dayDso.FindProperty("calmLoop").objectReferenceValue = DontCallMe.Editor.Audio.AudioPipeline.Music("investigate_calm");
            dayDso.FindProperty("tenseLoop").objectReferenceValue = DontCallMe.Editor.Audio.AudioPipeline.Music("investigate_tense");
            dayDso.FindProperty("seat").objectReferenceValue = seat;
            dayDso.FindProperty("standSpot").objectReferenceValue = stand;
            dayDso.ApplyModifiedPropertiesWithoutUndo();

            if (player != null)
            {
                // An interactor saved before its script had a file of its own is a dead component: clear it out.
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player.gameObject);
                var interactor = player.GetComponent<Interactor>();
                if (interactor == null)
                    interactor = player.gameObject.AddComponent<Interactor>();
                var iso = new SerializedObject(interactor);
                iso.FindProperty("view").objectReferenceValue = player.GetComponentInChildren<Camera>();
                iso.FindProperty("ui").objectReferenceValue = ui;
                iso.FindProperty("player").objectReferenceValue = player;
                iso.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[UIPipeline] No player in the scene; the interactor was not added.");
            }

            foreach (string name in Scenery)
            {
                var go = FindInScene(name);
                if (go == null)
                    continue;
                var old = go.GetComponent<Interactable>();
                if (old != null)
                    UnityEngine.Object.DestroyImmediate(old);
                // Its box would swallow clicks meant for what hangs on it (the calendar on the board).
                var box = go.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.enabled = false;
                    EditorUtility.SetDirty(box);
                }
            }
            foreach (var (obj, panel, prompt, plain) in Interactables)
            {
                var go = FindInScene(obj);
                if (go == null)
                {
                    Debug.LogWarning($"[UIPipeline] {obj} not found in the scene");
                    continue;
                }
                if (go.GetComponent<Collider>() == null)
                    AddBox(go, plain);
                var it = go.GetComponent<Interactable>();
                if (it == null)
                    it = go.AddComponent<Interactable>();
                it.panel = panel;
                it.prompt = prompt;
                it.plain = plain;
                EditorUtility.SetDirty(it);
            }
        }

        static GameObject FindInScene(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                return go;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == name)
                    return t.gameObject;
            return null;
        }

        /// <summary>A box around everything the object draws, a little thicker than paper so the cursor finds it.</summary>
        /// <summary>A box around the object's meshes, leaving out parts whose name contains <paramref name="skip"/> (a trailing cable).</summary>
        static void AddBox(GameObject go, string skip = "")
        {
            var toLocal = go.transform.worldToLocalMatrix;
            bool any = false;
            var bounds = new Bounds();
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || (!string.IsNullOrEmpty(skip) && filter.name.Contains(skip)))
                    continue;
                var m = toLocal * filter.transform.localToWorldMatrix;
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (any)
                        bounds.Encapsulate(p);
                    else
                        bounds = new Bounds(p, Vector3.zero);
                    any = true;
                }
            }
            if (!any)
                return;
            var box = go.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, new Vector3(0.02f, 0.02f, 0.02f));
        }

        static InputActionReference FindAction(string map, string action)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.name == action && r.action.actionMap.name == map);
            if (reference == null)
                Debug.LogError($"[UIPipeline] Input action {map}/{action} not found in {InputActionsPath}");
            return reference;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
