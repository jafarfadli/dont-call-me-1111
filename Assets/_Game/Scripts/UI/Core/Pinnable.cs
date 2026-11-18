using System;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// On a day the player writes the notebook (<see cref="CaseNotebook.WritesOwn"/>), makes a
    /// caller's line in the transcript pinnable: a click writes it in the notebook, a second click
    /// takes it out. Every caller line can be pinned, whether it makes a claim or not, so pinning
    /// gives nothing away.
    /// </summary>
    public static class Pinnable
    {
        /// <param name="line">The line as the caller said it.</param>
        /// <param name="claim">The claim the line makes (ConvLine.claim), or empty.</param>
        public static void Attach(VisualElement bubble, CaseNotebook notebook, string line, string claim)
        {
            if (bubble == null || notebook == null || !notebook.WritesOwn || string.IsNullOrEmpty(line))
                return;
            bubble.AddToClassList("pinnable");
            var tag = UIKit.Text(Loc.T("In your notebook"), "pinnable__tag");
            tag.pickingMode = PickingMode.Ignore;
            bubble.Add(tag);

            void Refresh()
            {
                bool on = notebook.IsPinned(line);
                bubble.EnableInClassList("pinnable--on", on);
                bubble.tooltip = Loc.T(on ? "In your notebook. Click again to take it out" : "Click to write it in your notebook");
            }

            Action changed = Refresh;
            bubble.RegisterCallback<AttachToPanelEvent>(_ => notebook.Changed += changed);
            bubble.RegisterCallback<DetachFromPanelEvent>(_ => notebook.Changed -= changed);
            if (bubble.panel != null)
                notebook.Changed += changed;
            bubble.RegisterCallback<ClickEvent>(e =>
            {
                bool was = notebook.IsPinned(line);
                if (notebook.TogglePin(line, claim))
                    Sfx.Play(was ? Sfx.Paper : Sfx.Pen, was ? 0.4f : 0.9f);
                else
                    Sfx.Play(Sfx.Error, 0.5f);
                e.StopPropagation();
            });
            Refresh();
        }
    }
}
