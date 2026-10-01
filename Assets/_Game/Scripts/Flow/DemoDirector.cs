using DontCallMe.Data;
using DontCallMe.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontCallMe.Flow
{
    /// <summary>
    /// Development shortcuts for trying conversations outside the day flow (editor and development
    /// builds only): F2 starts the demo call and F3 the demo chat. Optionally the demo call rings a few
    /// seconds after the newspaper is first closed.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        [SerializeField] UIManager ui;
        [SerializeField] CallDirector director;
        [SerializeField] ConversationData demoCall;
        [SerializeField] ConversationData demoChat;
        [SerializeField] bool ringAfterNewspaper;
        [SerializeField] float delayAfterNewspaper = 5f;

        bool rang;
        float countdown = -1f;
        DayDirector day;

        public ConversationData DemoCall => demoCall;
        public ConversationData DemoChat => demoChat;

        void Awake()
        {
            if (ui == null)
                ui = FindAnyObjectByType<UIManager>();
            if (director == null)
                director = FindAnyObjectByType<CallDirector>();
            day = FindAnyObjectByType<DayDirector>();
        }

        /// <summary>A running day owns the phone; the shortcuts would break its flow.</summary>
        bool DayRunning => day != null && day.isActiveAndEnabled &&
                           day.Current != DayDirector.Phase.Waiting && day.Current != DayDirector.Phase.Done;

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
            if (ringAfterNewspaper)
                ui?.Toast("ic_star", "UI template demo", "Read the newspaper on the desk. F2: demo call · F3: demo chat");
        }

        void OnPanelClosed(PanelId id)
        {
            if (ringAfterNewspaper && !rang && id == PanelId.Newspaper)
                countdown = delayAfterNewspaper;
        }

        void Update()
        {
            if (!Debug.isDebugBuild || DayRunning)
                return;
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
