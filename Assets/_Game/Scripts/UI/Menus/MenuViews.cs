using System;
using DontCallMe.Audio;
using DontCallMe.Data;
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
