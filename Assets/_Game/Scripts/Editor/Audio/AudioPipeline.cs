using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DontCallMe.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DontCallMe.Editor.Audio
{
    /// <summary>
    /// Voices and music. Voice lines come from the content (every caller line, answer, pressure line
    /// and official line that has a voice), go through Tools/Audio/tts.py (macOS `say` + a phone-line
    /// filter) and come back as a <see cref="VoiceBank"/>. Music loops come from Tools/Audio/music.py.
    /// </summary>
    public static class AudioPipeline
    {
        const string LinesJson = "Tools/Audio/voice_lines.json";
        const string ManifestPath = "Assets/_Game/Audio/Voices/manifest.json";
        const string VoicesDir = "Assets/_Game/Audio/Voices";
        const string MusicDir = "Assets/_Game/Audio/Music";
        public const string BankPath = "Assets/_Game/Data/VoiceBank.asset";

        [MenuItem("Tools/Don't Call Me/Audio/Run Voice Pipeline")]
        public static void RunVoices()
        {
            ExportLines();
            if (RunPython("Tools/Audio/tts.py"))
                ImportVoices();
        }

        [MenuItem("Tools/Don't Call Me/Audio/1. Export Voice Lines")]
        public static void ExportLines()
        {
            var lines = new List<(string voice, string text)>();
            foreach (string guid in AssetDatabase.FindAssets("t:ConversationData"))
                Collect(AssetDatabase.LoadAssetAtPath<ConversationData>(AssetDatabase.GUIDToAssetPath(guid)), lines);
            foreach (string guid in AssetDatabase.FindAssets("t:WorldDirectory"))
            {
                var dir = AssetDatabase.LoadAssetAtPath<WorldDirectory>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var cb in dir.callbacks)
                    foreach (var line in cb.lines)
                        Add(lines, cb.voice, line);
            }
            var unique = lines.Where(l => !string.IsNullOrEmpty(l.voice) && !string.IsNullOrWhiteSpace(l.text))
                              .Select(l => (l.voice, text: VoiceBank.Normalize(l.text)))
                              .Distinct()
                              .ToList();
            var json = new StringBuilder("[\n");
            for (int i = 0; i < unique.Count; i++)
                json.Append($" {{\"voice\": \"{Escape(unique[i].voice)}\", \"text\": \"{Escape(unique[i].text)}\"}}{(i < unique.Count - 1 ? "," : "")}\n");
            json.Append("]\n");
            File.WriteAllText(Path.Combine(ProjectRoot, LinesJson), json.ToString(), new UTF8Encoding(false));
            Debug.Log($"[AudioPipeline] {unique.Count} voice lines written to {LinesJson}");
        }

        static void Collect(ConversationData c, List<(string, string)> lines)
        {
            if (c == null)
                return;
            string voice = c.channel == Channel.Call ? c.caller.voice : null;
            foreach (var node in c.nodes)
            {
                foreach (var line in node.lines)
                    Add(lines, voice, line);
                if (node.hasDecision)
                    foreach (var p in node.decision.pressure)
                        lines.Add((voice, string.IsNullOrEmpty(p.spoken) ? p.text : p.spoken));
            }
            foreach (var q in c.questions)
                foreach (var line in q.answer)
                    Add(lines, voice, line);
            foreach (var beat in c.beats)
                Add(lines, voice, beat.line);
            foreach (var ending in c.endings)
            {
                // Endings reached by calling someone are spoken by whoever picks up.
                var trigger = c.actions.Find(a => a.kind == ActionKind.Call && a.endingId == ending.id);
                string v = trigger != null && !string.IsNullOrEmpty(trigger.voice) ? trigger.voice : voice;
                foreach (var line in ending.lines)
                    Add(lines, v, line);
            }
        }

        static void Add(List<(string, string)> lines, string voice, ConvLine line)
        {
            if (line != null && line.speaker == Speaker.Caller && !string.IsNullOrEmpty(voice))
                lines.Add((voice, line.Spoken));
        }

        [MenuItem("Tools/Don't Call Me/Audio/2. Generate Voices (TTS)")]
        public static void GenerateVoices()
        {
            if (RunPython("Tools/Audio/tts.py"))
                ImportVoices();
        }

        [MenuItem("Tools/Don't Call Me/Audio/3. Import Voices")]
        public static void ImportVoices()
        {
            AssetDatabase.Refresh();
            string manifestFile = Path.Combine(ProjectRoot, ManifestPath);
            if (!File.Exists(manifestFile))
            {
                Debug.LogError("[AudioPipeline] No voice manifest. Run Export Voice Lines and Generate Voices first.");
                return;
            }
            var entries = JsonUtility.FromJson<ManifestList>("{\"items\":" + File.ReadAllText(manifestFile) + "}");
            var bank = AssetDatabase.LoadAssetAtPath<VoiceBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<VoiceBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            bank.entries.Clear();
            int missing = 0;
            foreach (var item in entries.items)
            {
                ConfigureVoice(item.file);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(item.file);
                if (clip == null)
                {
                    missing++;
                    continue;
                }
                bank.entries.Add(new VoiceBank.Entry { voice = item.voice, text = item.text, clip = clip });
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioPipeline] VoiceBank: {bank.entries.Count} clips" + (missing > 0 ? $", {missing} missing" : ""));
        }

        static void ConfigureVoice(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                return;
            var s = importer.defaultSampleSettings;
            if (importer.forceToMono && s.loadType == AudioClipLoadType.DecompressOnLoad && s.compressionFormat == AudioCompressionFormat.Vorbis)
                return;
            importer.forceToMono = true;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        [MenuItem("Tools/Don't Call Me/Audio/Generate Music")]
        public static void GenerateMusic()
        {
            if (RunPython("Tools/Audio/music.py"))
                ImportMusic();
        }

        [MenuItem("Tools/Don't Call Me/Audio/Import Music")]
        public static void ImportMusic()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MusicDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                    continue;
                var s = importer.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
                importer.defaultSampleSettings = s;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }
            Debug.Log("[AudioPipeline] Music imported from " + MusicDir);
        }

        public static AudioClip Music(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicDir}/{name}.wav");

        // ---------------------------------------------------------------- helpers

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>Python 3 with numpy: the EditorPrefs "DCM.Python" path if set, else the usual install places.</summary>
        static string Python()
        {
            string custom = EditorPrefs.GetString("DCM.Python", "");
            var candidates = new[]
            {
                custom, "/Library/Frameworks/Python.framework/Versions/Current/bin/python3", "/usr/local/bin/python3",
                "/opt/homebrew/bin/python3", "/usr/bin/python3",
            };
            return candidates.FirstOrDefault(c => !string.IsNullOrEmpty(c) && File.Exists(c)) ?? "python3";
        }

        internal static bool RunPython(string script)
        {
            var info = new ProcessStartInfo(Python(), script)
            {
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            info.EnvironmentVariables["PATH"] = "/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin";
            try
            {
                using (var p = Process.Start(info))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        Debug.LogError($"[AudioPipeline] {script} failed ({p.ExitCode}):\n{error}\n{output}");
                        return false;
                    }
                    Debug.Log($"[AudioPipeline] {script}: {output.Trim()}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AudioPipeline] Could not run {script} with {info.FileName}: {e.Message}. Set EditorPrefs \"DCM.Python\" to a Python 3 with numpy.");
                return false;
            }
        }

        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");

        [System.Serializable]
        class ManifestItem
        {
            public string voice;
            public string text;
            public string file;
        }

        [System.Serializable]
        class ManifestList
        {
            public ManifestItem[] items;
        }
    }
}
