using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
namespace RetroPlatformer
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroSession")]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [Header("Level Settings")]
        public string worldLabel = "1-1";
        public int startTime = 400;
        public int startingLives = 3;
        public float timerSpeed = 2.5f;
        [Header("Debug HUD")]
        public bool showDebugHUD = true;
        [Header("Scene References and Transition")]
        public PlayerMovement player;
        public string nextScene;
        [Header("Events")]
        public UnityEvent onLevelFinished;
        public int Score { get; private set; }
        public int Coins { get; private set; }
        public int Lives { get; private set; }
        public bool Finished { get; private set; }
        public bool Completing { get; private set; }
        public bool CastleEnding { get; private set; }
        public float TimeLeft => timeLeft;
        public int TimeLeftRounded => Mathf.CeilToInt(timeLeft);
        public bool Restarting => restarting;
        public bool GameOverPending => restarting && Lives <= 0;
        float timeLeft;
        bool restarting;
        static bool carry;
        static int savedLives, savedScore, savedCoins;
        void Awake()
        {
            Instance = this; timeLeft = startTime;
            Lives = carry ? savedLives : startingLives;
            Score = carry ? savedScore : 0; Coins = carry ? savedCoins : 0; carry = false;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Update()
        {
            if (Finished || Completing || restarting || !player || player.IsDead) return;
            timeLeft = Mathf.Max(0,timeLeft - Time.deltaTime * timerSpeed);
            if (timeLeft <= 0) player.Die();
            else AudioManager.CheckTime(timeLeft);
        }
        public void AddScore(int amount) => Score += amount;
        public void AddCoin() { AudioManager.Play(AudioManager.Cue.Coin); Coins++; AddScore(200); if (Coins >= 100) { Coins -= 100; AddLife(); } }
        public void AddLife() { Lives++; AudioManager.Play(AudioManager.Cue.ExtraLife); }
        public void RestartAfterDeath() { if (restarting || Finished || Completing) return; restarting = true; Lives--; StartCoroutine(Restart(AudioManager.Death(Lives <= 0))); }
        IEnumerator Restart(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            carry = Lives > 0; savedLives = Lives; savedScore = Score; savedCoins = Coins;
            LoadCurrent();
        }
        public bool BeginGoal(int points)
        {
            if (Finished || Completing || restarting || !player || player.IsDead) return false;
            Completing = true; CastleEnding = true; player.ControlEnabled = false;
            AddScore(points); AudioManager.BeginPole(); return true;
        }
        public int ConsumeGoalTime(int ticks)
        {
            if (!Completing || ticks <= 0) return 0;
            int left = TimeLeftRounded, n = Mathf.Min(left, ticks);
            timeLeft = left - n; AddScore(n * 50); return n;
        }
        public void CompleteGoal()
        {
            if (!Completing) return;
            ConsumeGoalTime(TimeLeftRounded);
            Completing = false; Finished = true;
            StartCoroutine(Next(0));
        }
        public void Finish()
        {
            if (Finished || Completing || restarting) return;
            Finished = true; if (player) player.ControlEnabled = false;
            AddScore(Mathf.CeilToInt(timeLeft)*50);
            StartCoroutine(Next(AudioManager.Clear()));
        }
        IEnumerator Next(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            onLevelFinished?.Invoke();
            if (string.IsNullOrWhiteSpace(nextScene)) yield break;
            carry = true; savedLives = Lives; savedScore = Score; savedCoins = Coins;
            SceneManager.LoadScene(nextScene);
        }
        void LoadCurrent()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
#if UNITY_EDITOR
            else UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            else Debug.LogError("Add this scene to the build scene list to enable restart.");
#endif
        }
        void OnGUI()
        {
            if (!showDebugHUD) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(13,Screen.height / 32), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(0,8,Screen.width*.25f,55),$"SCORE\n{Score:000000}",style);
            GUI.Label(new Rect(Screen.width*.25f,8,Screen.width*.25f,55),$"COINS {Coins:00}\nLIVES {Lives}",style);
            GUI.Label(new Rect(Screen.width*.5f,8,Screen.width*.25f,55),$"WORLD\n{worldLabel}",style);
            GUI.Label(new Rect(Screen.width*.75f,8,Screen.width*.25f,55),$"TIME\n{Mathf.CeilToInt(timeLeft):000}",style);
            if (Finished && !CastleEnding) GUI.Label(new Rect(0,Screen.height*.4f,Screen.width,60),"LEVEL COMPLETE",style);
            else if (restarting && Lives <= 0) GUI.Label(new Rect(0,Screen.height*.4f,Screen.width,60),"GAME OVER",style);
        }
    }
}
