using UnityEngine;
using UnityEngine.UI;

namespace RetroPlatformer
{
    public class RetroHudCanvas : MonoBehaviour
    {
        public RetroSession session;
        public Text scoreText;
        public Text coinsLivesText;
        public Text worldText;
        public Text timeText;
        public Text centerMessageText;

        void Reset()
        {
            if (!session) session = FindFirstObjectByType<RetroSession>();
        }

        void LateUpdate()
        {
            if (!session) session = FindFirstObjectByType<RetroSession>();
            if (!session) return;

            if (scoreText) scoreText.text = $"SCORE\n{session.Score:000000}";
            if (coinsLivesText) coinsLivesText.text = $"COINS {session.Coins:00}\nLIVES {session.Lives}";
            if (worldText) worldText.text = $"WORLD\n{session.worldLabel}";
            if (timeText) timeText.text = $"TIME\n{session.TimeLeftRounded:000}";

            if (centerMessageText)
            {
                if (session.Finished && !session.CastleEnding) centerMessageText.text = "LEVEL COMPLETE";
                else if (session.GameOverPending) centerMessageText.text = "GAME OVER";
                else centerMessageText.text = "";
            }
        }
    }
}
