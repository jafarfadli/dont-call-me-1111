using System;
using System.Collections;
using System.IO;
using DontCallMe.Data;
using DontCallMe.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace DontCallMe.UI
{
    /// <summary>
    /// Development tool: walks through every panel and app, then plays the demo call three ways
    /// (hang up, send the transfer, call the bank back), the demo chat and an ordinary outgoing call,
    /// saving a screenshot of each step to Temp/UITour and logging every check that fails.
    /// Start it from Tools → Don't Call Me → UI → Capture UI Tour.
    /// </summary>
    public class UITour : MonoBehaviour
    {
        public string outputDir = "Temp/UITour";
        public int superSize = 2;
        public bool done;
        public int failures;

        UIManager ui;
        CallDirector director;
        DemoDirector demo;
        PhoneController phone;
        VisualElement root;
        int step;

        public void Begin()
        {
            ui = FindAnyObjectByType<UIManager>();
            director = FindAnyObjectByType<CallDirector>();
            demo = FindAnyObjectByType<DemoDirector>();
            // The tour drives calls itself: stop the day flow and the demo's own ring.
            FindAnyObjectByType<DayDirector>()?.Cancel();
            if (demo != null)
                demo.enabled = false;
            Directory.CreateDirectory(outputDir);
            foreach (var f in Directory.GetFiles(outputDir, "*.png"))
                File.Delete(f);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            phone = ui.PhoneView;
            root = ui.GetComponent<UIDocument>().rootVisualElement;
            yield return Shot("hud", 1.5f);
            yield return RoomPanels();
            yield return PhoneApps();
            yield return CallHangUp();
            yield return CallTransfer();
            yield return Chat();
            yield return OutgoingCall();
            done = true;
            Debug.Log($"[UITour] {step} screenshots in {outputDir}, {failures} failed checks");
        }

        // ---------------------------------------------------------------- room

        IEnumerator RoomPanels()
        {
            ui.OpenPanel(PanelId.Newspaper);
            yield return Shot("newspaper");
            ui.OpenPanel(PanelId.Drawer);
            yield return Shot("drawer_lease");
            ClickButton(ui.OpenPanelView.Root, "Next  ▶");
            yield return Shot("drawer_gasbill");
            ui.OpenPanel(PanelId.Board);
            yield return Shot("board");
            ClickFirst(ui.OpenPanelView.Root, "board__item", 1);
            yield return Shot("board_zoom");
            ui.OpenPanel(PanelId.Wallet);
            yield return Shot("wallet");
            ClickFirst(ui.OpenPanelView.Root, "wallet__card", 0);
            yield return Shot("wallet_back", 0.8f);
            ui.OpenPanel(PanelId.Notebook);
            yield return Shot("notebook_rules");
            ui.ClosePanel();
            Check(ui.OpenPanelView == null, "panel closes");
        }

        // ---------------------------------------------------------------- phone

        IEnumerator PhoneApps()
        {
            phone.Raise(true);
            yield return Shot("phone_home", 1.0f);
            phone.OpenApp<TalkApp>();
            yield return Shot("talk_list");
            ClickFirst(phone.Top.Root, "list-row", 0);
            yield return Shot("talk_family");
            phone.OpenApp<ContactsApp>();
            yield return Shot("contacts");
            phone.OpenApp<BankApp>();
            yield return Shot("bank_home");
            phone.Push(phone.App<BankApp>().CreateTransfer("110-900-551207", 1200000));
            yield return new WaitForSeconds(0.4f);
            ClickChip(phone.Top.Root, "Nuri Bank");
            yield return Shot("bank_transfer");
            ClickButton(phone.Top.Root, "Next");
            yield return Shot("bank_recipient_check");
            Check(HasText(phone.Top.Root, "JEONG MIRAN"), "the transfer shows the account holder's name");
            phone.OpenApp<CheckFirstApp>();
            yield return new WaitForSeconds(0.3f);
            SetField(phone.Top.Root, "070-8844-2019");
            ClickButton(phone.Top.Root, "Check");
            yield return Shot("checkfirst_result");
            phone.OpenApp<BrowserApp>();
            yield return new WaitForSeconds(0.3f);
            SetField(phone.Top.Root, "nuri bank");
            ClickButton(phone.Top.Root, "Go");
            yield return Shot("browser_results");
            ClickFirst(phone.Top.Root, "list-row", 0);
            yield return Shot("browser_page");
            phone.OpenApp<MessagesApp>();
            yield return Shot("messages_list");
            ClickFirst(phone.Top.Root, "list-row", 2);
            yield return Shot("messages_smishing");
            phone.OpenUrl("nuri-secure.kr/verify");
            yield return Shot("browser_fake_site");
            phone.Push(phone.App<CallsApp>().CreateKeypad("1599-0000"));
            yield return Shot("keypad");
            phone.OpenApp<ParcelsApp>();
            yield return Shot("parcels");
            phone.OpenApp<MailApp>();
            yield return Shot("mail");
            phone.GoHome();
            phone.Raise(false);
            yield return new WaitForSeconds(0.6f);
        }

        // ---------------------------------------------------------------- calls

        IEnumerator Ring()
        {
            director.StartCall(demo != null ? demo.DemoCall : null);
            yield return new WaitForSeconds(1.2f);
            Check(phone.Top is IncomingCallView, "the call rings on the raised phone");
        }

        IEnumerator Answer()
        {
            (phone.Top as IncomingCallView)?.Slider.Complete();
            yield return new WaitForSeconds(0.5f);
            Check(phone.Top is InCallView, "sliding answers the call");
        }

        IEnumerator CloseEnding(string name, string verdict)
        {
            yield return WaitFor(EndingShown, 30f, "the ending card appears");
            Check(HasText(root, verdict), $"the ending says '{verdict}'");
            // Both stamps have landed after about a second.
            yield return Shot(name, 1.4f);
            ClickButton(root, "Continue");
            yield return WaitFor(() => !director.IsBusy, 5f, "Continue closes the case");
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>Answer, listen, choose, ask while he holds, look away, then hang up while he pushes.</summary>
        IEnumerator CallHangUp()
        {
            yield return Ring();
            yield return Shot("call_incoming", 0.1f);
            yield return Answer();
            yield return Shot("call_answered", 0.8f);
            yield return WaitFor(() => ui.Transcript.HasDecision, 25f, "the first decision appears");
            yield return Shot("call_first_decision", 0.5f);
            ui.Transcript.Pick(1);
            yield return WaitFor(() => director.OnHold && ui.Transcript.HasQuestions, 40f, "the caller holds the line with questions");
            yield return Shot("call_hold", 0.5f);
            ui.Transcript.PickQuestion(1);
            yield return Shot("call_question", 1.5f);
            yield return WaitFor(() => ui.Transcript.HasQuestions, 30f, "the answer ends and the questions return");
            yield return Shot("call_answered_question", 0.3f);
            ui.OpenNotebook(true);
            yield return Shot("notebook_case", 0.8f);
            ui.ClosePanel();
            phone.Raise(false);
            yield return Shot("call_hud_phone_down", 1.0f);
            phone.Raise(true);
            director.Say(ConvLine.Caller("Miss Kim? Are you still there? Please don't put me on hold for long."));
            yield return Shot("call_pressure_line", 3f);
            yield return Shot("verdict_choices", 0.1f);
            ClickFirst(root, "verdict-btn--red", 0);
            yield return Shot("verdict_hang_up_confirm", 0.5f);
            ClickFirst(root, "verdict__go", 0);
            yield return CloseEnding("call_ending_hangup", "You refused");
        }

        /// <summary>Choose "Send the money" in the verdict: the confirmation shows who gets it.</summary>
        IEnumerator CallTransfer()
        {
            yield return Ring();
            yield return Answer();
            yield return WaitFor(() => ui.Transcript.HasDecision, 25f, "the first decision appears");
            ui.Transcript.Pick(0);
            yield return WaitFor(() => director.OnHold, 40f, "the caller holds the line");
            // The bank app only checks during a case: its Send button is off.
            phone.OpenApp<BankApp>();
            yield return new WaitForSeconds(0.3f);
            phone.Push(phone.App<BankApp>().CreateTransfer("110-900-551207", 1200000));
            yield return new WaitForSeconds(0.4f);
            ClickChip(phone.Top.Root, "Nuri Bank");
            ClickButton(phone.Top.Root, "Next");
            yield return Shot("bank_send_locked", 0.6f);
            phone.GoHome();
            ClickFirst(root, "verdict-btn--gold", 0);
            yield return Shot("verdict_send_confirm", 0.6f);
            ClickFirst(root, "verdict__go", 0);
            yield return Shot("verdict_sent", 1.5f);
            yield return CloseEnding("call_ending_transfer", "You went along");
        }

        // ---------------------------------------------------------------- chat

        IEnumerator Chat()
        {
            var chat = demo != null ? demo.DemoChat : null;
            director.StartChat(chat);
            Check(director.IsBusy, "the chat starts");
            phone.Raise(true);
            phone.OpenApp<TalkApp>();
            yield return Shot("chat_list_new", 1.0f);
            phone.Push(phone.App<TalkApp>().OpenThread(chat != null ? chat.caller.chatId : "mom_new"));
            yield return WaitFor(() => HasClass(phone.Top.Root, "reply-bar__prompt"), 30f, "the chat asks for a reply");
            yield return Shot("chat_decision", 0.6f);
            ClickButtonStartingWith(phone.Top.Root, "1.");
            yield return Shot("chat_after_reply", 3f);
            // Call Mom's saved number instead of answering the new profile.
            ui.Dial("010-2231-7745");
            yield return Shot("chat_call_mom", 1.2f);
            yield return CloseEnding("chat_ending_verify", "You checked first");
            phone.GoHome();
        }

        IEnumerator OutgoingCall()
        {
            ui.Dial("1332");
            yield return Shot("outgoing_call", 2.5f);
            yield return WaitFor(() => !director.IsBusy, 30f, "the outgoing call ends");
            yield return Shot("outgoing_done", 0.8f);
            phone.Raise(false);
        }

        // ---------------------------------------------------------------- helpers

        IEnumerator Shot(string name, float wait = 0.9f)
        {
            yield return new WaitForSeconds(wait);
            step++;
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"{step:00}_{name}.png"), superSize);
            yield return null;
            yield return null;
        }

        IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Check(condition(), what);
        }

        void Check(bool ok, string what)
        {
            if (ok)
                return;
            failures++;
            Debug.LogWarning($"[UITour] FAILED: {what} (step {step})");
        }

        bool EndingShown() => root.Q(className: "ending__verdict") != null;

        static bool HasText(VisualElement root, string text)
        {
            bool found = false;
            root.Query<TextElement>().ForEach(t =>
            {
                if (!found && t.text != null && t.text.Contains(text))
                    found = true;
            });
            return found;
        }

        static bool HasClass(VisualElement root, string cls) => root.Q(className: cls) != null;

        static void Click(VisualElement e)
        {
            if (e == null)
                return;
            using (var evt = ClickEvent.GetPooled())
            {
                evt.target = e;
                e.SendEvent(evt);
            }
            if (e is Button b)
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = b;
                    b.SendEvent(submit);
                }
        }

        void ClickButton(VisualElement root, string text) => ClickButtonWhere(root, b => b.text == text, $"'{text}'");

        void ClickButtonStartingWith(VisualElement root, string prefix) =>
            ClickButtonWhere(root, b => b.text != null && b.text.StartsWith(prefix), $"'{prefix}…'");

        void ClickButtonWhere(VisualElement root, Func<Button, bool> match, string what)
        {
            Button found = null;
            root.Query<Button>().ForEach(b =>
            {
                if (found == null && match(b))
                    found = b;
            });
            Check(found != null, $"button {what} exists");
            Click(found);
        }

        void ClickFirst(VisualElement root, string cls, int index)
        {
            var list = root.Query(className: cls).ToList();
            Check(index < list.Count, $".{cls} #{index} exists");
            if (index < list.Count)
                Click(list[index]);
        }

        void ClickChip(VisualElement root, string text)
        {
            foreach (var chip in root.Query(className: "chip").ToList())
            {
                var label = chip.Q<Label>();
                if (label != null && label.text == text)
                {
                    Click(chip);
                    return;
                }
            }
            Check(false, $"chip '{text}' exists");
        }

        static void SetField(VisualElement root, string value)
        {
            var field = root.Q<TextField>();
            if (field != null)
                field.value = value;
        }
    }
}
