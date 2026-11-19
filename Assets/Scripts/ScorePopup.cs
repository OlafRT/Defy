using UnityEngine;
using UnityEngine.UI;

namespace RetroPlatformer
{
    // World-space visual only. It never changes the score.
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroScorePopup")]
    public sealed class ScorePopup : MonoBehaviour
    {
        [Header("Text Reference")]
        public Text label;
        [Header("Animation")]
        [Min(0.01f)] public float lifetime = .8f;
        [Min(0)] public float riseSpeed = 3.75f;
        [Header("Pixel Snapping")]
        public bool snapToPixelGrid = true;
        [Min(1)] public int pixelsPerUnit = 16;
        Vector3 origin;
        float elapsed, speedMultiplier;
        int upSign = 1;
        bool automatic, initialized;

        public void Initialize(string value, bool floatAutomatically, float multiplier = 1f, int up = 1)
        {
            if (!label) label = GetComponentInChildren<Text>(true);
            if (label) label.text = value;
            origin = transform.position;
            elapsed = 0f;
            automatic = floatAutomatically;
            speedMultiplier = Mathf.Max(0, multiplier);
            upSign = up < 0 ? -1 : 1;
            initialized = true;
        }

        void LateUpdate()
        {
            if (!initialized || !automatic) return;
            elapsed += Time.deltaTime;
            if (elapsed >= Mathf.Max(.01f, lifetime)) { Destroy(gameObject); return; }
            Vector3 position = origin + Vector3.up * (elapsed * riseSpeed * speedMultiplier * upSign);
            if (snapToPixelGrid)
            {
                float grid = Mathf.Max(1, pixelsPerUnit);
                position.x = Mathf.Round(position.x * grid) / grid;
                position.y = Mathf.Round(position.y * grid) / grid;
            }
            transform.position = position;
        }
    }
}
