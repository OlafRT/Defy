using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace RetroPlatformer
{
    [RequireComponent(typeof(GameManager))]
    [DisallowMultipleComponent, DefaultExecutionOrder(-1000)]
    public class LevelScreens : MonoBehaviour
    {
        [Header("Level Intro")]
        public bool showIntro = true;
        [Min(0f)] public float introDuration = 2.5f;
        public IntroHUD introHUD;

        // Kept for levels that have not yet been migrated.
        [HideInInspector] public Canvas introCanvas;
        [HideInInspector] public Text introScore;
        [HideInInspector] public Text introCoins;
        [HideInInspector] public Text introWorld;
        [HideInInspector] public Text introTitle;
        [HideInInspector] public Text introLives;

        [Header("Victory")]
        [Tooltip("Only shown if GameManager's Next Scene is empty.")]
        public bool showVictory = true;
        public string victoryTitle = "YOU WIN!";
        [TextArea(3, 8)] public string credits = "";
        public string restartScene = "Level 1";
        public bool showCursorOnVictory = true;
        public Canvas victoryCanvas;
        public Text victoryHeading;
        public Text finalScore;
        public Text victoryCredits;
        public Button restartButton;

        GameManager session;
        bool intro, victory, loading, frozen;
        bool savedAudioPause, savedCursor, cursorSaved;
        CursorLockMode savedCursorLock;
        float savedTimeScale;
        bool savedControl, controlSaved;
        PlayerMovement frozenPlayer;
        GameObject createdEventSystem;

        void Awake()
        {
            session = GetComponent<GameManager>();
            ShowCanvas(introCanvas, false);
            ShowCanvas(victoryCanvas, false);
        }

        void Start()
        {
            if (session.onLevelFinished == null) session.onLevelFinished = new UnityEvent();
            session.onLevelFinished.AddListener(OnLevelFinished);
            if (restartButton) restartButton.onClick.AddListener(RestartGame);

            // All scene Awake and OnEnable calls have now completed. This Start
            // runs before normal gameplay Start/Update because of execution order.
            if (!showIntro || introDuration <= 0f) return;
            if (!introHUD) introHUD = GetComponent<IntroHUD>();
            if (introHUD && introHUD.Begin(session))
            {
                intro = true;
                Freeze(true);
            }
            else if (introCanvas)
            {
                intro = true;
                Freeze(true);
                ShowCanvas(introCanvas, true);
            }
            else
            {
                Debug.LogWarning("LevelScreens: intro could not start. Enable IntroHUD and assign HUD, Center Message Text (on HUD), Black Background, Lives Row and Lives Text.", this);
                return;
            }
            if (introHUD) introHUD.Refresh();
            SetText(introScore, $"MARIO\n{session.Score:000000}");
            SetText(introCoins, $"×{session.Coins:00}");
            SetText(introWorld, $"WORLD\n{session.worldLabel}");
            SetText(introTitle, $"WORLD  {session.worldLabel}");
            SetText(introLives, $"×  {session.Lives}");
            StartCoroutine(IntroSequence());
        }

        IEnumerator IntroSequence()
        {
            yield return new WaitForSecondsRealtime(introDuration);
            intro = false;
            if (introHUD) introHUD.End();
            ShowCanvas(introCanvas, false);
            Unfreeze();
        }

        void OnLevelFinished()
        {
            if (victory || !showVictory || !victoryCanvas || !string.IsNullOrWhiteSpace(session.nextScene)) return;
            victory = true;
            Freeze(false);
            SetText(victoryHeading, victoryTitle);
            SetText(finalScore, $"FINAL SCORE\n{session.Score:000000}");
            SetText(victoryCredits, credits);
            ShowCanvas(victoryCanvas, true);
            if (showCursorOnVictory)
            {
                cursorSaved = true;
                savedCursor = Cursor.visible;
                savedCursorLock = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (restartButton && !FindFirstObjectByType<EventSystem>())
            {
                createdEventSystem = new GameObject("Victory Event System", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                createdEventSystem.AddComponent<InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
                createdEventSystem.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        void Update()
        {
            if (!victory || loading) return;
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
                pressed = keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            var pad = Gamepad.current;
            if (pad != null) pressed |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
#endif
            if (pressed) RestartGame();
        }

        public void RestartGame()
        {
            if (!victory || loading) return;
            if (string.IsNullOrWhiteSpace(restartScene) || !Application.CanStreamedLevelBeLoaded(restartScene))
            {
                Debug.LogError("LevelScreens: assign the first scene's exact name and add it to the build scene list.", this);
                return;
            }
            loading = true;
            RestoreCursor();
            Unfreeze();
            SceneManager.LoadScene(restartScene);
        }

        void Freeze(bool pauseAudio)
        {
            if (frozen) return;
            frozen = true;
            savedTimeScale = Time.timeScale;
            savedAudioPause = AudioListener.pause;
            frozenPlayer = session.player;
            controlSaved = frozenPlayer;
            if (controlSaved)
            {
                savedControl = frozenPlayer.ControlEnabled;
                frozenPlayer.ControlEnabled = false;
            }
            Time.timeScale = 0f;
            if (pauseAudio) AudioListener.pause = true;
        }

        void Unfreeze()
        {
            if (!frozen) return;
            frozen = false;
            Time.timeScale = savedTimeScale;
            AudioListener.pause = savedAudioPause;
            if (controlSaved && frozenPlayer) frozenPlayer.ControlEnabled = savedControl;
            controlSaved = false;
        }

        void RestoreCursor()
        {
            if (!cursorSaved) return;
            cursorSaved = false;
            Cursor.lockState = savedCursorLock;
            Cursor.visible = savedCursor;
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (introHUD) introHUD.End();
            intro = false;
            Unfreeze();
            ShowCanvas(introCanvas, false);
            ShowCanvas(victoryCanvas, false);
            RestoreCursor();
            victory = false;
            if (session && session.onLevelFinished != null) session.onLevelFinished.RemoveListener(OnLevelFinished);
            if (restartButton) restartButton.onClick.RemoveListener(RestartGame);
            if (createdEventSystem) Destroy(createdEventSystem);
        }

        static void ShowCanvas(Canvas canvas, bool visible)
        {
            if (!canvas) return;
            if (visible) canvas.enabled = true;
            canvas.gameObject.SetActive(visible);
        }

        static void SetText(Text target, string value)
        {
            if (target) target.text = value;
        }
    }
}
