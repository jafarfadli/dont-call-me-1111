using System;
using System.Collections.Generic;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    // ==================================================================== Nuri Bank

    /// <summary>
    /// Balances, history, alerts and the transfer flow. The transfer shows the account holder's
    /// name before anything is sent: the check that exposes most scams in the game.
    /// </summary>
    public class BankApp : PhoneApp
    {
        public override string Id => "bank";
        public override string Name => "Nuri Bank";
        public override string Icon => "app_bank";
        public override int Badge => unreadAlerts;

        int unreadAlerts;

        public BankAccount Main => Data.bank.accounts.Count > 0 ? Data.bank.accounts[0] : null;

        public override PhoneScreen CreateHome() => new HomeScreenView(this);

        public void Deposit(long amount, string from, string memo)
        {
            if (Main != null)
                Main.balance += amount;
            Data.bank.transactions.Insert(0, new BankTransaction { when = "Today " + GameClock.Now, counterparty = from, memo = memo, amount = amount });
            Notify("Deposit " + FactText.Won(amount), $"From {from}" + (string.IsNullOrEmpty(memo) ? "" : $" · memo \"{memo}\""), false);
        }

        /// <summary>Alerts that came in before the day started and haven't been read.</summary>
        public void AddUnreadAlerts(int count) => unreadAlerts += Mathf.Max(0, count);

        public void Notify(string title, string body, bool alert)
        {
            Data.bank.notices.Insert(0, new BankNotice { when = "Today " + GameClock.Now, title = title, body = body, alert = alert });
            unreadAlerts++;
            Sfx.Play(Sfx.Pop);
            Phone.UI.Toast("app_bank", "Nuri Bank", title);
        }

        public PhoneScreen CreateTransfer(string account = null, long amount = 0) => new TransferScreen(this, account, amount);

        /// <summary>Takes the money from the main account and records it (after the PIN, or a verdict on the call).</summary>
        public void RecordTransfer(string holder, string bank, string number, long won)
        {
            Data.bank.Change(-won);
            Data.bank.transactions.Insert(0, new BankTransaction
            {
                when = "Today " + GameClock.Now, counterparty = holder, memo = $"{bank} {number}", amount = -won
            });
        }

        // ---------------------------------------------------------------- screens

        class HomeScreenView : PhoneScreen
        {
            readonly BankApp app;

            public HomeScreenView(BankApp app) : base(app.Phone, "Nuri Bank", "#2F7A6A")
            {
                this.app = app;
            }

            public override void OnShow()
            {
                Content.Clear();
                var data = app.Data.bank;
                for (int i = 0; i < data.accounts.Count; i++)
                {
                    var a = data.accounts[i];
                    var card = UIKit.Div("bank-card", i > 0 ? "bank-card--savings" : null);
                    card.Add(UIKit.Text(a.name, "bank-card__name"));
                    card.Add(UIKit.Text(a.number, "bank-card__number"));
                    card.Add(UIKit.Text(FactText.Won(a.balance), "bank-card__balance"));
                    Content.Add(card);
                }
                var actions = UIKit.Div("bank-actions");
                actions.Add(UIKit.Btn("Transfer", () => app.Phone.Push(app.CreateTransfer()), "btn--green"));
                actions.Add(UIKit.Btn("History", () => app.Phone.Push(History()), null));
                actions.Add(UIKit.Btn(app.unreadAlerts > 0 ? $"Alerts ({app.unreadAlerts})" : "Alerts", () => app.Phone.Push(Alerts()),
                                      app.unreadAlerts > 0 ? "btn--amber" : null));
                Content.Add(actions);
                Section("Recent");
                int n = 0;
                foreach (var t in data.transactions)
                {
                    if (n++ >= 5)
                        break;
                    TxRow(t, Content);
                }
            }

            void TxRow(BankTransaction t, VisualElement parent)
            {
                var row = UIKit.Div("list-row");
                var text = UIKit.Div("list-row__text");
                text.Add(UIKit.Text(t.counterparty, "list-row__title"));
                text.Add(UIKit.Text(t.when + (string.IsNullOrEmpty(t.memo) ? "" : $" · {t.memo}"), "list-row__subtitle"));
                row.Add(text);
                var amount = UIKit.Text((t.amount > 0 ? "+" : "") + FactText.Won(t.amount), "list-row__title", t.amount > 0 ? "amount-in" : "amount-out");
                row.Add(amount);
                parent.Add(row);
            }

            PhoneScreen History()
            {
                var s = new PhoneScreen(app.Phone, "History", "#2F7A6A");
                foreach (var t in app.Data.bank.transactions)
                    TxRow(t, s.Content);
                return s;
            }

            PhoneScreen Alerts()
            {
                app.unreadAlerts = 0;
                ClueEvents.Raise(ClueEvent.BankAlerts);
                var s = new PhoneScreen(app.Phone, "Alerts", "#2F7A6A");
                foreach (var n in app.Data.bank.notices)
                {
                    var box = UIKit.Div(n.alert ? "web-warning" : "web-notice");
                    box.Add(UIKit.Text($"<b>{n.title}</b>"));
                    box.Add(UIKit.Text(n.body));
                    box.Add(UIKit.Text(n.when, "msg__time"));
                    s.Content.Add(box);
                }
                return s;
            }
        }

        class TransferScreen : PhoneScreen
        {
            readonly BankApp app;
            string bank;
            readonly TextField account;
            readonly TextField amount;
            readonly Label error;
            readonly List<VisualElement> bankChips = new List<VisualElement>();

            public TransferScreen(BankApp app, string prefillAccount, long prefillAmount) : base(app.Phone, "Transfer", "#2F7A6A")
            {
                this.app = app;
                var from = app.Main;
                if (from != null)
                    Content.Add(UIKit.Kv("From", $"{from.name} · {FactText.Won(from.balance)}"));
                Section("To bank");
                var choice = UIKit.Div("bank-choice");
                foreach (string b in app.Data.bank.banks)
                {
                    var chip = UIKit.Div("chip");
                    var label = UIKit.Text(b, "chip__label");
                    label.pickingMode = PickingMode.Ignore;
                    chip.Add(label);
                    string captured = b;
                    chip.RegisterCallback<ClickEvent>(e =>
                    {
                        SelectBank(captured);
                        e.StopPropagation();
                    });
                    chip.userData = b;
                    bankChips.Add(chip);
                    choice.Add(chip);
                }
                Content.Add(choice);
                Section("Account number");
                account = PasteField("e.g. 110-123-456789", null, _ => { });
                if (!string.IsNullOrEmpty(prefillAccount))
                    account.value = prefillAccount;
                Section("Amount (won)");
                amount = PasteField("e.g. 50000", null, _ => { });
                if (prefillAmount > 0)
                    amount.value = prefillAmount.ToString();
                error = UIKit.Text("", "recipient__note");
                Content.Add(error);
                Content.Add(UIKit.Btn("Next", Next, "btn--green"));
                Content.Add(UIKit.Text("You'll see the account holder's name before anything is sent.", "t-muted", "t-center"));
            }

            void SelectBank(string b)
            {
                bank = b;
                foreach (var chip in bankChips)
                    chip.EnableInClassList("chip--selected", (string)chip.userData == b);
            }

            void Next()
            {
                error.text = "";
                string digits = FactText.Digits(account.value);
                long won = 0;
                long.TryParse(FactText.Digits(amount.value), out won);
                if (string.IsNullOrEmpty(bank))
                {
                    Fail("Choose the recipient's bank first.");
                    return;
                }
                if (digits.Length < 8)
                {
                    Fail("Enter the full account number.");
                    return;
                }
                if (won <= 0)
                {
                    Fail("Enter an amount.");
                    return;
                }
                // The savings cover what the everyday account can't (see BankData.Change).
                long total = 0;
                foreach (var a in app.Data.bank.accounts)
                    total += a.balance;
                if (won > total)
                {
                    Fail("Not enough money in your accounts.");
                    return;
                }
                var target = app.Dir.FindAccount(bank, account.value);
                if (target == null)
                {
                    var elsewhere = app.Dir.FindAccount(account.value);
                    Fail(elsewhere != null ? $"No such account at {bank}. Check the bank." : "No such account. Check the number.");
                    return;
                }
                app.Phone.Push(new ConfirmScreen(app, target, won));
            }

            void Fail(string message)
            {
                error.text = message;
                Sfx.Play(Sfx.Error, 0.6f);
            }
        }

        class ConfirmScreen : PhoneScreen
        {
            public ConfirmScreen(BankApp app, DirAccount target, long won) : base(app.Phone, "Check the recipient", "#2F7A6A")
            {
                ClueEvents.Raise(ClueEvent.RecipientShown, target.number);
                var box = UIKit.Div("recipient");
                box.Add(UIKit.Text("You are sending money to", "recipient__label"));
                box.Add(UIKit.Text(target.holder, "recipient__name"));
                box.Add(UIKit.Text($"{target.bank} · {target.number}", "recipient__sub"));
                box.Add(UIKit.Text(FactText.Won(won), "recipient__amount"));
                box.Add(UIKit.Text("Is this the person you meant? Scammers use other people's accounts.", "recipient__note"));
                var chips = UIKit.Div("chip-row");
                chips.style.justifyContent = Justify.Center;
                chips.Add(UIKit.Chip(target.holder, FactKind.Name));
                chips.Add(UIKit.Chip(target.number, FactKind.Account));
                box.Add(chips);
                Content.Add(box);
                var actions = UIKit.Div("bank-actions");
                actions.Add(UIKit.Btn("Cancel", () => app.Phone.Back(), null));
                var send = UIKit.Btn("Send", () => app.Phone.Push(new PinScreen(app, target, won)), "btn--red");
                actions.Add(send);
                Content.Add(actions);
                // During a case the phone is for checking; the decision is made on the call.
                if (app.Phone.UI.CaseCallActive)
                {
                    send.SetEnabled(false);
                    Content.Add(UIKit.Text("You're on a call. To send this money, choose \"Send the money\" in your verdict on the call.", "recipient__note"));
                }
            }
        }

        class PinScreen : PhoneScreen
        {
            readonly BankApp app;
            readonly DirAccount target;
            readonly long won;
            readonly VisualElement[] dots = new VisualElement[6];
            int entered;

            public PinScreen(BankApp app, DirAccount target, long won) : base(app.Phone, "Enter PIN", "#2F7A6A")
            {
                this.app = app;
                this.target = target;
                this.won = won;
                Content.Add(UIKit.Text($"Send {FactText.Won(won)} to {target.holder}", "detail__line", "t-center"));
                var row = UIKit.Div("pin-dots");
                for (int i = 0; i < 6; i++)
                {
                    dots[i] = UIKit.Div("pin-dot");
                    row.Add(dots[i]);
                }
                Content.Add(row);
                var pad = UIKit.Div("keypad");
                foreach (string k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "" })
                {
                    if (k == "")
                    {
                        var spacer = UIKit.Div("key");
                        spacer.style.opacity = 0;
                        pad.Add(spacer);
                        continue;
                    }
                    pad.Add(UIKit.Btn(k, Press, "key"));
                }
                Content.Add(pad);
            }

            void Press()
            {
                if (entered >= 6)
                    return;
                dots[entered++].AddToClassList("pin-dot--on");
                Sfx.Play(Sfx.Tick, 0.4f);
                if (entered == 6)
                    Root.schedule.Execute(Send).ExecuteLater(350);
            }

            void Send()
            {
                app.RecordTransfer(target.holder, target.bank, target.number, won);
                Sfx.Play(Sfx.Success);
                var done = new PhoneScreen(app.Phone, "Sent", "#2F7A6A");
                var box = UIKit.Div("recipient");
                box.Add(UIKit.Text("Transfer complete", "recipient__label"));
                box.Add(UIKit.Text(FactText.Won(won), "recipient__amount"));
                box.Add(UIKit.Text($"to {target.holder}\n{target.bank} · {target.number}", "recipient__sub"));
                done.Content.Add(box);
                done.Content.Add(UIKit.Btn("Done", () => app.Phone.OpenApp(app), "btn--green"));
                app.Phone.Push(done);
                PhoneEvents.RaiseTransfer(target.bank, target.number, won);
            }
        }
    }

    // ==================================================================== CheckFirst (fraud report lookup)

    public class CheckFirstApp : PhoneApp
    {
        public override string Id => "checkfirst";
        public override string Name => "CheckFirst";
        public override string Icon => "app_checkfirst";

        public override PhoneScreen CreateHome() => new SearchScreen(this);

        class SearchScreen : PhoneScreen
        {
            readonly CheckFirstApp app;
            readonly TextField field;
            readonly VisualElement suggestions;
            readonly VisualElement result;

            public SearchScreen(CheckFirstApp app) : base(app.Phone, "CheckFirst", "#B8453A")
            {
                this.app = app;
                Content.Add(UIKit.Text("Look up reports on a phone or account number before you trust it.", "detail__line"));
                field = PasteField("Phone or account number", "Check", Check);
                suggestions = UIKit.Div("chip-row");
                Content.Add(suggestions);
                result = UIKit.Div();
                Content.Add(result);
            }

            public override void OnShow()
            {
                suggestions.Clear();
                foreach (var f in app.Phone.UI.SuggestedFacts(FactKind.Phone, FactKind.Account))
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
                string digits = FactText.Digits(query);
                if (digits.Length < 3)
                    return;
                Sfx.Play(Sfx.Click);
                ClueEvents.Raise(ClueEvent.NumberChecked, query);
                List<string> reports = null;
                string what = query;
                var n = app.Dir.FindNumber(query);
                var a = app.Dir.FindAccount(query);
                if (n != null)
                {
                    reports = n.reports;
                    what = n.number;
                }
                else if (a != null)
                {
                    reports = a.reports;
                    what = $"{a.bank} {a.number}";
                }
                int count = reports?.Count ?? 0;
                var box = UIKit.Div("detail");
                box.Add(UIKit.Text(what, "detail__line", "t-center"));
                box.Add(UIKit.Text(count > 0 ? $"{count} report{(count > 1 ? "s" : "")}" : "No reports",
                                   "report-count", count > 0 ? "report-count--bad" : "report-count--clean"));
                box.Add(UIKit.Text(count > 0 ? "in the last 3 months" : "in the last 3 months", "t-muted", "t-center"));
                if (reports != null)
                    foreach (var r in reports)
                        box.Add(UIKit.Text(r, "report-item"));
                var note = UIKit.Div("web-notice");
                note.Add(UIKit.Text("<b>No reports doesn't mean safe.</b> New numbers and accounts start clean. Check who is behind them too."));
                box.Add(note);
                result.Add(box);
            }
        }
    }
}
