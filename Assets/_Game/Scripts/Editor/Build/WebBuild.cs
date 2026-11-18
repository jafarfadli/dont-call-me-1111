using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DontCallMe.Editor.Build
{
    /// <summary>
    /// The browser build for itch.io. Tools → Don't Call Me → Build → WebGL for itch.io writes the
    /// player to Builds/WebGL and zips it (index.html at the zip's root) as
    /// Builds/DontCallMe-WebGL.zip, the file to upload. It uses the page in
    /// Assets/WebGLTemplates/DontCallMe (the game fills the frame itch.io gives it), gzip with the
    /// decompression fallback, which loads on any host, and the PC quality level. From a shell:
    /// Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod DontCallMe.Editor.Build.WebBuild.Build
    /// </summary>
    public static class WebBuild
    {
        public const string Folder = "Builds/WebGL";
        public const string Zip = "Builds/DontCallMe-WebGL.zip";

        static readonly string[] Scenes =
        {
            "Assets/_Game/Scenes/Home.unity", "Assets/_Game/Scenes/Room.unity", "Assets/_Game/Scenes/End.unity",
        };

        [MenuItem("Tools/Don't Call Me/Build/WebGL for itch.io")]
        public static void Build()
        {
            var previousTarget = EditorUserBuildSettings.activeBuildTarget;
            var previousGroup = BuildPipeline.GetBuildTargetGroup(previousTarget);

            PlayerSettings.WebGL.template = "PROJECT:DontCallMe";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = true;
            UsePcQuality();

            if (Directory.Exists(Folder))
                Directory.Delete(Folder, true);
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = Folder, target = BuildTarget.WebGL, targetGroup = BuildTargetGroup.WebGL, options = BuildOptions.None,
            });
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[WebBuild] The build did not succeed: {summary.result}, {summary.totalErrors} errors.");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            if (File.Exists(Zip))
                File.Delete(Zip);
            ZipFile.CreateFromDirectory(Folder, Zip);
            Debug.Log($"[WebBuild] {Zip} ({new FileInfo(Zip).Length / 1048576} MB), built in {summary.totalTime.TotalSeconds:0} s. Upload it to itch.io as an HTML game.");

            // Building for the web moves the editor to that platform: put it back where it was.
            if (!Application.isBatchMode && previousTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previousTarget);
        }

        /// <summary>
        /// A web build starts on the quality level set for its platform, which the URP template puts
        /// on Mobile: a pipeline asset without the game's renderer (no ink lines, no hatching, no
        /// print grade). The game is made for the PC level, so the web build starts there too.
        /// </summary>
        public static void UsePcQuality()
        {
            int pc = System.Array.IndexOf(QualitySettings.names, "PC");
            if (pc < 0)
                return;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var perPlatform = settings.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                var entry = perPlatform.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == "WebGL")
                    entry.FindPropertyRelative("second").intValue = pc;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
