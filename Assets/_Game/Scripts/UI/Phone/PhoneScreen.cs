using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// One screen on the phone: a header with back button and title, and a scrolling body.
    /// Apps build their screens from the helpers here (sections, list rows, key/value rows, paste fields).
    /// </summary>
    public class PhoneScreen
    {
        public PhoneController Phone { get; }
        public VisualElement Root { get; }
        public ScrollView Body { get; }

        /// <summary>Light screens get dark status-bar text.</summary>
        public virtual bool LightStatus => true;

        public PhoneScreen(PhoneController phone, string title, string accent = null, bool scroll = true, bool header = true)
        {
            Phone = phone;
            Root = UIKit.Div("screen");
            if (header)
            {
                var bar = UIKit.Div("screen__header");
                var back = UIKit.Div("screen__back");
                back.tooltip = "Back (Esc)";
                back.RegisterCallback<ClickEvent>(e =>
                {
                    Sfx.Play(Sfx.Click, 0.5f);
                    phone.Back();
                    e.StopPropagation();
                });
                bar.Add(back);
                if (!string.IsNullOrEmpty(accent))
                {
                    var a = UIKit.Div("screen__accent");
                    a.style.backgroundColor = Hex(accent);
                    bar.Add(a);
                }
                bar.Add(UIKit.Text(title, "screen__title"));
                Root.Add(bar);
                Header = bar;
            }
            if (scroll)
            {
                Body = new ScrollView(ScrollViewMode.Vertical);
                Body.AddToClassList("screen__body");
                Body.contentContainer.AddToClassList("screen__content");
                Body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                Root.Add(Body);
            }
        }

        protected VisualElement Header { get; }

        public VisualElement Content => Body != null ? Body.contentContainer : Root;

        public virtual void OnShow() { }

        public virtual void OnHide() { }

        // ---------------------------------------------------------------- helpers

        protected Label Section(string title, VisualElement parent = null)
        {
            var l = UIKit.Text(title.ToUpperInvariant(), "section");
            (parent ?? Content).Add(l);
            return l;
        }

        /// <summary>List row: avatar or icon, title, subtitle and a meta text on the right.</summary>
        protected VisualElement Row(string image, bool avatar, string title, string subtitle, string meta, Action onClick,
                                    string metaClass = null, VisualElement parent = null, bool unread = false)
        {
            var row = UIKit.Div("list-row");
            if (!string.IsNullOrEmpty(image))
            {
                var img = UIKit.Div(avatar ? "list-row__avatar" : "list-row__icon");
                UIKit.SetImage(img, UISkin.Tex(image));
                img.pickingMode = PickingMode.Ignore;
                row.Add(img);
            }
            var text = UIKit.Div("list-row__text");
            text.pickingMode = PickingMode.Ignore;
            text.Add(UIKit.Text(title, "list-row__title"));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = UIKit.Text(subtitle, "list-row__subtitle");
                text.Add(sub);
            }
            row.Add(text);
            if (!string.IsNullOrEmpty(meta))
            {
                var m = UIKit.Text(meta, "list-row__meta", metaClass);
                m.pickingMode = PickingMode.Ignore;
                row.Add(m);
            }
            if (unread)
                row.Add(UIKit.Div("dot-unread"));
            if (onClick != null)
                row.RegisterCallback<ClickEvent>(e =>
                {
                    Sfx.Play(Sfx.Click, 0.5f);
                    onClick();
                    e.StopPropagation();
                });
            (parent ?? Content).Add(row);
            return row;
        }

        /// <summary>Text field with Paste and an action button; Enter also submits.</summary>
        protected TextField PasteField(string placeholder, string buttonText, Action<string> onSubmit, VisualElement parent = null)
        {
            var row = UIKit.Div("field");
            var field = new TextField();
            field.textEdition.placeholder = placeholder;
            row.Add(field);
            row.Add(UIKit.Btn("Paste", () =>
            {
                if (!string.IsNullOrEmpty(Clipboard.Value))
                    field.value = Clipboard.Value;
            }));
            if (!string.IsNullOrEmpty(buttonText))
                row.Add(UIKit.Btn(buttonText, () => onSubmit?.Invoke(field.value), "btn--blue"));
            field.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    onSubmit?.Invoke(field.value);
            }, TrickleDown.TrickleDown);
            (parent ?? Content).Add(row);
            return field;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }
    }
}
