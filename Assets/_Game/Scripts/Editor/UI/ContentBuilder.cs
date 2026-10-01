using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DontCallMe.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DontCallMe.Editor.UI
{
    /// <summary>
    /// Writes the game's content: the days of the run, the catalog that lists them and a sample chat.
    /// <list type="bullet">
    /// <item>Day 1 (Data/Day1): "Nuri Bank's protection team" wants the savings moved. Always a scam.</item>
    /// <item>Day 2 (Data/Day2): the landlord's son asks for the rent on a new account. Scam or legit.</item>
    /// <item>Day 3 (Data/Day3): a courier's customs desk asks for the duty on a parcel. Scam or legit.</item>
    /// </list>
    /// Every day's phone, room and directory grow from one household timeline
    /// (ContentBuilder.Household.cs): a text sent on Monday reads "Mon 22:47" on Day 1 and is still there
    /// on Day 3. Each variant adds its own evidence on top. Rerun to reset edits; then run the voice
    /// pipeline for new or changed lines.
    /// </summary>
    public static partial class ContentBuilder
    {
        const string Day1Dir = "Assets/_Game/Data/Day1";
        const string Day2Dir = "Assets/_Game/Data/Day2";
        const string Day3Dir = "Assets/_Game/Data/Day3";
        const string SamplesDir = "Assets/_Game/Data/Samples";
        const string ResourcesDir = "Assets/_Game/Resources";
        public const string PhonePath = Day1Dir + "/Day1_Phone.asset";
        public const string DirectoryPath = Day1Dir + "/Day1_Directory.asset";
        public const string RoomPath = Day1Dir + "/Day1_Room.asset";
        public const string CallPath = Day1Dir + "/Day1_Call_ProtectedAccount.asset";
        public const string DayPath = Day1Dir + "/Day1.asset";
        public const string ChatPath = SamplesDir + "/Chat_E2_MomsBrokenPhone.asset";
        public const string CatalogPath = ResourcesDir + "/" + DayCatalog.ResourceName + ".asset";
        const string Sprites = "Assets/_Game/UI/Sprites/";
        const string PapersJson = "Tools/ArtGen/papers.json";
        const string PrintsDir = "Assets/_Game/Art/Textures/Papers";

        // The voices (macOS `say`) for everyone who speaks on the phone.
        const string VoiceJeon = "Daniel";
        const string VoiceBank = "Samantha";
        const string VoiceFss = "Karen";
        const string VoicePolice = "Tessa";
        const string VoiceMom = "Moira";
        const string VoiceDad = "Rishi";
        const string VoiceHyunwoo = "Reed (English (US))";
        const string VoiceCustoms = "Shelley (English (UK))";

        /// <summary>Bump when the built content changes, so open projects rebuild it (and its voices) by themselves.</summary>
        public const int Version = 4;

        [InitializeOnLoadMethod]
        static void RebuildWhenOutdated()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                var day = AssetDatabase.LoadAssetAtPath<DayData>(DayPath);
                bool catalog = AssetDatabase.LoadAssetAtPath<DayCatalog>(CatalogPath) != null;
                if (day == null || day.builtWith >= Version && catalog)
                    return;
                Debug.Log($"[ContentBuilder] Content is v{day.builtWith}, updating to v{Version} (content, newspaper prints and voices).");
                Build();
                PrintPapers();
                DontCallMe.Editor.Audio.AudioPipeline.RunVoices();
            };
        }

        [MenuItem("Tools/Don't Call Me/Content/Build Days")]
        public static void Build()
        {
            EnsureFolder(Day1Dir);
            EnsureFolder(Day2Dir);
            EnsureFolder(Day3Dir);
            EnsureFolder(SamplesDir);
            EnsureFolder(ResourcesDir);
            // Earlier builds wrote the same assets as "demo" content; move them so scenes keep their references.
            Move("Assets/_Game/Data/Demo/Demo_Phone.asset", PhonePath);
            Move("Assets/_Game/Data/Demo/Demo_Directory.asset", DirectoryPath);
            Move("Assets/_Game/Data/Demo/Demo_Room.asset", RoomPath);
            Move("Assets/_Game/Data/Demo/Demo_Call_M6_ProtectedAccount.asset", CallPath);
            Move("Assets/_Game/Data/Demo/Demo_Chat_E2_MomsBrokenPhone.asset", ChatPath);

            var catalog = ScriptableObject.CreateInstance<DayCatalog>();
            catalog.days = new List<DayData> { BuildDay1(), BuildDay2(), BuildDay3() };
            Store(BuildChat(), ChatPath);
            Store(catalog, CatalogPath);
            ExportPapers(catalog.days);
            AssignPrints(catalog.days);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ContentBuilder] {catalog.days.Count} days written (Data/Day1..Day{catalog.days.Count}) and listed in {CatalogPath}");
        }

        // ---------------------------------------------------------------- assets

        static void Move(string from, string to)
        {
            if (AssetDatabase.LoadMainAssetAtPath(from) == null || AssetDatabase.LoadMainAssetAtPath(to) != null)
                return;
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"[ContentBuilder] Could not move {from}: {error}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Writes the asset (over the old one, so references to it survive) and returns the saved asset.</summary>
        static T Store<T>(T data, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(data, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(data);
                return existing;
            }
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        /// <summary>A day's truth, with its call and evidence stored as assets named after the day and variant.</summary>
        static DayVariant StoreVariant(string id, string dir, string prefix, ConversationData call, PhoneContent phone, RoomContent room, WorldDirectory directory)
        {
            return new DayVariant
            {
                id = id,
                conversation = Store(call, $"{dir}/{prefix}_Call.asset"),
                phone = Store(phone, $"{dir}/{prefix}_Phone.asset"),
                room = Store(room, $"{dir}/{prefix}_Room.asset"),
                directory = Store(directory, $"{dir}/{prefix}_Directory.asset"),
            };
        }

        static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Sprites + name + ".png");

        // ---------------------------------------------------------------- the paper on the desk

        /// <summary>
        /// Prints the desk newspaper for every front page that lies on the desk the next day
        /// (Tools/ArtGen/tex_prints.py papers) and hands the prints to the papers.
        /// </summary>
        [MenuItem("Tools/Don't Call Me/Content/Print Desk Newspapers")]
        public static void PrintPapers()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DayCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError("[ContentBuilder] Build the days first.");
                return;
            }
            ExportPapers(catalog.days);
            if (!DontCallMe.Editor.Audio.AudioPipeline.RunPython("Tools/ArtGen/tex_prints.py papers"))
                return;
            AssetDatabase.Refresh();
            AssignPrints(catalog.days);
            AssetDatabase.SaveAssets();
        }

        static string PrintName(int day, DayVariant v, EndPaper p) => $"T_Paper_D{day}_{v.id}_{p.outcome}";

        static readonly string[] KoreanWeekdays = { "일", "월", "화", "수", "목", "금", "토" };

        /// <summary>The front pages that lie on the desk the next day, for tex_prints.py.</summary>
        static void ExportPapers(List<DayData> days)
        {
            var json = new StringBuilder("[\n");
            bool first = true;
            foreach (var d in days)
            {
                if (!days.Exists(x => x != null && x.day == d.day + 1))
                    continue;
                var date = new DateTime(2026, 10, 6 + d.day);
                string dateEn = date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
                string dateKo = $"{date.Year}년 {date.Month}월 {date.Day}일 {KoreanWeekdays[(int)date.DayOfWeek]}요일";
                string issue = $"제 {12408 + d.day:N0}호";
                foreach (var v in d.variants)
                    foreach (var p in v.papers)
                    {
                        json.Append(first ? "" : ",\n");
                        first = false;
                        json.Append($" {{\"name\": \"{PrintName(d.day, v, p)}\", \"headline\": \"{Json(p.headline)}\", \"subhead\": \"{Json(p.subhead)}\", " +
                                    $"\"dateEn\": \"{dateEn}\", \"dateKo\": \"{dateKo}\", \"issue\": \"{issue}\"}}");
                    }
            }
            json.Append("\n]\n");
            File.WriteAllText(PapersJson, json.ToString(), new UTF8Encoding(false));
        }

        static string Json(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");

        /// <summary>Gives each paper its print, if tex_prints.py has made it, with the room textures' import settings.</summary>
        static void AssignPrints(List<DayData> days)
        {
            foreach (var d in days)
            {
                bool changed = false;
                foreach (var v in d.variants)
                    foreach (var p in v.papers)
                    {
                        string path = $"{PrintsDir}/{PrintName(d.day, v, p)}.png";
                        if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureCompression != TextureImporterCompression.CompressedHQ)
                        {
                            importer.textureType = TextureImporterType.Default;
                            importer.sRGBTexture = true;
                            importer.alphaSource = TextureImporterAlphaSource.None;
                            importer.wrapMode = TextureWrapMode.Clamp;
                            importer.mipmapEnabled = true;
                            importer.filterMode = FilterMode.Trilinear;
                            importer.anisoLevel = 4;
                            importer.maxTextureSize = 2048;
                            importer.textureCompression = TextureImporterCompression.CompressedHQ;
                            importer.SaveAndReimport();
                        }
                        var print = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (print != null && p.print != print)
                        {
                            p.print = print;
                            changed = true;
                        }
                    }
                if (changed)
                    EditorUtility.SetDirty(d);
            }
        }

        // ---------------------------------------------------------------- the calendar

        /// <summary>The day being built and the time it starts; the timeline shows what happened before then.</summary>
        static DateTime today = new DateTime(2026, 10, 6);
        static string startTime = "16:20";

        static void SetDay(int day, string start)
        {
            today = new DateTime(2026, 10, 5 + day);
            startTime = start;
        }

        /// <summary>The day of the run being built (6 October is Day 1).</summary>
        static int DayNumber => today.Day - 5;

        /// <summary>Has it happened by the time the day starts?</summary>
        static bool Seen(int month, int dom, string time = null)
        {
            var at = new DateTime(2026, month, dom);
            if (at != today)
                return at < today;
            return string.CompareOrdinal(time ?? "00:00", startTime) < 0;
        }

        /// <summary>How the phone labels a moment: "Today 09:12", "Yesterday 21:40", "Mon 22:47", "Fri 25 Sep".</summary>
        static string At(int month, int dom, string time = null)
        {
            var at = new DateTime(2026, month, dom);
            int ago = (today - at).Days;
            string t = string.IsNullOrEmpty(time) ? "" : " " + time;
            string weekday = at.ToString("ddd", CultureInfo.InvariantCulture);
            if (ago == 0)
                return "Today" + t;
            if (ago == 1)
                return "Yesterday" + t;
            if (ago < 7)
                return weekday + t;
            return $"{weekday} {at.Day} {at.ToString("MMM", CultureInfo.InvariantCulture)}" + t;
        }

        static string Oct(int dom, string time = null) => At(10, dom, time);

        // ---------------------------------------------------------------- small builders

        static Fact F(FactKind k, string v, string label = null) => new Fact(k, v, label);

        static DocField Field(string label, string value, FactKind? fact = null) =>
            new DocField { label = label, value = value, isFact = fact.HasValue, factKind = fact ?? FactKind.Text };

        static ConvLine Say(string text, string spoken = null, float pauseAfter = 0.3f) => ConvLine.Caller(text, spoken, pauseAfter);

        static ConvLine Caller(string text, params Fact[] facts) => new ConvLine { speaker = Speaker.Caller, text = text, facts = new List<Fact>(facts) };

        static ConvLine Caller(string text, string spoken, params Fact[] facts) =>
            new ConvLine { speaker = Speaker.Caller, text = text, spoken = spoken, facts = new List<Fact>(facts) };

        static CallbackScript Callback(string number, string answeredBy, string portrait, string voice, params ConvLine[] lines) =>
            new CallbackScript { number = number, answeredBy = answeredBy, portrait = portrait, voice = voice, lines = new List<ConvLine>(lines) };

        static ConvNode Node(string id, string next, params ConvLine[] lines) => new ConvNode { id = id, next = next, lines = new List<ConvLine>(lines) };

        static ConvNode Decide(string id, ConvDecision d, params ConvLine[] lines) =>
            new ConvNode { id = id, hasDecision = true, decision = d, lines = new List<ConvLine>(lines) };

        static ConvNode Hold(string id, params ConvLine[] lines) => new ConvNode { id = id, holds = true, lines = new List<ConvLine>(lines) };

        static ConvDecision D(DecisionKind kind, string prompt, float seconds, string aLabel, string aNext, string bLabel, string bNext, params PressureLine[] pressure) =>
            new ConvDecision
            {
                kind = kind, prompt = prompt, patienceSeconds = seconds,
                a = new ConvOption { label = aLabel, playerLine = aLabel, next = aNext },
                b = new ConvOption { label = bLabel, playerLine = bLabel, next = bNext },
                pressure = new List<PressureLine>(pressure),
            };

        static PressureLine P(float at, string text) => new PressureLine { atPatience = at, text = text };

        static ConvQuestion Q(string label, string playerLine, string endingId, params ConvLine[] answer) =>
            new ConvQuestion { label = label, playerLine = playerLine, endingId = endingId, answer = new List<ConvLine>(answer) };

        static PressureBeat Beat(string at, ConvLine line) => new PressureBeat { at = at, line = line };

        static ConvLine WithSms(ConvLine line, string from, string text, string link = null)
        {
            line.deliver.Add(new Delivery { kind = DeliveryKind.Sms, from = from, text = text, link = link });
            return line;
        }

        static ClueDef Clue(string id, string text, string where, params ClueWhen[] when) =>
            new ClueDef { id = id, text = text, where = where, when = new List<ClueWhen>(when) };

        static EndPaper Paper(Outcome outcome, string headline, string subhead, string body, string note) =>
            new EndPaper { outcome = outcome, headline = headline, subhead = subhead, body = body, verdictNote = note };

        // Echoes: what an earlier day left behind.
        static DayEcho Echo(int afterDay, Truth truth, OutcomeMask outcomes, EchoKind kind) =>
            new DayEcho { afterDay = afterDay, truth = truth, outcomes = outcomes, kind = kind };

        static DayEcho EchoSms(int afterDay, Truth truth, OutcomeMask outcomes, string from, string when, string text, bool notify = false)
        {
            var e = Echo(afterDay, truth, outcomes, EchoKind.Sms);
            e.from = from;
            e.when = when;
            e.text = text;
            e.notify = notify;
            return e;
        }

        static DayEcho EchoChat(int afterDay, Truth truth, OutcomeMask outcomes, string chat, string sender, string avatar, string when, string text, bool notify = false)
        {
            var e = Echo(afterDay, truth, outcomes, EchoKind.Chat);
            e.from = chat;
            e.sender = sender;
            e.avatar = avatar;
            e.when = when;
            e.text = text;
            e.notify = notify;
            return e;
        }

        static DayEcho EchoMine(int afterDay, Truth truth, OutcomeMask outcomes, string chat, string when, string text)
        {
            var e = Echo(afterDay, truth, outcomes, EchoKind.Chat);
            e.from = chat;
            e.when = when;
            e.text = text;
            e.outgoing = true;
            return e;
        }

        static DayEcho EchoCard(int afterDay, Truth truth, OutcomeMask outcomes, string text)
        {
            var e = Echo(afterDay, truth, outcomes, EchoKind.DayCard);
            e.text = text;
            return e;
        }

        static DayEcho EchoNote(int afterDay, Truth truth, OutcomeMask outcomes, string title, string text, string sticky, Vector2 position, float rotation)
        {
            var e = Echo(afterDay, truth, outcomes, EchoKind.BoardNote);
            e.title = title;
            e.text = text;
            e.image = Tex(sticky + "_blank");
            e.position = position;
            e.rotation = rotation;
            return e;
        }

        const OutcomeMask Any = OutcomeMask.Any;
        const OutcomeMask Sent = OutcomeMask.GoAlong;
        const OutcomeMask HungUp = OutcomeMask.Refuse;
        const OutcomeMask TooLate = OutcomeMask.Timeout;
        const OutcomeMask Kept = OutcomeMask.Refuse | OutcomeMask.Timeout;
    }
}
