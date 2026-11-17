using UnityEngine;
namespace RetroPlatformer
{
    [RequireComponent(typeof(Camera))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroCamera")]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public PlayerMovement target;
        [Header("Scrolling")]
        public bool oneWay = true;
        public float startLeft = 0;
        public float endRight = 212;
        public float fixedY = 5.5f;
        public float scrollTriggerTiles = 5f;
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
