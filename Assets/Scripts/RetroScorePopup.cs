using UnityEngine;
using UnityEngine.UI;

namespace RetroPlatformer
{
    // World-space visual only. It never changes the score.
    [DisallowMultipleComponent]
    public sealed class RetroScorePopup : MonoBehaviour
    {
        public Text label;
        [Min(0.01f)] public float lifetime = .8f;
        [Min(0)] public float riseSpeed = 3.75f;
        public bool snapToPixelGrid = true;
        [Min(1)] public int pixelsPerUnit = 16;
        Vector3 origin;
        float elapsed, speedMultiplier;
        bool automatic, initialized;

        public void Initialize(string value, bool floatAutomatically, float multiplier = 1f)
        {
            if (!label) label = GetComponentInChildren<Text>(true);
            if (label) label.text = value;
            origin = transform.position;
            elapsed = 0f;
            automatic = floatAutomatically;
            speedMultiplier = Mathf.Max(0, multiplier);
            initialized = true;
        }

        void LateUpdate()
        {
            if (!initialized || !automatic) return;
            elapsed += Time.deltaTime;
            if (elapsed >= Mathf.Max(.01f, lifetime)) { Destroy(gameObject); return; }
            Vector3 position = origin + Vector3.up * (elapsed * riseSpeed * speedMultiplier);
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
