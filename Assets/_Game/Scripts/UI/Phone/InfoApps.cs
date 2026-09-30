using System.Linq;
using DontCallMe.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    // ==================================================================== Browser

    /// <summary>
    /// Search and web pages from the world directory: official sites with their numbers and notices,
    /// lookup forms, news, and the fake sites behind scam links (with their real address showing).
    /// </summary>
    public class BrowserApp : PhoneApp
    {
        public override string Id => "browser";
        public override string Name => "Browser";
        public override string Icon => "app_browser";

        public override PhoneScreen CreateHome() => new SearchHome(this);

        public void OpenUrl(string url)
        {
            var page = Dir.FindPage(url);
            Phone.Push(page != null ? new PageScreen(this, page) : NotFound(url));
            PhoneEvents.RaiseLink(url);
        }

        PhoneScreen NotFound(string url)
        {
            var s = new PhoneScreen(Phone, "Browser", "#5A6FB0");
            s.Content.Add(UIKit.Text(url, "url", "url--suspect"));
            s.Content.Add(UIKit.Text("This site can't be reached.", "web-title"));
            s.Content.Add(UIKit.Text("Check the address. It may have been taken down.", "web-p"));
            return s;
        }

        class SearchHome : PhoneScreen
        {
            readonly BrowserApp app;
            readonly TextField field;
            readonly VisualElement suggestions;

            public SearchHome(BrowserApp app) : base(app.Phone, "Browser", "#5A6FB0")
            {
                this.app = app;
                field = PasteField("Search or type an address", "Go", Go);
                suggestions = UIKit.Div("chip-row");
                Content.Add(suggestions);
                Section("Bookmarks");
                foreach (var p in app.Dir.pages.Where(p => p.official).Take(4))
                {
                    var page = p;
                    Row("app_browser", false, p.title, p.url, null, () => app.Phone.Push(new PageScreen(app, page)));
                }
            }

            public override void OnShow()
            {
                suggestions.Clear();
                foreach (var f in app.Phone.UI.SuggestedFacts(FactKind.Name, FactKind.Phone, FactKind.Url, FactKind.Case, FactKind.Text))
                {
                    var fact = f;
                    var chip = UIKit.Div("chip");
                    var label = UIKit.Text(fact.value, "chip__label");
                    label.pickingMode = PickingMode.Ignore;
                    chip.Add(label);
                    chip.RegisterCallback<ClickEvent>(e =>
                    {
                        field.value = fact.value;
                        Go(fact.value);
                        e.StopPropagation();
                    });
                    suggestions.Add(chip);
                }
            }

            void Go(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;
                string q = query.Trim();
                if (q.Contains(".") && !q.Contains(" "))
                {
                    app.OpenUrl(q);
                    return;
                }
                app.Phone.Push(new ResultsScreen(app, q));
            }
        }

        class ResultsScreen : PhoneScreen
        {
            public ResultsScreen(BrowserApp app, string query) : base(app.Phone, "Results", "#5A6FB0")
            {
                Content.Add(UIKit.Text($"Results for \"{query}\"", "t-muted"));
                var results = app.Dir.Search(query);
                if (results.Count == 0)
                {
                    Content.Add(UIKit.Text("No results. Try the organisation's name.", "web-p"));
                    return;
                }
                foreach (var p in results)
                {
                    var page = p;
                    var row = UIKit.Div("list-row");
                    var text = UIKit.Div("list-row__text");
                    var title = UIKit.Div("row");
                    title.Add(UIKit.Text(p.title, "list-row__title"));
                    if (p.official)
                        title.Add(UIKit.Text("OFFICIAL", "badge-official"));
                    text.Add(title);
                    text.Add(UIKit.Text(p.url, "url", p.official ? "url--official" : null));
                    var first = p.blocks.FirstOrDefault(b => b.kind == WebBlockKind.Paragraph);
                    if (first != null)
                        text.Add(UIKit.Text(first.text.Length > 90 ? first.text.Substring(0, 90) + "…" : first.text, "list-row__subtitle"));
                    row.Add(text);
                    row.RegisterCallback<ClickEvent>(e =>
                    {
                        Sfx.Play(Sfx.Click, 0.5f);
                        app.Phone.Push(new PageScreen(app, page));
                        e.StopPropagation();
                    });
                    Content.Add(row);
                }
            }
        }

        class PageScreen : PhoneScreen
        {
            public PageScreen(BrowserApp app, WebPage page) : base(app.Phone, page.site, "#5A6FB0")
            {
                var urlRow = UIKit.Div("row");
                if (page.official)
                    urlRow.Add(UIKit.Icon("ic_lock", "list-row__icon"));
                urlRow.Add(UIKit.Text(page.url, "url", page.official ? "url--official" : "url--suspect"));
                Content.Add(urlRow);
                Content.Add(UIKit.Chip(page.url, FactKind.Url));
                Content.Add(UIKit.Text(page.title, "web-title"));
                foreach (var b in page.blocks)
                    Content.Add(Block(app, page, b));
            }

            VisualElement Block(BrowserApp app, WebPage page, WebBlock b)
            {
                switch (b.kind)
                {
                    case WebBlockKind.Heading:
                        return UIKit.Text($"<b>{b.text}</b>", "web-p");
                    case WebBlockKind.Contact:
                    {
                        var row = UIKit.Div("kv");
                        row.Add(UIKit.Text(b.text, "kv__key"));
                        var right = UIKit.Div("row");
                        right.Add(UIKit.Chip(b.value, FactKind.Phone));
                        var call = UIKit.Btn("Call", () => app.Phone.Dial(b.value), "btn--green");
                        call.style.minHeight = 44;
                        right.Add(call);
                        row.Add(right);
                        return row;
                    }
                    case WebBlockKind.Notice:
                    {
                        var box = UIKit.Div("web-notice");
                        box.Add(UIKit.Text(b.text));
                        return box;
                    }
                    case WebBlockKind.Warning:
                    {
                        var box = UIKit.Div("web-warning");
                        box.Add(UIKit.Text(b.text));
                        return box;
                    }
                    case WebBlockKind.Lookup:
                    {
                        var box = UIKit.Div("web-form");
                        box.Add(UIKit.Text($"<b>{b.text}</b>"));
                        var result = UIKit.Div();
                        var row = UIKit.Div("field");
                        var field = new TextField();
                        field.textEdition.placeholder = "Enter a number";
                        row.Add(field);
                        row.Add(UIKit.Btn("Paste", () => field.value = Clipboard.Value ?? ""));
                        row.Add(UIKit.Btn("Look up", () => Lookup(page, field.value, result), "btn--blue"));
                        box.Add(row);
                        box.Add(result);
                        return box;
                    }
                    case WebBlockKind.Form:
                    {
                        var box = UIKit.Div("web-form");
                        box.Add(UIKit.Text($"<b>{b.text}</b>"));
                        var row = UIKit.Div("field");
                        var field = new TextField();
                        field.textEdition.placeholder = "Card number";
                        row.Add(field);
                        box.Add(row);
                        box.Add(UIKit.Btn(string.IsNullOrEmpty(b.value) ? "Pay" : b.value, () =>
                        {
                            Sfx.Play(Sfx.Error);
                            app.Phone.UI.Toast("app_browser", "Browser", "Payment submitted.");
                            PhoneEvents.RaiseLink(page.url + "#submitted");
                        }, "btn--red"));
                        return box;
                    }
                    default:
                        return UIKit.Text(b.text, "web-p");
                }
            }

            static void Lookup(WebPage page, string query, VisualElement result)
            {
                result.Clear();
                if (string.IsNullOrWhiteSpace(query))
                    return;
                string q = FactText.Digits(query);
                string qText = query.Trim().ToLowerInvariant();
                var hit = page.lookup.FirstOrDefault(l =>
                    (q.Length >= 4 && FactText.Digits(l.key) == q) || l.key.ToLowerInvariant() == qText);
                result.Add(UIKit.Text(hit != null ? hit.result : "No record found for this number.", "lookup-result"));
                Sfx.Play(Sfx.Click);
            }
        }
    }

    // ==================================================================== Parcels

    public class ParcelsApp : PhoneApp
    {
        public override string Id => "parcels";
        public override string Name => "Parcels";
        public override string Icon => "app_parcels";

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, "Parcels", "#9A6A42");
            var result = UIKit.Div();
            PasteFieldOn(s, "Tracking number", "Track", q =>
            {
                result.Clear();
                var hit = Data.parcels.Find(p => FactText.SameNumber(p.tracking, q));
                if (hit != null)
                    Phone.Push(Detail(hit));
                else if (FactText.Digits(q).Length > 0)
                    result.Add(UIKit.Text("Not found. No parcel with this tracking number is on its way to you.", "lookup-result"));
            });
            s.Content.Add(result);
            s.Content.Add(UIKit.Text("MY ORDERS", "section"));
            foreach (var p in Data.parcels)
            {
                var parcel = p;
                var row = UIKit.Div("list-row");
                row.Add(UIKit.Icon("app_parcels", "list-row__icon"));
                var text = UIKit.Div("list-row__text");
                text.Add(UIKit.Text(p.item, "list-row__title"));
                text.Add(UIKit.Text($"{p.seller} · {p.status}", "list-row__subtitle"));
                row.Add(text);
                row.RegisterCallback<ClickEvent>(e =>
                {
                    Phone.Push(Detail(parcel));
                    e.StopPropagation();
                });
                s.Content.Add(row);
            }
            return s;
        }

        TextField PasteFieldOn(PhoneScreen s, string placeholder, string button, System.Action<string> submit)
        {
            var row = UIKit.Div("field");
            var field = new TextField();
            field.textEdition.placeholder = placeholder;
            row.Add(field);
            row.Add(UIKit.Btn("Paste", () => field.value = Clipboard.Value ?? ""));
            row.Add(UIKit.Btn(button, () => submit(field.value), "btn--blue"));
            s.Content.Add(row);
            return field;
        }

        PhoneScreen Detail(Parcel p)
        {
            var s = new PhoneScreen(Phone, p.item, "#9A6A42");
            s.Content.Add(UIKit.Kv("Status", p.status));
            s.Content.Add(UIKit.Kv("Seller", p.seller));
            s.Content.Add(UIKit.Kv("Tracking", p.tracking, FactKind.Case));
            s.Content.Add(UIKit.Kv("Courier", p.courier));
            if (!string.IsNullOrEmpty(p.driver))
                s.Content.Add(UIKit.Kv("Driver", p.driver));
            if (!string.IsNullOrEmpty(p.driverPhone))
                s.Content.Add(UIKit.Kv("Driver phone", p.driverPhone, FactKind.Phone));
            if (!string.IsNullOrEmpty(p.window))
                s.Content.Add(UIKit.Kv("Delivery", p.window));
            s.Content.Add(UIKit.Kv("From", p.overseas ? "Overseas" : "Korea"));
            return s;
        }
    }

    // ==================================================================== Mail

    public class MailApp : PhoneApp
    {
        public override string Id => "mail";
        public override string Name => "Mail";
        public override string Icon => "app_mail";
        public override int Badge => Data.mails.Count(m => m.unread);

        public override PhoneScreen CreateHome()
        {
            var s = new PhoneScreen(Phone, "Inbox", "#C9A873");
            foreach (var m in Data.mails)
            {
                var mail = m;
                var row = UIKit.Div("list-row");
                var text = UIKit.Div("list-row__text");
                text.Add(UIKit.Text((m.unread ? "<b>" : "") + m.from + (m.unread ? "</b>" : ""), "list-row__title"));
                text.Add(UIKit.Text(m.subject, "list-row__subtitle"));
                row.Add(text);
                row.Add(UIKit.Text(m.when, "list-row__meta"));
                if (m.unread)
                    row.Add(UIKit.Div("dot-unread"));
                row.RegisterCallback<ClickEvent>(e =>
                {
                    mail.unread = false;
                    Phone.Push(Read(mail));
                    e.StopPropagation();
                });
                s.Content.Add(row);
            }
            return s;
        }

        PhoneScreen Read(MailItem m)
        {
            var s = new PhoneScreen(Phone, "Mail", "#C9A873");
            s.Content.Add(UIKit.Text(m.subject, "web-title"));
            s.Content.Add(UIKit.Text($"<b>{m.from}</b>", "detail__line"));
            s.Content.Add(UIKit.Chip(m.fromAddress, FactKind.Text));
            s.Content.Add(UIKit.Text(m.when, "t-muted"));
            s.Content.Add(UIKit.Div("rule-thin"));
            s.Content.Add(UIKit.Text(m.body, "web-p"));
            foreach (var a in m.attachments)
            {
                var chip = UIKit.Div("chip");
                chip.Add(UIKit.Text("Attachment: " + a, "chip__label"));
                s.Content.Add(chip);
            }
            return s;
        }
    }
}
