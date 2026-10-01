using System;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Small builders for UI Toolkit elements, so panels and apps read like layout templates.
    /// All styling lives in the USS theme (Assets/_Game/UI/Uss); these only set classes and content.
    /// </summary>
    public static class UIKit
    {
        public static VisualElement Div(params string[] classes) => new VisualElement().With(classes);

        public static Label Text(string text, params string[] classes)
        {
            var label = new Label(text).With(classes);
            label.enableRichText = true;
            // The default runtime theme keeps labels on one line; ours wrap unless a class says otherwise.
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        public static Button Btn(string text, Action onClick, params string[] classes)
        {
            var b = new Button(onClick) { text = text, focusable = false };
            b.style.whiteSpace = WhiteSpace.Normal;
            b.AddToClassList("btn");
            b.With(classes);
            b.RegisterCallback<ClickEvent>(_ => Sfx.Play(Sfx.Click, 0.5f));
            return b;
        }

        public static Button IconBtn(string icon, Action onClick, params string[] classes)
        {
            var b = Btn("", onClick, "btn-icon");
            b.With(classes);
            b.Add(Icon(icon));
            return b;
        }

        public static VisualElement Icon(string name, params string[] classes)
        {
            var e = Div("icon").With(classes);
            e.pickingMode = PickingMode.Ignore;
            SetImage(e, UISkin.Tex(name));
            return e;
        }

        public static VisualElement Image(Texture2D tex, params string[] classes)
        {
            var e = Div(classes);
            SetImage(e, tex);
            return e;
        }

        public static VisualElement Portrait(string id, params string[] classes)
        {
            var e = Div("portrait").With(classes);
            SetImage(e, UISkin.Tex(string.IsNullOrEmpty(id) ? "pt_unknown" : id) ?? UISkin.Tex("pt_unknown"));
            return e;
        }

        public static void SetImage(VisualElement e, Texture2D tex)
        {
            e.style.backgroundImage = tex != null ? new StyleBackground(tex) : new StyleBackground(StyleKeyword.None);
        }

        public static FactChip Chip(Fact fact) => new FactChip(fact);

        public static FactChip Chip(string value, FactKind kind, string label = null) => new FactChip(new Fact(kind, value, label));

        /// <summary>A key/value row; the value becomes a copy chip when it is a fact.</summary>
        public static VisualElement Kv(string key, string value, FactKind? factKind = null)
        {
            var row = Div("kv");
            row.Add(Text(key, "kv__key"));
            if (factKind.HasValue)
                row.Add(Chip(value, factKind.Value));
            else
                row.Add(Text(value, "kv__value"));
            return row;
        }

        public static T With<T>(this T element, params string[] classes) where T : VisualElement
        {
            if (classes == null)
                return element;
            foreach (string c in classes)
                if (!string.IsNullOrEmpty(c))
                    element.AddToClassList(c);
            return element;
        }

        public static T Hide<T>(this T element, bool hidden) where T : VisualElement
        {
            element.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
            return element;
        }

        public static void IgnorePicking(this VisualElement element) => element.pickingMode = PickingMode.Ignore;

        /// <summary>Scroll a ScrollView to its end after the next layout pass.</summary>
        public static void ScrollToEnd(ScrollView view)
        {
            view.schedule.Execute(() =>
            {
                view.scrollOffset = new Vector2(0, Mathf.Max(0, view.contentContainer.layout.height - view.contentViewport.layout.height));
            }).ExecuteLater(30);
        }
    }
}
