using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.Editor.Tools
{
    /// <summary>
    /// Screenshots of the running game at a fixed 1920 × 1080 that do not depend on the Game view
    /// being visible: the main camera renders into one texture, the UI Toolkit panel into another
    /// (for one stepped frame), and the two are composited. Play mode must be paused (it steps).
    /// </summary>
    public static class GameCapture
    {
        public static bool Capture(string path, int width = 1920, int height = 1080)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[GameCapture] Enter Play mode first.");
                return false;
            }
            EditorApplication.isPaused = true;

            var cam = Camera.main;
            var world = new Texture2D(width, height, TextureFormat.RGBA32, false);
            if (cam != null)
            {
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var previous = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previous;
                Read(rt, world);
                RenderTexture.ReleaseTemporary(rt);
            }

            var ui = new Texture2D(width, height, TextureFormat.RGBA32, false);
            bool hasUi = false;
            var doc = Object.FindAnyObjectByType<UIDocument>();
            if (doc != null && doc.panelSettings != null)
            {
                var ps = doc.panelSettings;
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var previous = ps.targetTexture;
                bool clear = ps.clearColor;
                var clearValue = ps.colorClearValue;
                ps.targetTexture = rt;
                ps.clearColor = true;
                ps.colorClearValue = new Color(0f, 0f, 0f, 0f);
                // Two frames: one to lay out at the texture's size, one to draw.
                EditorApplication.Step();
                EditorApplication.Step();
                ps.targetTexture = previous;
                ps.clearColor = clear;
                ps.colorClearValue = clearValue;
                Read(rt, ui);
                Object.DestroyImmediate(rt);
                EditorApplication.Step();
                hasUi = true;
            }

            var a = world.GetPixels32();
            if (hasUi)
            {
                var b = ui.GetPixels32();
                for (int i = 0; i < a.Length; i++)
                {
                    // UI Toolkit writes premultiplied colour.
                    float k = 1f - b[i].a / 255f;
                    a[i] = new Color32((byte)Mathf.Min(255, b[i].r + a[i].r * k), (byte)Mathf.Min(255, b[i].g + a[i].g * k),
                                       (byte)Mathf.Min(255, b[i].b + a[i].b * k), 255);
                }
            }
            world.SetPixels32(a);
            world.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, world.EncodeToPNG());
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(ui);
            return true;
        }

        static void Read(RenderTexture rt, Texture2D into)
        {
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            into.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            into.Apply();
            RenderTexture.active = active;
        }
    }
}
