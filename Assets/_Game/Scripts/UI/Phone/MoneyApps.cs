using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    // ==================================================================== Nuri Bank

    /// <summary>
    /// Jiwoo's accounts and what went in and out. Money is only ever sent through the verdict on
    /// the call, which records the transfer here.
    /// </summary>
    public class BankApp : PhoneApp
    {
        public override string Id => "bank";
        public override string Name => Loc.T("Nuri Bank");
        public override string Icon => "app_bank";

        public BankAccount Main => Data.bank.accounts.Count > 0 ? Data.bank.accounts[0] : null;

        public override PhoneScreen CreateHome() => new HomeScreenView(this);

        public void Deposit(long amount, string from, string memo)
        {
            if (Main != null)
                Main.balance += amount;
            Data.bank.transactions.Insert(0, new BankTransaction { when = Loc.Today + " " + GameClock.Now, counterparty = from, memo = memo, amount = amount });
            Sfx.Play(Sfx.Pop);
            Phone.UI.Toast("app_bank", Loc.T("Nuri Bank"), Loc.F("Deposit {0}", FactText.Won(amount)) + " · " + from);
        }

        /// <summary>Takes the money from the main account and records it (the verdict on the call sends it).</summary>
        public void RecordTransfer(string holder, string bank, string number, long won)
        {
            Data.bank.Change(-won);
            Data.bank.transactions.Insert(0, new BankTransaction
            {
                when = Loc.Today + " " + GameClock.Now, counterparty = holder, memo = $"{bank} {number}", amount = -won
            });
        }

        class HomeScreenView : PhoneScreen
        {
            readonly BankApp app;

            public HomeScreenView(BankApp app) : base(app.Phone, Loc.T("Nuri Bank"), "#2F7A6A")
            {
                this.app = app;
            }

            public override void OnShow()
            {
                ClueEvents.Raise(ClueEvent.BankOpened);
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
                Section(Loc.T("History"));
                foreach (var t in data.transactions)
                    TxRow(t, Content);
                Content.Add(UIKit.Text(Loc.T("Transfers show the recipient's name before you confirm."), "t-muted", "t-center"));
            }

            static void TxRow(BankTransaction t, VisualElement parent)
            {
                // Who and how much on one line, when and what for on the line under it.
                var row = UIKit.Div("list-row");
                var text = UIKit.Div("list-row__text");
                var top = UIKit.Div("list-row__top");
                top.Add(UIKit.Text(t.counterparty, "list-row__title"));
                top.Add(UIKit.Text((t.amount > 0 ? "+" : "") + FactText.Won(t.amount), "list-row__amount", t.amount > 0 ? "amount-in" : "amount-out"));
                text.Add(top);
                text.Add(UIKit.Text(t.when + (string.IsNullOrEmpty(t.memo) ? "" : $" · {t.memo}"), "list-row__subtitle"));
                row.Add(text);
                parent.Add(row);
            }
        }
    }
}
