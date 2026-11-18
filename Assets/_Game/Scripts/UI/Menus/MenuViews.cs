using System;
using DontCallMe.Audio;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// The language, music, sound effect and voice volume and the look sensitivity, as buttons and
    /// chunky sliders on paper. Changes apply at once and are saved (<see cref="GameSettings"/>,
    /// <see cref="Loc"/>); the owner decides what a new language rebuilds. Used by the title screen and the pause menu.
    /// </summary>
    public class SettingsView
    {
        public VisualElement Root { get; }
        float lastSfxPreview;

        public SettingsView(Action onBack, Action<Lang> onLanguage = null)
        {
            Root = UIKit.Div("menu-card", "paper", "settings");
            Root.Add(UIKit.Text(Loc.T("SETTINGS"), "menu-card__title"));
            Root.Add(UIKit.Div("rule"));
            Root.Add(LanguageRow(onLanguage));
            Root.Add(Row("ic_music", Loc.T("Music"), GameSettings.Music, 0f, 1f, v => GameSettings.Music = v, Percent));
            Root.Add(Row("ic_sfx", Loc.T("Sound effects"), GameSettings.Sfx, 0f, 1f, v =>
            {
                GameSettings.Sfx = v;
                if (Time.unscaledTime - lastSfxPreview > 0.15f)
                {
                    lastSfxPreview = Time.unscaledTime;
                    Sfx.Play(Sfx.Pop, 0.7f);
                }
            }, Percent));
            Root.Add(Row("ic_voice", Loc.T("Voices"), GameSettings.Voice, 0f, 1f, v => GameSettings.Voice = v, Percent));
            Root.Add(Row("ic_mouse", Loc.T("Look sensitivity"), GameSettings.LookSensitivity, GameSettings.MinLook, GameSettings.MaxLook,
                         v => GameSettings.LookSensitivity = v, v => $"{v:0.0}×"));
            Root.Add(UIKit.Text(Loc.T("Drag with the mouse to look around. Higher turns the view faster."), "settings__note"));
            var buttons = UIKit.Div("modal__buttons");
            buttons.Add(UIKit.Btn(Loc.T("Back"), onBack, "btn--blue"));
            Root.Add(buttons);
        }

        /// <summary>English or 한국어 (each written in its own language), for the text, the voices and the papers.</summary>
        static VisualElement LanguageRow(Action<Lang> onLanguage)
        {
            var row = UIKit.Div("settings__row");
            row.Add(UIKit.Icon("ic_globe", "settings__icon"));
            row.Add(UIKit.Text(Loc.T("Language"), "settings__label"));
            var choices = UIKit.Div("settings__langs");
            foreach (Lang lang in new[] { Lang.En, Lang.Ko })
            {
                var l = lang;
                var b = UIKit.Btn(Loc.Name(l), () =>
                {
                    if (l == Loc.Current)
                        return;
                    Sfx.Play(Sfx.Click);
                    if (onLanguage != null)
                        onLanguage(l);
                    else
                        Loc.Current = l;
                }, "settings__lang", l == Loc.Current ? "btn--amber" : null);
                choices.Add(b);
            }
            row.Add(choices);
            return row;
        }

        static string Percent(float v) => $"{Mathf.RoundToInt(v * 100)}%";

        static VisualElement Row(string icon, string label, float value, float low, float high, Action<float> set, Func<float, string> format)
        {
            var row = UIKit.Div("settings__row");
            row.Add(UIKit.Icon(icon, "settings__icon"));
            row.Add(UIKit.Text(label, "settings__label"));
            var slider = new Slider(low, high) { value = value };
            slider.AddToClassList("dcm-slider");
            slider.focusable = false;
            var readout = UIKit.Text(format(value), "settings__value");
            slider.RegisterValueChangedCallback(e =>
            {
                readout.text = format(e.newValue);
                set(e.newValue);
            });
            row.Add(slider);
            row.Add(readout);
            return row;
        }
    }

    /// <summary>
    /// The title screen's Case Files, in three pages:
    /// <list type="bullet">
    /// <item>This week: every day as a line. A finished day shows who really called, what the
    /// player did and the clues found. Days are started from the title screen's day select
    /// (<see cref="DaySelectView"/>). New week clears this week's lines.</item>
    /// <item>Cases: every case with its truths, the ones not seen yet face down (<see cref="CaseBook"/>).</item>
    /// <item>Trick cards: the cards won by catching false claims (<see cref="TrickCards"/>).</item>
    /// </list>
    /// The cases and the cards are kept across weeks.
    /// </summary>
    public class CaseFilesView
    {
        public enum Page { Week, Cases, Tricks }

        public VisualElement Root { get; }
        public Page Current { get; private set; }

        readonly DayCatalog catalog;
        readonly VisualElement tabs;
        readonly ScrollView body;

        public CaseFilesView(Action onBack, Action onNewWeek)
        {
            Root = UIKit.Div("menu-card", "paper", "case-files");
            var head = UIKit.Div("pause__head");
            head.Add(UIKit.Icon("ic_case", "pause__icon"));
            head.Add(UIKit.Text(Loc.T("CASE FILES"), "menu-card__title"));
            Root.Add(head);

            catalog = DayCatalog.Load();
            var (seen, truths) = CaseBook.Count(catalog);
            var (won, cards) = TrickCards.Count(catalog);
            tabs = UIKit.Div("files-tabs");
            tabs.Add(Tab(Page.Week, Loc.T("This week")));
            tabs.Add(Tab(Page.Cases, Loc.F("Cases  {0}/{1}", seen, truths)));
            tabs.Add(Tab(Page.Tricks, Loc.F("Trick cards  {0}/{1}", won, cards)));
            Root.Add(tabs);

            body = new ScrollView(ScrollViewMode.Vertical);
            body.AddToClassList("files-body");
            body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(body);

            var buttons = UIKit.Div("modal__buttons");
            // For playtests: hands over what was logged (PlaytestLog).
            var log = UIKit.Btn(Loc.T("Save playtest log"), PlaytestLog.Export, "files-log");
            log.tooltip = Loc.T("What you did each day, for the people testing the game");
            buttons.Add(log);
            buttons.Add(UIKit.Div("grow"));
            buttons.Add(UIKit.Btn(Loc.T("New week"), onNewWeek));
            buttons.Add(UIKit.Btn(Loc.T("Back"), onBack, "btn--blue"));
            Root.Add(buttons);
            Show(Page.Week);
        }

        Button Tab(Page page, string text)
        {
            var b = UIKit.Btn(text, () =>
            {
                Sfx.Play(Sfx.Paper, 0.5f);
                Show(page);
            }, "files-tab");
            b.userData = page;
            return b;
        }

        public void Show(Page page)
        {
            Current = page;
            foreach (var t in tabs.Children())
                t.EnableInClassList("files-tab--on", t.userData is Page p && p == page);
            body.Clear();
            body.scrollOffset = Vector2.zero;
            if (page == Page.Cases)
                body.Add(CaseBook.Page(catalog));
            else if (page == Page.Tricks)
                body.Add(TrickCards.Page(catalog));
            else
                body.Add(Week());
        }

        VisualElement Week()
        {
            var table = UIKit.Div("week-table");
            if (catalog != null)
                foreach (var day in catalog.days)
                    if (day != null)
                        table.Add(Row(day, GameRun.Get(day.day)));
            return table;
        }

        static VisualElement Row(DayData day, DayRecord record)
        {
            var variant = record != null ? day.Variant(record.variant) : null;
            var conv = variant?.conversation;
            var row = UIKit.Div("week-row", "week-row--in", record == null ? "week-row--closed" : null);
            row.Add(UIKit.Portrait(conv != null ? conv.caller.portrait : null, "week-row__portrait"));
            var name = UIKit.Div("week-row__name");
            name.Add(UIKit.Text(Loc.F("DAY {0}  ·  {1}", day.day, day.dateLabel), "week-row__day"));
            name.Add(UIKit.Text(conv != null ? conv.caseInfo.caseTitle : Loc.T("Not played this week"), "week-row__title"));
            row.Add(name);
            if (record != null)
            {
                row.Add(UIKit.Text(Loc.T(record.scam ? "SCAM" : "REAL"), "week-row__truth", record.scam ? "week-row__truth--scam" : "week-row__truth--real"));
                row.Add(UIKit.Text(DayRecordText.Did(record, conv), "week-row__did"));
                row.Add(UIKit.Text($"{record.clues.Count}/{record.cluesTotal}", "week-row__clues"));
                var mark = UIKit.Div("week-row__mark");
                mark.Add(UIKit.Icon(record.Right ? "ic_check" : "ic_cross", "week-row__icon"));
                row.Add(mark);
            }
            else
            {
                row.Add(UIKit.Div("grow"));
            }
            return row;
        }
    }

    /// <summary>How a finished day is put into words on the summaries.</summary>
    public static class DayRecordText
    {
        /// <summary>What the player did, in a few words: "Sent ₩450,000", "Hung up", "Ran out of time".</summary>
        public static string Did(DayRecord r, ConversationData conv) => r.outcome switch
        {
            Outcome.GoAlong => r.moneyDelta < 0 ? Loc.F("Sent {0}", FactText.Won(-r.moneyDelta)) : conv?.verdict?.goAlong ?? Loc.T("Went along"),
            Outcome.Refuse => Loc.T("Hung up"),
            Outcome.Timeout => Loc.T("Ran out of time"),
            _ => Loc.T("Checked first"),
        };
    }

    /// <summary>The pause menu: Resume, Settings, Main menu, over the frozen room.</summary>
    public class PauseMenuView
    {
        public VisualElement Root { get; }
        readonly VisualElement main;
        readonly Action<Lang> onLanguage;
        SettingsView settings;

        public bool InSettings => settings != null;

        public PauseMenuView(string dayLabel, Action onResume, Action onMainMenu, Action<Lang> onLanguage = null)
        {
            this.onLanguage = onLanguage;
            Root = UIKit.Div("menu-shade");
            main = UIKit.Div("menu-card", "paper", "pause");
            var head = UIKit.Div("pause__head");
            head.Add(UIKit.Icon("ic_pause", "pause__icon"));
            head.Add(UIKit.Text(Loc.T("PAUSED"), "menu-card__title"));
            main.Add(head);
            main.Add(UIKit.Text(dayLabel, "pause__day"));
            main.Add(UIKit.Div("rule"));
            main.Add(MenuButton("ic_play", Loc.T("Resume"), onResume, "btn--green"));
            main.Add(MenuButton("ic_gear", Loc.T("Settings"), OpenSettings, null));
            main.Add(MenuButton("ic_exit", Loc.T("Main menu"), onMainMenu, "btn--red"));
            main.Add(UIKit.Text(Loc.T("Esc  resume"), "pause__hint"));
            Root.Add(main);
            main.schedule.Execute(() => main.AddToClassList("menu-card--in")).ExecuteLater(20);
        }

        /// <summary>A wide menu button with an icon; shared with the title screen.</summary>
        public static Button MenuButton(string icon, string text, Action onClick, string cls)
        {
            var b = UIKit.Btn("", onClick, "menu-btn", cls);
            b.Add(UIKit.Icon(icon, "menu-btn__icon"));
            b.Add(UIKit.Text(text, "menu-btn__text"));
            return b;
        }

        void OpenSettings()
        {
            main.Hide(true);
            settings = new SettingsView(() => CloseSettings(), onLanguage);
            settings.Root.AddToClassList("menu-card--in");
            Root.Add(settings.Root);
        }

        /// <summary>Back out of the settings page. False when the settings were not open.</summary>
        public bool CloseSettings()
        {
            if (settings == null)
                return false;
            settings.Root.RemoveFromHierarchy();
            settings = null;
            main.Hide(false);
            return true;
        }
    }
}
