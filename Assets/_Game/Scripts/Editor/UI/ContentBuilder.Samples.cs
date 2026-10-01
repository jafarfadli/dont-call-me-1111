using System.Collections.Generic;
using DontCallMe.Data;
using UnityEngine;

namespace DontCallMe.Editor.UI
{
    public static partial class ContentBuilder
    {
        // ================================================================ E2: "Mom's broken phone" chat (a sample for testing chats)

        static ConversationData BuildChat()
        {
            var c = ScriptableObject.CreateInstance<ConversationData>();
            c.title = "Sample chat · Mom's broken phone (E2)";
            c.channel = Channel.Chat;
            c.isScam = true;
            c.revealAtEnd = true;
            c.caller = new CallerInfo
            {
                displayName = "엄마 ♥", number = "", inContacts = false, portrait = "pt_mom", chatId = "mom_new", profileId = "mom_hyejin72",
            };
            c.claims = new List<string>
            {
                "It's Mom, writing from a new Talk profile.",
                "Her phone screen broke; she's on the repair shop's tablet.",
                "She needs 200,000 won sent to the shop owner's account.",
                "Account: Daehan Bank 3333-12-7788901.",
            };
            c.nodes = new List<ConvNode>
            {
                Decide("start",
                    D(DecisionKind.Ask, "Reply", 90f,
                      "Mom? Why a new profile? Call me.", "no_call",
                      "Oh no! Where do I send it?", "account"),
                    Caller("Jiwoo-ya, it's Mom. My phone screen broke so I'm using the repair shop's tablet."),
                    Caller("Can you do me a favour? I need to pay for the repair and a new phone deposit, 200,000 won.")),
                Node("no_call", "account",
                    Caller("I can't call, the tablet has no phone. Just send it quickly, the shop closes soon.")),
                Decide("account",
                    D(DecisionKind.Commit, "Reply", 90f,
                      "Let me check something first.", "stall",
                      "I'm not sending money to a stranger's account.", "refuse",
                      P(0.4f, "Jiwoo-ya? Are you there?")),
                    Caller("Send it to the shop owner's account, I'll pay you back tonight: Daehan Bank 3333-12-7788901.",
                           F(FactKind.Account, "3333-12-7788901")),
                    Caller("Please hurry, dear. They won't give me the phone until it's paid.")),
                Decide("stall",
                    D(DecisionKind.Commit, "Reply", 60f,
                      "Sending it now.", "wait",
                      "I'll call your real number first.", "refuse",
                      P(0.4f, "Jiwoo-ya, please! Before 5 o'clock!")),
                    Caller("What is there to check? It's me! Please, before 5 o'clock.")),
                Decide("wait",
                    D(DecisionKind.Commit, "Send it in Nuri Bank, or stop.", 90f,
                      "Done.", "not_seen",
                      "Actually, wait.", "stall"),
                    Caller("Thank you!! Tell me when it's done.")),
                Node("not_seen", "wait",
                    Caller("The shop says nothing came in yet. Please check and send it again.", F(FactKind.Account, "3333-12-7788901"))),
            };
            c.endings = new List<ConvEnding>
            {
                new ConvEnding
                {
                    id = "refuse", verdict = Verdict.Refuse,
                    lines = new List<ConvLine> { Caller("...") },
                    consequence = "You didn't send anything. The \"엄마 ♥\" profile disappeared an hour later. Mom's usual profile had posted a photo of dinner eight minutes before the first message.",
                },
                new ConvEnding
                {
                    id = "timeout", verdict = Verdict.Refuse, timedOut = true,
                    consequence = "You never answered. The profile gave up.",
                },
                new ConvEnding
                {
                    id = "go_along", verdict = Verdict.GoAlong, moneyDelta = -200000,
                    lines = new List<ConvLine> { Caller("Thank you dear!! ♥") },
                    consequence = "The 200,000 won went to Yoon Jaehee's account. Your real mom's phone was fine all along.",
                },
                new ConvEnding
                {
                    id = "verify", verdict = Verdict.Verify,
                    lines = new List<ConvLine>
                    {
                        Caller("Jiwoo-ya! Did you eat?", "Jiwoo ya! Did you eat?"),
                        Caller("What? My phone is fine, I'm at home with your dad."),
                        Caller("Don't send anything! Block that profile."),
                    },
                    consequence = "You called Mom's saved number. Her phone was fine: the new profile was a scammer.",
                },
            };
            c.actions = new List<ActionTrigger>
            {
                new ActionTrigger { kind = ActionKind.Transfer, target = "3333-12-7788901", endingId = "go_along" },
                new ActionTrigger { kind = ActionKind.Call, target = "010-2231-7745", endingId = "verify", answeredBy = "Mom", portrait = "pt_mom", voice = VoiceMom },
            };
            c.hangUpEndingId = "refuse";
            c.timeoutEndingId = "timeout";
            return c;
        }
    }
}
