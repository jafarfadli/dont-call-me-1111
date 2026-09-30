using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    public enum PanelId
    {
        None,
        Newspaper,
        Drawer,
        Board,
        Wallet,
        Notebook,
    }

    /// <summary>A full-screen room panel with a close button. Esc or right-click closes it.</summary>
    public abstract class RoomPanel
    {
        public readonly UIManager UI;
        public VisualElement Root { get; }
        public abstract PanelId Id { get; }

        protected RoomPanel(UIManager ui)
        {
            UI = ui;
            Root = UIKit.Div("room-panel");
            var box = Build();
            var close = UIKit.IconBtn("ic_close", () => ui.ClosePanel(), "close-btn");
            close.tooltip = "Close (Esc)";
            box.Add(close);
            Root.Add(box);
        }

        protected abstract VisualElement Build();

        public virtual void OnOpen() { }

        /// <summary>Handle Esc inside the panel (e.g. close a zoomed item). False lets the panel close.</summary>
        public virtual bool Back() => false;

        protected RoomContent Content => UI.Room;
    }

    // ==================================================================== newspaper

    public class NewspaperPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Newspaper;

        public NewspaperPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var n = UI.Room.newspaper;
            var sheet = UIKit.Div("news");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("news__scroll");
            var c = scroll.contentContainer;

            var top = UIKit.Div("news__top");
            top.Add(UIKit.Text("서울데일리", "news__kor"));
            top.Add(UIKit.Text(n.masthead, "news__masthead"));
            top.Add(UIKit.Text(n.issue, "news__issue"));
            c.Add(top);
            var date = UIKit.Div("news__dateline");
            date.Add(UIKit.Text(n.dateLine));
            date.Add(UIKit.Text("MANGWON · MAPO · SEOUL"));
            date.Add(UIKit.Text(n.price));
            c.Add(date);
            c.Add(UIKit.Text(n.headline, "news__headline"));
            c.Add(UIKit.Text(n.subhead, "news__subhead"));

            var cols = UIKit.Div("news__cols");
            var main = UIKit.Div("news__main");
            if (n.photo != null)
                main.Add(UIKit.Image(n.photo, "news__photo"));
            main.Add(UIKit.Text(n.caption, "news__caption"));
            foreach (string p in n.body)
                main.Add(UIKit.Text(p, "news__body"));
            cols.Add(main);

            var side = UIKit.Div("news__side");
            if (!string.IsNullOrEmpty(n.warningTitle))
            {
                var box = UIKit.Div("news__box");
                box.Add(UIKit.Text(n.warningTitle, "news__box-title"));
                box.Add(UIKit.Text(n.warningText, "news__box-text"));
                side.Add(box);
            }
            side.Add(UIKit.Text("LOCAL NEWS", "news__box-title"));
            foreach (var item in n.local)
            {
                side.Add(UIKit.Text(item.title, "news__item-title"));
                side.Add(UIKit.Text(item.text, "news__item-text"));
            }
            cols.Add(side);
            c.Add(cols);

            if (n.ads.Count > 0)
            {
                var ads = UIKit.Div("news__ads");
                foreach (var ad in n.ads)
                {
                    var box = UIKit.Div("news__ad");
                    box.Add(UIKit.Text(ad.title, "news__ad-title"));
                    box.Add(UIKit.Text(ad.text, "news__ad-text"));
                    ads.Add(box);
                }
                c.Add(ads);
            }
            sheet.Add(scroll);
            return sheet;
        }

        public override void OnOpen() => Sfx.Play(Sfx.Paper);
    }

    // ==================================================================== desk drawer

    public class DrawerPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Drawer;

        VisualElement doc;
        VisualElement tabs;
        Label counter;
        int index;
        readonly List<Button> tabButtons = new List<Button>();

        public DrawerPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var tray = UIKit.Div("drawer");
            tabs = UIKit.Div("drawer__tabs");
            tabs.Add(UIKit.Text("DESK DRAWER", "section"));
            for (int i = 0; i < UI.Room.drawer.Count; i++)
            {
                int k = i;
                var b = UIKit.Btn(UI.Room.drawer[i].title, () => Show(k), "drawer__tab");
                tabButtons.Add(b);
                tabs.Add(b);
            }
            tray.Add(tabs);
            var right = UIKit.Div("grow");
            doc = UIKit.Div("doc");
            doc.style.flexGrow = 1;
            right.Add(doc);
            var nav = UIKit.Div("doc__nav");
            nav.Add(UIKit.Btn("◀  Previous", () => Show(index - 1)));
            counter = UIKit.Text("", "t-bold");
            counter.style.color = new Color(0.98f, 0.95f, 0.88f);
            nav.Add(counter);
            nav.Add(UIKit.Btn("Next  ▶", () => Show(index + 1)));
            right.Add(nav);
            tray.Add(right);
            Show(0);
            return tray;
        }

        void Show(int i)
        {
            var docs = UI.Room.drawer;
            if (docs.Count == 0)
                return;
            index = (i + docs.Count) % docs.Count;
            var d = docs[index];
            doc.Clear();
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            var c = scroll.contentContainer;
            c.Add(UIKit.Text(d.issuer?.ToUpperInvariant(), "doc__issuer"));
            c.Add(UIKit.Text(d.title, "doc__title"));
            foreach (var f in d.fields)
            {
                var row = UIKit.Div("doc__field");
                row.Add(UIKit.Text(f.label, "doc__label"));
                if (f.isFact)
                    row.Add(UIKit.Chip(f.value, f.factKind));
                else
                    row.Add(UIKit.Text(f.value, "doc__value"));
                c.Add(row);
            }
            if (!string.IsNullOrEmpty(d.body))
                c.Add(UIKit.Text(d.body, "doc__body"));
            if (d.image != null)
                c.Add(UIKit.Image(d.image, "doc__image"));
            doc.Add(scroll);
            if (!string.IsNullOrEmpty(d.stamp))
            {
                var stamp = UIKit.Image(UISkin.Tex(d.stamp), "doc__stamp");
                stamp.pickingMode = PickingMode.Ignore;
                doc.Add(stamp);
            }
            counter.text = $"{index + 1} / {docs.Count}";
            for (int k = 0; k < tabButtons.Count; k++)
                tabButtons[k].EnableInClassList("drawer__tab--on", k == index);
            Sfx.Play(Sfx.Paper, 0.6f);
        }
    }

    // ==================================================================== cork board

    public class BoardPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Board;

        VisualElement board;
        VisualElement zoom;

        public BoardPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            board = UIKit.Div("board");
            var frame = UIKit.Div("board__frame");
            frame.pickingMode = PickingMode.Ignore;
            board.Add(frame);
            const float W = 1272f, H = 872f;
            string[] pins = { "#D2403A", "#3F76C8", "#E9BE3A", "#4CA05A" };
            for (int i = 0; i < UI.Room.board.Count; i++)
            {
                var item = UI.Room.board[i];
                var e = UIKit.Image(item.image, "board__item");
                float w = item.width * W;
                float aspect = item.image != null ? (float)item.image.height / item.image.width : 1f;
                e.style.left = item.position.x * W;
                e.style.top = item.position.y * H;
                e.style.width = w;
                e.style.height = w * aspect;
                e.style.rotate = new Rotate(new Angle(item.rotation, AngleUnit.Degree));
                var pin = UIKit.Div("board__pin");
                pin.style.backgroundColor = PhoneScreen.Hex(pins[i % pins.Length]);
                pin.pickingMode = PickingMode.Ignore;
                e.Add(pin);
                e.tooltip = item.title;
                var captured = item;
                e.RegisterCallback<ClickEvent>(ev =>
                {
                    Zoom(captured);
                    ev.StopPropagation();
                });
                board.Add(e);
            }
            return board;
        }

        void Zoom(BoardItem item)
        {
            CloseZoom();
            Sfx.Play(Sfx.Paper, 0.6f);
            zoom = UIKit.Div("board__zoom");
            var img = UIKit.Image(item.image, "board__zoom-image");
            zoom.Add(img);
            var side = UIKit.Div("board__zoom-side", "paper");
            side.Add(UIKit.Text(item.title, "doc__title"));
            foreach (var f in item.details)
            {
                var row = UIKit.Div("doc__field");
                row.Add(UIKit.Text(f.label, "doc__label"));
                if (f.isFact)
                    row.Add(UIKit.Chip(f.value, f.factKind));
                else
                    row.Add(UIKit.Text(f.value, "doc__value"));
                side.Add(row);
            }
            foreach (var entry in item.calendar)
            {
                var row = UIKit.Div("doc__field");
                row.Add(UIKit.Text($"Oct {entry.day}", "doc__label"));
                row.Add(UIKit.Text(entry.text, "doc__value"));
                side.Add(row);
            }
            side.Add(UIKit.Btn("Back to the board", CloseZoom));
            zoom.Add(side);
            zoom.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == zoom)
                    CloseZoom();
            });
            board.Add(zoom);
        }

        void CloseZoom()
        {
            zoom?.RemoveFromHierarchy();
            zoom = null;
        }

        public override bool Back()
        {
            if (zoom == null)
                return false;
            CloseZoom();
            return true;
        }
    }

    // ==================================================================== wallet

    public class WalletPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Wallet;

        VisualElement big;
        VisualElement side;
        readonly List<VisualElement> thumbs = new List<VisualElement>();
        int current;
        bool showingBack;

        public WalletPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var wallet = UIKit.Div("wallet");
            var stitch = UIKit.Div("wallet__stitch");
            stitch.pickingMode = PickingMode.Ignore;
            wallet.Add(stitch);
            var row = UIKit.Div("wallet__cards");
            for (int i = 0; i < UI.Room.wallet.Count; i++)
            {
                int k = i;
                var t = UIKit.Image(UI.Room.wallet[i].front, "wallet__thumb");
                t.tooltip = UI.Room.wallet[i].title;
                t.RegisterCallback<ClickEvent>(e =>
                {
                    Select(k);
                    e.StopPropagation();
                });
                thumbs.Add(t);
                row.Add(t);
            }
            wallet.Add(row);
            var main = UIKit.Div("wallet__main");
            big = UIKit.Div("wallet__card");
            big.tooltip = "Click to flip";
            big.RegisterCallback<ClickEvent>(e =>
            {
                Flip();
                e.StopPropagation();
            });
            main.Add(big);
            side = UIKit.Div("wallet__side", "paper");
            main.Add(side);
            wallet.Add(main);
            Select(0);
            return wallet;
        }

        void Select(int i)
        {
            if (UI.Room.wallet.Count == 0)
                return;
            current = i;
            showingBack = false;
            var card = UI.Room.wallet[i];
            UIKit.SetImage(big, card.front);
            for (int k = 0; k < thumbs.Count; k++)
                thumbs[k].EnableInClassList("wallet__thumb--on", k == i);
            side.Clear();
            side.Add(UIKit.Text(card.title, "doc__title"));
            foreach (var f in card.details)
            {
                var row = UIKit.Div("doc__field");
                row.Add(UIKit.Text(f.label, "doc__label"));
                if (f.isFact)
                    row.Add(UIKit.Chip(f.value, f.factKind));
                else
                    row.Add(UIKit.Text(f.value, "doc__value"));
                side.Add(row);
            }
            var flip = UIKit.Btn(card.back != null ? "Flip the card" : "No back side", Flip);
            flip.SetEnabled(card.back != null);
            side.Add(flip);
            Sfx.Play(Sfx.Click);
        }

        void Flip()
        {
            var card = UI.Room.wallet[current];
            if (card.back == null)
                return;
            big.AddToClassList("wallet__card--flip");
            Sfx.Play(Sfx.Paper, 0.5f);
            big.schedule.Execute(() =>
            {
                showingBack = !showingBack;
                UIKit.SetImage(big, showingBack ? card.back : card.front);
                big.RemoveFromClassList("wallet__card--flip");
            }).ExecuteLater(150);
        }
    }

    // ==================================================================== notebook

    /// <summary>Two tabs: the rules read so far, and the current case (caller, claims, facts heard).</summary>
    public class NotebookPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Notebook;

        VisualElement page;
        Button rulesTab;
        Button caseTab;
        bool showCase;

        public NotebookPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var book = UIKit.Div("notebook");
            var lines = UIKit.Div("notebook__lines");
            lines.pickingMode = PickingMode.Ignore;
            book.Add(lines);
            var rings = UIKit.Div("notebook__rings");
            rings.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 9; i++)
                rings.Add(UIKit.Div("notebook__ring"));
            book.Add(rings);
            var tabs = UIKit.Div("notebook__tabs");
            rulesTab = UIKit.Btn("Rules", () => ShowTab(false), "notebook__tab");
            caseTab = UIKit.Btn("Case", () => ShowTab(true), "notebook__tab");
            tabs.Add(rulesTab);
            tabs.Add(caseTab);
            book.Add(tabs);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            page = scroll.contentContainer;
            book.Add(scroll);
            return book;
        }

        public void ShowTab(bool caseTabOn)
        {
            showCase = caseTabOn;
            rulesTab.EnableInClassList("notebook__tab--on", !showCase);
            caseTab.EnableInClassList("notebook__tab--on", showCase);
            page.Clear();
            if (showCase)
                BuildCase();
            else
                BuildRules();
            Sfx.Play(Sfx.Paper, 0.5f);
        }

        public override void OnOpen() => ShowTab(showCase);

        void BuildRules()
        {
            page.Add(UIKit.Text("Rules I've learned", "hand-title"));
            if (UI.Room.rules.Count == 0)
                page.Add(UIKit.Text("Nothing yet. The newspaper teaches a rule every day.", "hand"));
            for (int i = UI.Room.rules.Count - 1; i >= 0; i--)
            {
                var r = UI.Room.rules[i];
                var entry = UIKit.Div("rule-entry");
                entry.Add(UIKit.Text($"{UI.Room.rules.Count - i}. {r.title}", "rule-entry__title"));
                entry.Add(UIKit.Text(r.text, "hand"));
                if (!string.IsNullOrEmpty(r.learnedOn))
                    entry.Add(UIKit.Text("— " + r.learnedOn, "hand", "t-muted"));
                page.Add(entry);
            }
        }

        void BuildCase()
        {
            var c = UI.CurrentCase;
            page.Add(UIKit.Text("Today's case", "hand-title"));
            if (c == null)
            {
                page.Add(UIKit.Text("No call yet. When someone calls, their claims and every number they give end up here.", "hand"));
                return;
            }
            var card = UIKit.Div("case-card");
            card.Add(UIKit.Portrait(c.caller.portrait));
            var who = UIKit.Div("grow");
            who.Add(UIKit.Text(c.callerTitle, "rule-entry__title"));
            var chips = UIKit.Div("chip-row");
            chips.Add(UIKit.Chip(c.caller.number, FactKind.Phone, "Caller ID"));
            who.Add(chips);
            card.Add(who);
            page.Add(card);
            page.Add(UIKit.Text("They claim", "rule-entry__title"));
            foreach (var claim in c.claims)
                page.Add(UIKit.Text("• " + claim, "hand"));
            page.Add(UIKit.Text("Heard or found", "rule-entry__title"));
            var facts = UIKit.Div("chip-row");
            foreach (var f in c.facts)
                facts.Add(UIKit.Chip(f));
            page.Add(facts);
            page.Add(UIKit.Text("Check: whose account is it? Is the number really theirs? What does the family chat say?", "hand", "hand--red"));
        }
    }
}
