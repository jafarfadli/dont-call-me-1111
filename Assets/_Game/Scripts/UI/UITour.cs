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
    /// Development tool: walks through every panel and app, then plays the day's call twice (hang
    /// up, then send the money), saving a screenshot of each step to Temp/UITour and logging every
    /// check that fails. Start it from Tools → Don't Call Me → UI → Capture UI Tour.
    /// </summary>
    public class UITour : MonoBehaviour
    {
        public string outputDir = "Temp/UITour";
        public int superSize = 2;
        public bool done;
        public int failures;

        UIManager ui;
        CallDirector director;
        ConversationData call;
        PhoneController phone;
        VisualElement root;
        int step;

        public void Begin()
        {
            ui = FindAnyObjectByType<UIManager>();
            director = FindAnyObjectByType<CallDirector>();
            // The tour drives the call itself: stop the day flow, keep its conversation.
            var day = FindAnyObjectByType<DayDirector>();
            call = day != null && day.Plan != null ? day.Plan.Conversation : null;
            day?.Cancel();
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
            ui.OpenPanel(PanelId.Calendar);
            yield return Shot("calendar");
            ui.OpenPanel(PanelId.Computer);
            yield return Shot("computer");
            ClickButton(ui.OpenPanelView.Root, "Check a bank account");
            SetField(ui.OpenPanelView.Root, "110-900-551207");
            ClickButton(ui.OpenPanelView.Root, "Check");
            yield return Shot("computer_account");
            Check(HasText(ui.OpenPanelView.Root, "JEONG MIRAN"), "the account lookup shows the holder's name");
            ClickButton(ui.OpenPanelView.Root, "Check a phone number");
            SetField(ui.OpenPanelView.Root, "070-8844-2019");
            ClickButton(ui.OpenPanelView.Root, "Check");
            yield return Shot("computer_number");
            Check(HasText(ui.OpenPanelView.Root, "7 reports"), "the number lookup shows its reports");
            ui.ClosePanel();
            Check(ui.OpenPanelView == null, "panel closes");
        }

        // ---------------------------------------------------------------- phone

        IEnumerator PhoneApps()
        {
            phone.Raise(true);
            yield return Shot("phone_home", 1.0f);
            phone.OpenApp<ChatsApp>();
            yield return Shot("chats_list");
            phone.Push(phone.App<ChatsApp>().OpenThread("family"));
            yield return Shot("chats_family");
            phone.OpenApp<ContactsApp>();
            yield return Shot("contacts");
            ClickFirst(phone.Top.Root, "list-row", 0);
            yield return Shot("contact_detail");
            phone.OpenApp<BankApp>();
            yield return Shot("bank");
            phone.GoHome();
            phone.Raise(false);
            yield return new WaitForSeconds(0.6f);
        }

        // ---------------------------------------------------------------- calls

        IEnumerator Ring()
        {
            director.StartCall(call);
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
            yield return Shot(name, 1.4f);
            ClickButton(root, "Continue");
            yield return WaitFor(() => !director.IsBusy, 5f, "Continue closes the case");
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>Answer, listen, choose, ask while he holds, look things up, then hang up.</summary>
        IEnumerator CallHangUp()
        {
            yield return Ring();
            yield return Shot("call_incoming", 0.1f);
            yield return Answer();
            yield return Shot("call_answered", 0.8f);
            yield return WaitFor(() => ui.Transcript.HasDecision, 25f, "the first decision appears");
            yield return Shot("call_first_decision", 0.5f);
            ui.Transcript.Pick(1);
            yield return WaitFor(() => director.OnHold && ui.Transcript.HasQuestions, 90f, "the caller holds the line with questions");
            yield return Shot("call_hold", 0.5f);
            ui.Transcript.PickQuestion(1);
            yield return Shot("call_question", 1.5f);
            yield return WaitFor(() => ui.Transcript.HasQuestions, 30f, "the answer ends and the questions return");
            phone.Raise(false);
            yield return Shot("call_hud_phone_down", 1.0f);
            // The computer offers the caller's number and the account he gave as chips.
            ui.OpenPanel(PanelId.Computer);
            yield return new WaitForSeconds(0.4f);
            ClickFirst(ui.OpenPanelView.Root, "chip", 0);
            yield return Shot("computer_during_call", 0.6f);
            ui.ClosePanel();
            phone.Raise(true);
            director.Say(ConvLine.Caller("Miss Kim? Are you still there? Please don't put me on hold for long."));
            yield return Shot("call_pressure_line", 3f);
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
            yield return WaitFor(() => director.OnHold, 90f, "the caller holds the line");
            ClickFirst(root, "verdict-btn--gold", 0);
            yield return Shot("verdict_send_confirm", 0.6f);
            Check(HasText(root, "JEONG MIRAN"), "the send step shows who gets the money");
            ClickFirst(root, "verdict__go", 0);
            yield return Shot("verdict_sent", 1.5f);
            yield return CloseEnding("call_ending_transfer", "You went along");
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

        static void SetField(VisualElement root, string value)
        {
            var field = root.Q<TextField>();
            if (field != null)
                field.value = value;
        }
    }
}
