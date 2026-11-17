using System.Collections;
using UnityEngine;

namespace RetroPlatformer
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroScorePopups")]
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("Prefab")]
        public ScorePopup popupPrefab;
        [Header("Display Rules")]
        public bool enemyPopups = true;
        public bool powerUpPopups = true;
        public bool blockCoinPopups = true;
        public bool looseCoinPopups = false;
        public bool brickPopups = false;
        public bool flagpolePopups = true;
        [Header("Coin Popup Animation")]
        [Min(0)] public float blockCoinRiseMultiplier = .5f;
        [Header("Flagpole label position")]
        [Tooltip("Offset in world units. Applied to the visual only.")]
        public Vector3 flagpoleLabelOffset = new Vector3(.4f, 0f, 0f);
        static ScoreManager Current => GameManager.Instance ? GameManager.Instance.GetComponent<ScoreManager>() : null;

        public ScorePopup Show(string value, Vector3 position, bool automatic = true, Transform parent = null, float riseMultiplier = 1f)
        {
            if (!popupPrefab) return null;
            var popup = Instantiate(popupPrefab, position, Quaternion.identity);
            popup.name = automatic ? "Score Popup (" + value + ")" : "Flagpole Bonus";
            if (parent) popup.transform.SetParent(parent, true);
            popup.Initialize(value, automatic, riseMultiplier);
            return popup;
        }
        public static void Enemy(int points, Vector3 pos)
        { var m = Current; if (m && m.enemyPopups) m.Show(points.ToString(), pos); }
        public static void PowerUp(string value, Vector3 pos)
        { var m = Current; if (m && m.powerUpPopups) m.Show(value, pos); }
        public static void BlockCoin(Vector3 pos, float delay)
        { var m = Current; if (m && m.blockCoinPopups) m.StartCoroutine(m.Delayed("200", pos, delay)); }
        public static void LooseCoin(Vector3 pos)
        { var m = Current; if (m && m.looseCoinPopups) m.Show("200", pos); }
        public static void Brick(Vector3 pos)
        { var m = Current; if (m && m.brickPopups) m.Show("50", pos); }
        public static ScorePopup Pole(int points, Vector3 pos, Transform parent)
        { var m = Current; return m && m.flagpolePopups ? m.Show(points.ToString(), pos, false, parent) : null; }
        public static Vector3 PoleOffset { get { var m = Current; return m ? m.flagpoleLabelOffset : Vector3.zero; } }
        IEnumerator Delayed(string value, Vector3 pos, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            Show(value, pos, riseMultiplier: blockCoinRiseMultiplier);
        }
    }
}
