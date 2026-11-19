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

        [Header("Continuous Vertical Tracking")]
        [Tooltip("Follow the player's height on both sides of the level, regardless of gravity. Overrides the conditional vertical scrolling settings above.")]
        public bool alwaysFollowVertically = false;
        [Tooltip("World Y of the ceiling's walkable underside. Vertical Follow Start Y is the normal ground surface.")]
        public float ceilingSurfaceY = 22f;
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
            if (alwaysFollowVertically)
            {
                // Interpolate between the two standing views using world height, not gravity.
                // This keeps the camera continuous when the gravity mushroom is collected.
                float height = Mathf.InverseLerp(verticalFollowStartY, ceilingSurfaceY, playerPosition.y);
                y = Mathf.Lerp(fixedY, ceilingViewY, height);
            }
            else
            {
                if (followDownward && target.IsInSafeDropZone(playerPosition))
                    y += Mathf.Min(0f, playerPosition.y - verticalFollowStartY);
                if (followUpward && target.IsInverted)
                    y = Mathf.Clamp(playerPosition.y, Mathf.Min(fixedY, ceilingViewY), Mathf.Max(fixedY, ceilingViewY));
            }
            SetView(Mathf.Clamp(oneWay ? Mathf.Max(LeftEdge, desired) : desired, startLeft, max), y);
        }
    }
}
