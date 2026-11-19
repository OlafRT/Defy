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
        [Tooltip("Prevent the camera from scrolling left and Mario from going behind the visible screen.")]
        public bool oneWay = true;
        public float startLeft = 0;
        public float endRight = 212;
        public float fixedY = 5.5f;
        public float scrollTriggerTiles = 5f;
        [Header("Vertical Scrolling")]
        [Tooltip("Follow Mario down while he is inside an assigned safe drop zone.")]
        public bool followDownward = false;
        [Tooltip("Player Y at which downward scrolling starts, normally the upper ground level.")]
        public float verticalFollowStartY = 0f;
        [Tooltip("Track Mario upward after gravity inversion.")]
        public bool followUpward = false;
        [Tooltip("Camera center Y when the player reaches the ceiling. Set to ceiling surface Y minus about 5.5 tiles.")]
        public float ceilingViewY = 15f;
        public float LeftEdge => transform.position.x - cam.orthographicSize * cam.aspect;
        Camera cam;
        void Awake() { cam = GetComponent<Camera>(); SetView(startLeft, fixedY); }
        void SetView(float left, float y)
        {
            transform.position = new Vector3(left + cam.orthographicSize * cam.aspect, y, -10);
        }
        void LateUpdate()
        {
            if (!target || target.IsDead) return;
            float desired = target.transform.position.x - scrollTriggerTiles;
            float max = Mathf.Max(startLeft, endRight - 2 * cam.orthographicSize * cam.aspect);
            float y = fixedY;
            Vector3 playerPosition = target.transform.position;
            if (followDownward && target.IsInSafeDropZone(playerPosition))
                y += Mathf.Min(0f, playerPosition.y - verticalFollowStartY);
            if (followUpward && target.IsInverted)
                y = Mathf.Clamp(playerPosition.y, Mathf.Min(fixedY, ceilingViewY), Mathf.Max(fixedY, ceilingViewY));
            SetView(Mathf.Clamp(oneWay ? Mathf.Max(LeftEdge, desired) : desired, startLeft, max), y);
        }
    }
}
