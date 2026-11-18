using System;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Makes an item in a panel or app circleable for the case notebook when today's case has a
    /// mark for it: it lights up under the pointer, a click draws an ink ring round it and files a
    /// copy under its claim, and a second click lets go of it. Items without a mark stay plain text.
    /// </summary>
    public static class Markable
    {
        /// <param name="text">The item as the notebook quotes it.</param>
        /// <param name="where">Where it was, e.g. "Desk drawer · Lease contract".</param>
        /// <param name="handle">
        /// A small button inside the item that circles it, when a click on the item itself already
        /// does something else (a caller's line on a Write day pins it). Null: the item is the button.
        /// </param>
        /// <returns>True when the item can be circled today.</returns>
        public static bool Attach(VisualElement item, UIManager ui, MarkPlace place, string key, string text, string where, VisualElement handle = null)
        {
            var notebook = ui != null ? ui.Notebook : null;
            var mark = notebook?.Find(place, key);
            if (mark == null)
                return false;
            var button = handle ?? item;
            item.AddToClassList("markable");
            item.EnableInClassList("markable--glow", notebook.help == NotebookHelp.Guided);
            button.tooltip = Loc.T("Click to circle it for your notebook");
            var ring = UIKit.Div("ink-ring");
            ring.pickingMode = PickingMode.Ignore;
            item.Add(ring);
            if (handle != null)
            {
                handle.AddToClassList("mark-handle");
                item.Add(handle);
            }

            void Refresh()
            {
                bool on = notebook.IsMarked(mark.id);
                item.EnableInClassList("markable--on", on);
                button.tooltip = Loc.T(on ? "Circled. Click again to let go of it" : "Click to circle it for your notebook");
                if (handle is Label label)
                    label.text = Loc.T(on ? "Circled" : "Circle");
            }

            Action changed = Refresh;
            item.RegisterCallback<AttachToPanelEvent>(_ => notebook.Changed += changed);
            item.RegisterCallback<DetachFromPanelEvent>(_ => notebook.Changed -= changed);
            if (item.panel != null)
                notebook.Changed += changed;
            // A long line gets a long flat ring, a block a rounder one.
            item.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float aspect = e.newRect.height > 1f ? e.newRect.width / e.newRect.height : 4f;
                ring.EnableInClassList("ink-ring--wide", aspect >= 5f);
                ring.EnableInClassList("ink-ring--mid", aspect >= 2f && aspect < 5f);
                ring.EnableInClassList("ink-ring--round", aspect < 2f);
            });
            button.RegisterCallback<ClickEvent>(e =>
            {
                bool was = notebook.IsMarked(mark.id);
                notebook.Toggle(mark, text, where);
                Sfx.Play(was ? Sfx.Paper : Sfx.Pen, was ? 0.4f : 0.9f);
                e.StopPropagation();
            });
            Refresh();
            return true;
        }
    }
}
