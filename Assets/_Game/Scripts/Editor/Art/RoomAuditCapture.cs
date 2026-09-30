using System.IO;
using UnityEditor;
using UnityEngine;

namespace DontCallMe.Editor.Art
{
    /// <summary>Renders the Room scene's audit cameras to PNG files for visual review.</summary>
    public static class RoomAuditCapture
    {
        [MenuItem("Tools/Don't Call Me/Art/Capture Audit Views")]
        static void CaptureToTemp() => Capture(Path.GetFullPath("Temp/AuditViews"), 1600, 900);

        public static string[] Capture(string outputDir, int width, int height, string only = null)
        {
            Directory.CreateDirectory(outputDir);
            var root = GameObject.Find("AuditCameras");
            if (root == null)
                return new string[0];
            // Edit mode does not tick particles; advance them so dust shows up in the capture.
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>())
                ps.Simulate(12f, true, true);
            var saved = new System.Collections.Generic.List<string>();
            foreach (Transform child in root.transform)
            {
                if (only != null && !child.name.Contains(only))
                    continue;
                var cam = child.GetComponent<Camera>();
                if (cam == null)
                    continue;
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.Create();
                var previousTarget = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previousTarget;
                var previousActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = previousActive;
                string path = Path.Combine(outputDir, child.name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                rt.Release();
                Object.DestroyImmediate(rt);
                saved.Add(path);
            }
            return saved.ToArray();
        }
    }
}
