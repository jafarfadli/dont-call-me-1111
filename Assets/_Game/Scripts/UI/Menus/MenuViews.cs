using System;
using DontCallMe.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Music, sound effect and voice volume and the look sensitivity, as chunky sliders on paper.
    /// Changes apply at once and are saved (<see cref="GameSettings"/>). Used by the title screen and the pause menu.
    /// </summary>
    public class SettingsView
    {
        public VisualElement Root { get; }
        float lastSfxPreview;

        public SettingsView(Action onBack)
        {
            Root = UIKit.Div("menu-card", "paper", "settings");
            Root.Add(UIKit.Text("SETTINGS", "menu-card__title"));
            Root.Add(UIKit.Div("rule"));
            Root.Add(Row("ic_music", "Music", GameSettings.Music, 0f, 1f, v => GameSettings.Music = v, Percent));
            Root.Add(Row("ic_sfx", "Sound effects", GameSettings.Sfx, 0f, 1f, v =>
            {
                GameSettings.Sfx = v;
                if (Time.unscaledTime - lastSfxPreview > 0.15f)
                {
                    lastSfxPreview = Time.unscaledTime;
                    Sfx.Play(Sfx.Pop, 0.7f);
                }
            }, Percent));
            Root.Add(Row("ic_voice", "Voices", GameSettings.Voice, 0f, 1f, v => GameSettings.Voice = v, Percent));
            Root.Add(Row("ic_mouse", "Look sensitivity", GameSettings.LookSensitivity, GameSettings.MinLook, GameSettings.MaxLook,
                         v => GameSettings.LookSensitivity = v, v => $"{v:0.0}×"));
            Root.Add(UIKit.Text("Drag with the mouse to look around. Higher turns the view faster.", "settings__note"));
            var buttons = UIKit.Div("modal__buttons");
            buttons.Add(UIKit.Btn("Back", onBack, "btn--blue"));
            Root.Add(buttons);
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
        SettingsView settings;

        public bool InSettings => settings != null;

        public PauseMenuView(string dayLabel, Action onResume, Action onMainMenu)
        {
            Root = UIKit.Div("menu-shade");
            main = UIKit.Div("menu-card", "paper", "pause");
            var head = UIKit.Div("pause__head");
            head.Add(UIKit.Icon("ic_pause", "pause__icon"));
            head.Add(UIKit.Text("PAUSED", "menu-card__title"));
            main.Add(head);
            main.Add(UIKit.Text(dayLabel, "pause__day"));
            main.Add(UIKit.Div("rule"));
            main.Add(MenuButton("ic_play", "Resume", onResume, "btn--green"));
            main.Add(MenuButton("ic_gear", "Settings", OpenSettings, null));
            main.Add(MenuButton("ic_exit", "Main menu", onMainMenu, "btn--red"));
            main.Add(UIKit.Text("Esc  resume", "pause__hint"));
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
            settings = new SettingsView(() => CloseSettings());
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
