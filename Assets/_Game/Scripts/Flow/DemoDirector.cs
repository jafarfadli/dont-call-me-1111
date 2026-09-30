using DontCallMe.Data;
using DontCallMe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Flow
{
    /// <summary>
    /// Drives the UI templates until DayDirector exists: the demo call rings a few seconds after the
    /// newspaper is first closed (as a real day will), F2 starts the demo call and F3 the demo chat.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        [SerializeField] UIManager ui;
        [SerializeField] CallDirector director;
        [SerializeField] ConversationData demoCall;
        [SerializeField] ConversationData demoChat;
        [SerializeField] bool ringAfterNewspaper = true;
        [SerializeField] float delayAfterNewspaper = 5f;

        bool rang;
        float countdown = -1f;

        public ConversationData DemoCall => demoCall;
        public ConversationData DemoChat => demoChat;

        void Awake()
        {
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
            if (director == null)
                director = FindAnyObjectByType<CallDirector>();
        }

        void OnEnable()
        {
            if (ui != null)
                ui.PanelClosed += OnPanelClosed;
        }

        void OnDisable()
        {
            if (ui != null)
                ui.PanelClosed -= OnPanelClosed;
        }

        void Start()
        {
            ui?.Toast("ic_star", "UI template demo", "Read the newspaper on the desk. F2: demo call · F3: demo chat");
        }

        void OnPanelClosed(PanelId id)
        {
            if (ringAfterNewspaper && !rang && id == PanelId.Newspaper)
                countdown = delayAfterNewspaper;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
                Ring();
            if (kb != null && kb.f3Key.wasPressedThisFrame && director != null && !director.IsBusy)
                director.StartChat(demoChat);
            if (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0f)
                    Ring();
            }
        }

        void Ring()
        {
            if (director == null || director.IsBusy || demoCall == null)
                return;
            rang = true;
            countdown = -1f;
            director.StartCall(demoCall);
        }
    }
}
