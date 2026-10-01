using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    public enum PanelId
    {
        None,
        Newspaper,
        Drawer,
        Calendar,
        Computer,
    }

    /// <summary>
    /// A full-screen room panel with a close button. Esc, right-click or a click on the dimmed room
    /// around it closes it.
    /// </summary>
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
            close.tooltip = Loc.T("Close (Esc)");
            box.Add(close);
            Root.Add(box);
            Root.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == Root)
                    ui.ClosePanel();
            });
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
            top.Add(UIKit.Text(Loc.Ko ? "SEOUL DAILY" : "서울데일리", "news__kor"));
            top.Add(UIKit.Text(n.masthead, "news__masthead"));
            top.Add(UIKit.Text(n.issue, "news__issue"));
            c.Add(top);
            var date = UIKit.Div("news__dateline");
            date.Add(UIKit.Text(n.dateLine));
            date.Add(UIKit.Text(Loc.T("MANGWON · MAPO · SEOUL")));
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
            side.Add(UIKit.Text(Loc.T("LOCAL NEWS"), "news__box-title"));
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
            tabs.Add(UIKit.Text(Loc.T("DESK DRAWER"), "section"));
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
            nav.Add(UIKit.Btn("◀  " + Loc.T("Previous"), () => Show(index - 1)));
            counter = UIKit.Text("", "t-bold");
            counter.style.color = new Color(0.98f, 0.95f, 0.88f);
            nav.Add(counter);
            nav.Add(UIKit.Btn(Loc.T("Next") + "  ▶", () => Show(index + 1)));
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
            ClueEvents.Raise(ClueEvent.DocumentViewed, d.title);
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

    // ==================================================================== wall calendar

    /// <summary>The pharmacy calendar from the cork board, with a list of what Jiwoo wrote on it and today marked.</summary>
    public class CalendarPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Calendar;

        public CalendarPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var cal = UI.Room.calendar;
            var wrap = UIKit.Div("calendar");
            wrap.Add(UIKit.Image(cal.image, "calendar__image"));
            var side = UIKit.Div("calendar__side", "paper");
            side.Add(UIKit.Text(cal.title, "doc__title"));
            side.Add(UIKit.Text(cal.todayLabel, "calendar__today"));
            side.Add(UIKit.Text(Loc.T("WRITTEN ON IT"), "section"));
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            foreach (var entry in cal.entries)
            {
                var row = UIKit.Div("calendar__row", entry.day == cal.today ? "calendar__row--today" : entry.day < cal.today ? "calendar__row--past" : null);
                row.Add(UIKit.Text(entry.label, "calendar__date"));
                row.Add(UIKit.Text(entry.text, "calendar__note"));
                scroll.contentContainer.Add(row);
            }
            side.Add(scroll);
            wrap.Add(side);
            return wrap;
        }

        public override void OnOpen() => Sfx.Play(Sfx.Paper, 0.6f);
    }

    // ==================================================================== computer

    /// <summary>
    /// The laptop, open on CheckFirst: look up a phone number or a bank account and see who owns
    /// it and what people reported about it. The caller's number and the account they gave are one
    /// click away as chips.
    /// </summary>
    public class ComputerPanel : RoomPanel
    {
        public override PanelId Id => PanelId.Computer;

        Button phoneTab;
        Button accountTab;
        Label prompt;
        TextField field;
        VisualElement suggestions;
        VisualElement result;
        bool accountMode;

        public ComputerPanel(UIManager ui) : base(ui) { }

        protected override VisualElement Build()
        {
            var laptop = UIKit.Div("computer");
            var window = UIKit.Div("browser");
            var bar = UIKit.Div("browser__bar");
            for (int i = 0; i < 3; i++)
                bar.Add(UIKit.Div("browser__dot", "browser__dot--" + i));
            bar.Add(UIKit.Text("CheckFirst", "browser__tab"));
            var address = UIKit.Div("browser__address");
            address.Add(UIKit.Icon("ic_lock", "browser__lock"));
            address.Add(UIKit.Text("https://checkfirst.kr", "browser__url"));
            bar.Add(address);
            window.Add(bar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("browser__page");
            var page = scroll.contentContainer;
            var head = UIKit.Div("check__head");
            head.Add(UIKit.Icon("app_checkfirst", "check__logo"));
            var brand = UIKit.Div("grow");
            brand.Add(UIKit.Text(Loc.T("CheckFirst"), "check__brand"));
            brand.Add(UIKit.Text(Loc.T("Who is really behind a number or an account? Look it up before you trust it."), "check__tagline"));
            head.Add(brand);
            page.Add(head);

            var tabs = UIKit.Div("check__tabs");
            phoneTab = UIKit.Btn(Loc.T("Check a phone number"), () => SetMode(false), "check__tab");
            accountTab = UIKit.Btn(Loc.T("Check a bank account"), () => SetMode(true), "check__tab");
            tabs.Add(phoneTab);
            tabs.Add(accountTab);
            page.Add(tabs);

            var form = UIKit.Div("check__form");
            prompt = UIKit.Text("", "check__prompt");
            form.Add(prompt);
            var row = UIKit.Div("field", "check__field");
            field = new TextField();
            row.Add(field);
            row.Add(UIKit.Btn(Loc.T("Paste"), () =>
            {
                if (!string.IsNullOrEmpty(Clipboard.Value))
                    field.value = Clipboard.Value;
            }));
            row.Add(UIKit.Btn(Loc.T("Check"), () => Check(field.value), "btn--blue"));
            field.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    Check(field.value);
            }, TrickleDown.TrickleDown);
            form.Add(row);
            suggestions = UIKit.Div("chip-row", "check__suggestions");
            form.Add(suggestions);
            page.Add(form);

            result = UIKit.Div("check__result");
            page.Add(result);
            window.Add(scroll);
            laptop.Add(window);
            SetMode(false, false);
            return laptop;
        }

        public override void OnOpen() => Sfx.Play(Sfx.Click);

        void SetMode(bool account, bool clear = true)
        {
            accountMode = account;
            phoneTab.EnableInClassList("check__tab--on", !account);
            accountTab.EnableInClassList("check__tab--on", account);
            prompt.text = Loc.T(account ? "Account number" : "Phone number");
            field.textEdition.placeholder = Loc.T(account ? "e.g. 110-123-456789" : "e.g. 010-1234-5678");
            if (clear)
            {
                field.value = "";
                result.Clear();
            }
            suggestions.Clear();
            var facts = UI.SuggestedFacts(account ? FactKind.Account : FactKind.Phone);
            if (facts.Count > 0)
                suggestions.Add(UIKit.Text(Loc.T("From the call and your notes:"), "check__hint"));
            foreach (var f in facts)
            {
                var fact = f;
                var chip = UIKit.Div("chip");
                var label = UIKit.Text(fact.value, "chip__label");
                label.pickingMode = PickingMode.Ignore;
                chip.Add(label);
                chip.RegisterCallback<ClickEvent>(e =>
                {
                    field.value = fact.value;
                    Check(fact.value);
                    e.StopPropagation();
                });
                suggestions.Add(chip);
            }
        }

        void Check(string query)
        {
            result.Clear();
            if (FactText.Digits(query).Length < 3)
                return;
            Sfx.Play(Sfx.Click);
            var number = UI.Directory.FindNumber(query);
            var account = UI.Directory.FindAccount(query);
            // Pasted into the wrong tab: switch to the one that knows it.
            if (accountMode && account == null && number != null)
                SetMode(false, false);
            else if (!accountMode && number == null && account != null)
                SetMode(true, false);
            field.value = query;
            ClueEvents.Raise(ClueEvent.NumberChecked, query);

            var card = UIKit.Div("check-card");
            if (accountMode ? account == null : number == null)
            {
                card.Add(UIKit.Text(query, "check-card__what"));
                card.Add(UIKit.Text(Loc.T(accountMode ? "No account with this number. Check the digits." : "No record of this number. Check the digits."), "check-card__none"));
                result.Add(card);
                return;
            }
            string what = accountMode ? $"{account.bank}  {account.number}" : number.number;
            string owner = accountMode ? account.holder : number.owner;
            string note = accountMode ? account.note : number.note;
            var reports = accountMode ? account.reports : number.reports;
            card.Add(UIKit.Text(what, "check-card__what"));
            var who = UIKit.Div("check-card__row");
            who.Add(UIKit.Text(Loc.T(accountMode ? "Account holder" : "Owner"), "check-card__label"));
            var name = UIKit.Div("check-card__owner");
            name.Add(UIKit.Text(owner, "check-card__name"));
            if (!accountMode && number.official)
                name.Add(UIKit.Text(Loc.T("OFFICIAL NUMBER"), "check-card__official"));
            who.Add(name);
            card.Add(who);
            if (!string.IsNullOrEmpty(note))
                card.Add(UIKit.Text(note, "check-card__note"));
            var rep = UIKit.Div("check-card__row");
            rep.Add(UIKit.Text(Loc.T("Reports"), "check-card__label"));
            int count = reports?.Count ?? 0;
            rep.Add(UIKit.Text(count > 1 ? Loc.F("{0} reports", count) : count == 1 ? Loc.T("1 report") : Loc.T("No reports"),
                               "check-card__count", count > 0 ? "check-card__count--bad" : "check-card__count--clean"));
            card.Add(rep);
            if (reports != null)
                foreach (string r in reports)
                    card.Add(UIKit.Text(r, "check-card__report"));
            card.Add(UIKit.Text(Loc.T("No reports doesn't mean safe: new numbers and accounts start clean. Check that the owner is who the caller says."), "check-card__tip"));
            result.Add(card);
        }
    }
}
