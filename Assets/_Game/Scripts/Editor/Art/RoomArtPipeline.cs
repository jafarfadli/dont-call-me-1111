using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DontCallMe.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DontCallMe.Editor.Art
{
    /// <summary>
    /// Turns the generated art (Tools/ArtGen) into Unity assets: texture settings,
    /// materials from materials.json, the bedroom model, its prefab, renderer setup and the Room scene.
    /// </summary>
    public static class RoomArtPipeline
    {
        const string ArtRoot = "Assets/_Game/Art";
        const string TextureDir = ArtRoot + "/Textures";
        const string MaterialDir = ArtRoot + "/Materials";
        const string ModelPath = ArtRoot + "/Models/SM_Bedroom.fbx";
        const string PrefabPath = "Assets/_Game/Prefabs/Room/PF_Bedroom.prefab";
        const string RenderingDir = "Assets/_Game/Rendering";
        const string InkMaterialPath = RenderingDir + "/M_InkComposite.mat";
        const string VolumeProfilePath = RenderingDir + "/VP_Room.asset";
        const string ScenePath = "Assets/_Game/Scenes/Room.unity";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";

        static string MaterialsJsonPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "ArtGen", "materials.json"));

        static readonly HashSet<string> DataTextures = new HashSet<string>
            { "T_Ink_Wood", "T_Ink_Plaster", "T_Ink_Fabric", "T_Ink_Tabby", "T_Ink_Brick", "T_PaperGrain" };

        static readonly HashSet<string> TilingTextures = new HashSet<string>
            { "T_Ink_Wood", "T_Ink_Plaster", "T_Ink_Fabric", "T_Ink_Tabby", "T_Ink_Brick", "T_PaperGrain", "T_Floor_Vinyl", "T_Bedding",
              "T_Curtain_Floral", "T_Cork" };

        static readonly HashSet<string> AlphaTextures = new HashSet<string> { "T_Leaves_Atlas" };

        /// <summary>Furniture the player walks around. Their colliders are knee-high boxes, so they block
        /// walking but not a look (or a click) at things standing on them.</summary>
        static readonly string[] Obstacles = { "Bed", "Wardrobe", "Bookshelf", "Desk", "DeskChair", "CornerShelf", "TVCabinet",
                                               "StandingFan", "Dehumidifier", "TrashBin", "FloorMirror" };
        const float ObstacleHeight = 0.5f;

        [Serializable]
        class MaterialSpec
        {
            public string name;
            public string shader = "toon";
            public string color = "#FFFFFF";
            public string texture;
            public string detail;
            public int detail_mode = 1;
            public float detail_scale = 1f;
            public float detail_strength = 0.8f;
            public float hatch = 0.3f;
            public float glint;
            public float glint_size = 0.06f;
            public float rim;
            public float light_threshold;
            public bool alpha_clip;
            public int cull = 2;
            public string emission;
            public float emission_intensity = 1f;
            public string tint = "#CFE8EC";
            public float tint_alpha = 0.12f;
            public float edge_alpha = 0.35f;
            public float streak_alpha = 0.45f;
            public float streak_frequency = 1.4f;
            public float brightness = 1f;
        }

        [Serializable]
        class MaterialList
        {
            public MaterialSpec[] materials;
        }

        [MenuItem("Tools/Don't Call Me/Art/Run Full Pipeline")]
        public static void RunAll()
        {
            ConfigureTextures();
            BuildMaterials();
            ImportModel();
            BuildPrefab();
            SetupRenderer();
            BuildScene();
        }

        [MenuItem("Tools/Don't Call Me/Art/1. Configure Textures")]
        public static void ConfigureTextures()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                bool data = DataTextures.Contains(name);
                bool alpha = AlphaTextures.Contains(name);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = !data;
                importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                importer.alphaIsTransparency = alpha;
                importer.mipMapsPreserveCoverage = alpha;
                importer.alphaTestReferenceValue = 0.5f;
                importer.wrapMode = TilingTextures.Contains(name) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.mipmapEnabled = name != "T_PaperGrain";
                importer.filterMode = name == "T_PaperGrain" ? FilterMode.Bilinear : FilterMode.Trilinear;
                importer.anisoLevel = TilingTextures.Contains(name) ? 8 : 4;
                importer.maxTextureSize = 2048;
                importer.textureCompression = data ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Tools/Don't Call Me/Art/2. Build Materials")]
        public static void BuildMaterials()
        {
            EnsureFolder(MaterialDir);
            var list = JsonUtility.FromJson<MaterialList>(File.ReadAllText(MaterialsJsonPath));
            var toon = Shader.Find("DontCallMe/Toon");
            var glass = Shader.Find("DontCallMe/ToonGlass");
            var backdrop = Shader.Find("DontCallMe/Backdrop");
            int inkIndex = 0;
            foreach (var spec in list.materials)
            {
                Shader shader = spec.shader == "glass" ? glass : spec.shader == "backdrop" ? backdrop : toon;
                string path = $"{MaterialDir}/{spec.name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.shader = shader;
                if (shader == glass)
                    ApplyGlass(mat, spec);
                else if (shader == backdrop)
                    ApplyBackdrop(mat, spec);
                else
                {
                    ApplyToon(mat, spec);
                    // One 8-bit step apart (normals alpha is R8 SNorm) so every material gets its own outline id.
                    mat.SetFloat("_InkId", (inkIndex++ + 5) / 127f);
                }
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Lists generated art the pipeline no longer produces (materials missing from materials.json,
        /// textures no material uses, older room models and prefabs) and deletes it after confirmation.
        /// </summary>
        [MenuItem("Tools/Don't Call Me/Art/Remove Stale Art...")]
        public static void RemoveStaleArt()
        {
            var list = JsonUtility.FromJson<MaterialList>(File.ReadAllText(MaterialsJsonPath));
            var materials = new HashSet<string>(list.materials.Select(m => m.name));
            var textures = new HashSet<string>(list.materials.SelectMany(m => new[] { m.texture, m.detail }).Where(t => !string.IsNullOrEmpty(t)))
                { "T_PaperGrain" };
            var stale = new List<string>();
            stale.AddRange(AssetDatabase.FindAssets("t:Material", new[] { MaterialDir }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !materials.Contains(Path.GetFileNameWithoutExtension(p))));
            stale.AddRange(AssetDatabase.FindAssets("t:Texture2D", new[] { TextureDir }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !textures.Contains(Path.GetFileNameWithoutExtension(p))));
            stale.AddRange(AssetDatabase.FindAssets("t:Model", new[] { ArtRoot + "/Models" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != ModelPath));
            stale.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { Path.GetDirectoryName(PrefabPath).Replace('\\', '/') })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p != PrefabPath));
            stale = stale.Distinct().ToList();
            if (stale.Count == 0)
            {
                EditorUtility.DisplayDialog("Remove Stale Art", "Nothing to remove.", "OK");
                return;
            }
            string shown = string.Join("\n", stale.Take(40)) + (stale.Count > 40 ? $"\n... and {stale.Count - 40} more" : "");
            if (!EditorUtility.DisplayDialog("Remove Stale Art", $"Delete {stale.Count} assets the art pipeline no longer uses?\n\n{shown}",
                    "Delete", "Cancel"))
                return;
            foreach (string path in stale)
                AssetDatabase.DeleteAsset(path);
            Debug.Log($"[RoomArtPipeline] Removed {stale.Count} stale assets:\n{string.Join("\n", stale)}");
        }

        static void ApplyToon(Material mat, MaterialSpec spec)
        {
            mat.SetColor("_BaseColor", Hex(spec.color));
            mat.SetTexture("_BaseMap", LoadTexture(spec.texture));
            var detail = LoadTexture(spec.detail);
            mat.SetTexture("_DetailMap", detail);
            mat.SetFloat("_DetailMode", spec.detail_mode);
            mat.SetFloat("_DetailScale", spec.detail_scale);
            mat.SetFloat("_DetailStrength", detail != null ? spec.detail_strength : 0f);
            mat.SetFloat("_HatchStrength", spec.hatch);
            mat.SetFloat("_HatchDensity", 38f);
            mat.SetFloat("_HatchWidth", 0.2f);
            mat.SetColor("_GlintColor", new Color(1f, 0.98f, 0.93f, spec.glint));
            mat.SetFloat("_GlintSize", spec.glint_size);
            mat.SetColor("_RimColor", new Color(1f, 0.96f, 0.88f, spec.rim));
            mat.SetFloat("_LightThreshold", spec.light_threshold);
            mat.SetFloat("_Cull", spec.cull);
            mat.SetFloat("_AlphaClip", spec.alpha_clip ? 1f : 0f);
            mat.SetFloat("_Cutoff", 0.5f);
            if (spec.alpha_clip)
            {
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                mat.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else
            {
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = -1;
                mat.SetOverrideTag("RenderType", "");
            }
            Color emission = string.IsNullOrEmpty(spec.emission) ? Color.black : Hex(spec.emission) * spec.emission_intensity;
            mat.SetColor("_EmissionColor", emission);
            mat.enableInstancing = true;
        }

        static void ApplyGlass(Material mat, MaterialSpec spec)
        {
            Color tint = Hex(spec.tint);
            tint.a = spec.tint_alpha;
            mat.SetColor("_TintColor", tint);
            mat.SetColor("_EdgeColor", new Color(1f, 1f, 1f, spec.edge_alpha));
            mat.SetColor("_StreakColor", new Color(1f, 1f, 1f, spec.streak_alpha));
            mat.SetFloat("_StreakFrequency", spec.streak_frequency);
            mat.SetVector("_StreakDirection", new Vector4(1f, 1.3f, 0.8f, 0f));
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void ApplyBackdrop(Material mat, MaterialSpec spec)
        {
            mat.SetTexture("_BaseMap", LoadTexture(spec.texture));
            mat.SetColor("_BaseColor", Hex(spec.color));
            mat.SetFloat("_Brightness", spec.brightness);
        }

        [MenuItem("Tools/Don't Call Me/Art/3. Import Room Model")]
        public static void ImportModel()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.generateSecondaryUV = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();

            var names = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().Select(m => m.name).ToList();
            foreach (var pair in importer.GetExternalObjectMap())
                if (pair.Key.type == typeof(Material) && !names.Contains(pair.Key.name))
                    names.Add(pair.Key.name);
            int mapped = 0;
            foreach (string name in names.Distinct())
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{name}.mat");
                if (mat == null)
                {
                    Debug.LogWarning($"[RoomArtPipeline] No material for '{name}'");
                    continue;
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
                mapped++;
            }
            importer.SaveAndReimport();
            Debug.Log($"[RoomArtPipeline] Remapped {mapped} materials on {ModelPath}");
        }

        [MenuItem("Tools/Don't Call Me/Art/4. Build Room Prefab")]
        public static void BuildPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    bool noShadow = renderer.sharedMaterials.Any(m => m != null &&
                        (m.shader.name == "DontCallMe/ToonGlass" || m.shader.name == "DontCallMe/Backdrop"));
                    renderer.shadowCastingMode = noShadow ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    bool moving = renderer.GetComponentsInParent<Transform>(true).Any(t => t.name.StartsWith("INT_"));
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, moving ? 0 : StaticEditorFlags.BatchingStatic);
                }
                AddColliders(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static void AddColliders(GameObject root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                string n = t.name;
                bool shell = n == "Floor" || n == "Ceiling" || n.StartsWith("Wall_") || n == "Door_Slab";
                if (shell && t.GetComponent<MeshFilter>() != null && t.GetComponent<Collider>() == null)
                    t.gameObject.AddComponent<MeshCollider>();
            }
            foreach (string name in Obstacles)
            {
                var t = all.FirstOrDefault(x => x.name == name);
                if (t == null)
                {
                    Debug.LogWarning($"[RoomArtPipeline] Obstacle '{name}' not found in the room model");
                    continue;
                }
                if (t.GetComponent<Collider>() != null)
                    continue;
                // Imported groups keep Blender's Z-up axes, so find the local axis that points up
                // and cut the box to knee height along it, starting at the floor.
                var bounds = LocalBounds(t);
                Vector3 up = t.InverseTransformDirection(Vector3.up);
                int axis = Mathf.Abs(up.x) > Mathf.Abs(up.y)
                    ? (Mathf.Abs(up.x) > Mathf.Abs(up.z) ? 0 : 2)
                    : (Mathf.Abs(up.y) > Mathf.Abs(up.z) ? 1 : 2);
                float floor = t.InverseTransformPoint(new Vector3(t.position.x, 0f, t.position.z))[axis];
                Vector3 center = bounds.center, size = bounds.size;
                center[axis] = floor + Mathf.Sign(up[axis]) * ObstacleHeight / 2f;
                size[axis] = ObstacleHeight;
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
            }
            // Things the player will pick up or open get their own tight boxes (parts inside them do not).
            foreach (var t in all.Where(x => x.name.StartsWith("INT_")))
            {
                bool inside = false;
                for (var p = t.parent; p != null; p = p.parent)
                    inside |= p.name.StartsWith("INT_");
                if (inside || t.GetComponent<Collider>() != null)
                    continue;
                var bounds = LocalBounds(t);
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size, new Vector3(0.01f, 0.01f, 0.01f));
            }
        }

        static Bounds LocalBounds(Transform root)
        {
            var world2Local = root.worldToLocalMatrix;
            bool any = false;
            var bounds = new Bounds();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                    continue;
                var m = world2Local * mf.transform.localToWorldMatrix;
                var b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                    else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }

        [MenuItem("Tools/Don't Call Me/Art/5. Setup Renderer")]
        public static void SetupRenderer()
        {
            EnsureFolder(RenderingDir);
            var ink = AssetDatabase.LoadAssetAtPath<Material>(InkMaterialPath);
            if (ink == null)
            {
                ink = new Material(Shader.Find("Hidden/DontCallMe/InkComposite"));
                AssetDatabase.CreateAsset(ink, InkMaterialPath);
            }
            ink.SetTexture("_GrainTex", LoadTexture("T_PaperGrain"));
            ink.SetColor("_InkColor", new Color(0.17f, 0.11f, 0.08f, 1f));
            ink.SetFloat("_Thickness", 1f);
            ink.SetFloat("_DepthThreshold", 0.008f);
            ink.SetFloat("_NormalThreshold", 0.45f);
            ink.SetFloat("_IdStrength", 0.9f);
            ink.SetFloat("_CreaseStrength", 0.85f);
            ink.SetFloat("_InkVariation", 0.3f);
            ink.SetFloat("_GrainStrength", 0.12f);
            ink.SetFloat("_FadeStart", 14f);
            ink.SetFloat("_FadeEnd", 40f);
            ink.SetColor("_PaperTint", new Color(1f, 0.965f, 0.9f, 1f));
            EditorUtility.SetDirty(ink);

            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == "DCM Ink");
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = "DCM Ink";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            feature.passMaterial = ink;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.fetchColorBuffer = true;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);

            var ssao = data.rendererFeatures.FirstOrDefault(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            if (ssao != null)
            {
                var so = new SerializedObject(ssao);
                SetProp(so, "m_Settings.AfterOpaque", false);
                SetProp(so, "m_Settings.Source", 1);
                SetProp(so, "m_Settings.Downsample", false);
                SetProp(so, "m_Settings.Intensity", 1.4f);
                SetProp(so, "m_Settings.Radius", 0.12f);
                SetProp(so, "m_Settings.DirectLightingStrength", 0f);
                SetProp(so, "m_Settings.Falloff", 25f);
                SetProp(so, "m_Settings.Samples", 0);
                SetProp(so, "m_Settings.BlurQuality", 0);
                so.ApplyModifiedPropertiesWithoutUndo();
                ssao.SetActive(true);
            }
            EditorUtility.SetDirty(data);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var pso = new SerializedObject(pipeline);
            SetProp(pso, "m_ShadowDistance", 24f);
            SetProp(pso, "m_ShadowCascadeCount", 2);
            SetProp(pso, "m_Cascade2Split", 0.35f);
            SetProp(pso, "m_MainLightShadowmapResolution", 4096);
            SetProp(pso, "m_AdditionalLightsShadowmapResolution", 2048);
            SetProp(pso, "m_SoftShadowQuality", 1);
            SetProp(pso, "m_ShadowDepthBias", 0.6f);
            SetProp(pso, "m_ShadowNormalBias", 0.4f);
            pso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }

        static void SetProp(SerializedObject so, string path, object value)
        {
            var p = so.FindProperty(path);
            if (p == null)
            {
                Debug.LogWarning($"[RoomArtPipeline] Missing property {path} on {so.targetObject.name}");
                return;
            }
            switch (value)
            {
                case bool b: p.boolValue = b; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
            }
        }

        [MenuItem("Tools/Don't Call Me/Art/6. Build Room Scene")]
        public static void BuildScene()
        {
            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var room = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            room.name = "Bedroom";

            var lighting = new GameObject("Lighting");
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(lighting.transform);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.8f, 0.56f);
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.2f;
            // Late-afternoon sun low in the west, through the window over the bed.
            sunGo.transform.rotation = Quaternion.LookRotation(FromBlender(new Vector3(0.78f, -0.3f, -0.55f)).normalized, Vector3.up);
            RenderSettings.sun = sun;

            var fillGo = new GameObject("WindowFill");
            fillGo.transform.SetParent(lighting.transform);
            fillGo.transform.rotation = Quaternion.LookRotation(FromBlender(new Vector3(0.85f, -0.1f, -0.5f)).normalized, Vector3.up);
            fillGo.AddComponent<DCMLook>();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.53f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.56f, 0.59f);
            RenderSettings.ambientGroundColor = new Color(0.74f, 0.63f, 0.51f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0f;

            BuildSunDust(lighting.transform);

            var volumeGo = new GameObject("PostFX");
            volumeGo.transform.SetParent(lighting.transform);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BuildVolumeProfile();

            BuildPlayer(new Vector3(1.1f, -1.3f, 0f), new Vector3(-1.2f, 0.9f, 0f));

            var audit = new GameObject("AuditCameras");
            AddAuditCamera(audit.transform, "Audit_Overview", new Vector3(1.45f, -1.55f, 1.62f), new Vector3(-1.0f, 1.0f, 0.95f), 70f);
            AddAuditCamera(audit.transform, "Audit_WindowBed", new Vector3(0.45f, -0.35f, 1.55f), new Vector3(-1.8f, 0.9f, 1.15f));
            AddAuditCamera(audit.transform, "Audit_Desk", new Vector3(0.55f, 0.3f, 1.55f), new Vector3(1.0f, 1.9f, 1.0f));
            AddAuditCamera(audit.transform, "Audit_Board", new Vector3(0.92f, 0.85f, 1.45f), new Vector3(0.92f, 1.9f, 1.4f), 50f);
            AddAuditCamera(audit.transform, "Audit_DeskTop", new Vector3(0.92f, 1.0f, 1.3f), new Vector3(0.92f, 1.62f, 0.74f), 50f);
            AddAuditCamera(audit.transform, "Audit_Drawer", new Vector3(1.05f, 0.75f, 1.05f), new Vector3(1.12f, 1.62f, 0.62f), 45f);
            AddAuditCamera(audit.transform, "Audit_Door", new Vector3(-0.6f, 1.0f, 1.6f), new Vector3(0.9f, -1.9f, 1.05f), 65f);
            AddAuditCamera(audit.transform, "Audit_Wardrobe", new Vector3(0.45f, 0.65f, 1.6f), new Vector3(-1.6f, -1.3f, 1.15f), 65f);
            AddAuditCamera(audit.transform, "Audit_EastWall", new Vector3(-0.6f, -0.2f, 1.6f), new Vector3(1.8f, 0.2f, 1.25f), 65f);
            AddAuditCamera(audit.transform, "Audit_Outside", new Vector3(-0.55f, 0.85f, 1.58f), new Vector3(-8.0f, 0.9f, 0.9f), 55f);
            AddAuditCamera(audit.transform, "Audit_SunPatch", new Vector3(1.3f, -1.1f, 1.5f), new Vector3(-0.2f, 0.3f, 0.35f), 60f);
            AddAuditCamera(audit.transform, "Audit_Bed", new Vector3(-0.3f, -0.4f, 1.35f), new Vector3(-1.2f, 1.0f, 0.55f), 55f);

            // The game UI (UIDocument, directors, interactor, interactables) lives in the same scene.
            DontCallMe.Editor.UI.UIPipeline.Populate();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.All(s => s.path != ScenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
        }

        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        static void BuildPlayer(Vector3 blenderFeet, Vector3 blenderLookAt)
        {
            var player = new GameObject("Player");
            var feet = FromBlender(blenderFeet);
            var forward = FromBlender(blenderLookAt) - feet;
            forward.y = 0f;
            player.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(forward.normalized, Vector3.up));

            var body = player.AddComponent<CharacterController>();
            body.height = 1.7f;
            body.radius = 0.25f;
            body.center = new Vector3(0f, 0.85f, 0f);
            body.stepOffset = 0.25f;
            body.slopeLimit = 45f;
            body.skinWidth = 0.02f;
            body.minMoveDistance = 0f;

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.58f, 0f);
            pivot.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(pivot.transform, false);
            var cam = cameraGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.66f, 0.71f, 0.78f);
            var camData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            cameraGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<DontCallMe.Player.FirstPersonController>();
            var so = new SerializedObject(controller);
            so.FindProperty("move").objectReferenceValue = FindAction("Player", "Move");
            so.FindProperty("look").objectReferenceValue = FindAction("Player", "Look");
            so.FindProperty("drag").objectReferenceValue = FindAction("Player", "Drag");
            so.FindProperty("sprint").objectReferenceValue = FindAction("Player", "Sprint");
            so.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static UnityEngine.InputSystem.InputActionReference FindAction(string map, string action)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<UnityEngine.InputSystem.InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.name == action && r.action.actionMap.name == map);
            if (reference == null)
                Debug.LogError($"[RoomArtPipeline] Input action {map}/{action} not found in {InputActionsPath}");
            return reference;
        }

        static void BuildSunDust(Transform parent)
        {
            const string motePath = RenderingDir + "/M_SunMote.mat";
            var mote = AssetDatabase.LoadAssetAtPath<Material>(motePath);
            if (mote == null)
            {
                mote = new Material(Shader.Find("DontCallMe/SunMote"));
                AssetDatabase.CreateAsset(mote, motePath);
            }
            mote.SetColor("_Color", new Color(1f, 0.93f, 0.78f, 1f));
            mote.SetFloat("_Intensity", 2.6f);
            mote.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mote);

            var go = new GameObject("SunDust");
            go.transform.SetParent(parent);
            // Fill the sunbeam between the window and the floor patch.
            go.transform.position = FromBlender(new Vector3(-0.75f, 0.45f, 1.05f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
            main.startColor = new Color(1f, 0.96f, 0.86f, 0.9f);
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.002f;
            var emission = ps.emission;
            emission.rateOverTime = 55f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(2.4f, 1.9f, 2.2f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.035f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.04f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mote;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 0.02f;
            ps.Play();
        }

        static VolumeProfile BuildVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            foreach (var c in profile.components.ToList())
            {
                profile.Remove(c.GetType());
                Object.DestroyImmediate(c, true);
            }

            // Retro print grade: warm faded highlights, blue shadows, lifted blacks, muted greens, grain.
            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);
            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1.3f);
            bloom.intensity.Override(0.16f);
            bloom.scatter.Override(0.4f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.76f));
            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.3f);
            color.contrast.Override(6f);
            color.saturation.Override(2f);
            color.colorFilter.Override(new Color(1f, 0.975f, 0.93f));
            var wb = Add<WhiteBalance>(profile);
            wb.temperature.Override(8f);
            var mixer = Add<ChannelMixer>(profile);
            mixer.redOutRedIn.Override(102f);
            mixer.redOutGreenIn.Override(4f);
            mixer.greenOutRedIn.Override(8f);
            mixer.greenOutGreenIn.Override(86f);
            mixer.greenOutBlueIn.Override(4f);
            mixer.blueOutGreenIn.Override(4f);
            mixer.blueOutBlueIn.Override(96f);
            var split = Add<SplitToning>(profile);
            split.shadows.Override(new Color(0.33f, 0.43f, 0.6f));
            split.highlights.Override(new Color(0.93f, 0.72f, 0.48f));
            split.balance.Override(-15f);
            var lgg = Add<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(1f, 0.99f, 1.03f, 0.045f));
            lgg.gain.Override(new Vector4(1.02f, 1f, 0.96f, 0f));
            var smh = Add<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(0.95f, 0.97f, 1.08f, 0f));
            smh.highlights.Override(new Vector4(1.05f, 1.0f, 0.93f, 0f));
            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium2);
            grain.intensity.Override(0.22f);
            grain.response.Override(0.7f);
            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.18f, 0.1f, 0.06f));
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        static void AddAuditCamera(Transform parent, string name, Vector3 blenderPos, Vector3 blenderTarget, float fov = 62f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.66f, 0.71f, 0.78f);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            cam.enabled = false;
            PlaceCamera(go.transform, blenderPos, blenderTarget);
        }

        static void PlaceCamera(Transform t, Vector3 blenderPos, Vector3 blenderTarget)
        {
            var pos = FromBlender(blenderPos);
            t.position = pos;
            t.rotation = Quaternion.LookRotation(FromBlender(blenderTarget) - pos, Vector3.up);
        }

        /// <summary>Blender (x, y, z) to Unity with Bake Axis Conversion on: (x, z, y). North is +Z, the window wall is -X.</summary>
        public static Vector3 FromBlender(Vector3 b) => new Vector3(b.x, b.z, b.y);

        static Texture2D LoadTexture(string name) =>
            string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}/{name}.png");

        static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c))
                c = Color.magenta;
            return c;
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
