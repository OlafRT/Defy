using UnityEngine;
namespace RetroPlatformer
{
    [RequireComponent(typeof(Camera))]
    public class RetroCamera : MonoBehaviour
    {
        public RetroPlayer target;
        public bool oneWay = true;
        public float startLeft = 0, endRight = 212, fixedY = 5.5f, scrollTriggerTiles = 5f;
        public float LeftEdge => transform.position.x - cam.orthographicSize * cam.aspect;
        Camera cam;
        void Awake() { cam = GetComponent<Camera>(); SetLeft(startLeft); }
        void SetLeft(float left) { transform.position = new Vector3(left + cam.orthographicSize * cam.aspect, fixedY, -10); }
        void LateUpdate()
        {
            if (!target || target.IsDead) return;
            float desired = target.transform.position.x - scrollTriggerTiles;
            float max = Mathf.Max(startLeft, endRight - 2 * cam.orthographicSize * cam.aspect);
            SetLeft(Mathf.Clamp(oneWay ? Mathf.Max(LeftEdge, desired) : desired, startLeft, max));
        }
    }
}
