using UnityEngine;

namespace RetroPlatformer
{
    // Presentation only: PlayerMovement still owns movement, physics and power state.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    [DefaultExecutionOrder(100)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroMarioAnimation")]
    public sealed class MarioAnimation : MonoBehaviour
    {
        [Header("Small Mario")]
        public Sprite smallIdle;
        public Sprite smallJump;
        public Sprite smallSkid;
        public Sprite smallDeath;
        public Sprite[] smallWalk = new Sprite[3];
        [Header("Super Mario")]
        public Sprite bigIdle;
        public Sprite bigJump;
        public Sprite bigSkid;
        public Sprite bigCrouch;
        public Sprite[] bigWalk = new Sprite[3];
        [Header("Power-up animation")]
        public Sprite growMiddle;
        [Min(0f)] public float growthDuration = .48f;
        [Min(0f)] public float shrinkDuration = .32f;
        [Min(1f)] public float transformationFPS = 18f;
        [Header("Walking animation")]
        [Min(0f)] public float idleSpeed = .12f;
        [Min(0f)] public float minimumWalkFPS = 4f;
        [Min(0f)] public float framesPerUnit = 1.35f;
        [Min(1f)] public float maxWalkFPS = 15f;
        public bool startWithThirdFrame = true;
        [Header("Sprite Placement")]
        public bool nativePixelSize = true;
        public Vector2 visualOffset;

        PlayerMovement player;
        float walkPhase, changeTimer, changeDuration;
        bool walking, growing;

        void Awake() { player = GetComponent<PlayerMovement>(); }
        void OnEnable() { player.PowerChanged += OnPowerChanged; walking = false; changeTimer = 0; }
        void OnDisable() { if (player) player.PowerChanged -= OnPowerChanged; }
        void OnPowerChanged(bool big)
        {
            growing = big;
            changeDuration = big ? growthDuration : shrinkDuration;
            changeTimer = changeDuration;
        }
        void LateUpdate()
        {
            SpriteRenderer r = player.artwork;
            if (!r) return;
            Sprite frame;
            bool flip = player.Facing < 0;
            bool big = player.IsBig;
            float speed = Mathf.Abs(player.Velocity.x);
            if (player.IsDead)
            {
                frame = smallDeath ? smallDeath : smallIdle;
                flip = false;
                walking = false;
                changeTimer = 0;
            }
            else if (changeTimer > 0f)
            {
                int tick = Mathf.FloorToInt((changeDuration - changeTimer) * transformationFPS);
                // Alternate actual extracted small, intermediate and big poses.
                int pose = tick % 4;
                if (!growing) pose = 3 - pose;
                frame = pose == 0 ? smallIdle : pose == 2 ? bigIdle : (growMiddle ? growMiddle : bigIdle);
                changeTimer = Mathf.Max(0f, changeTimer - Time.deltaTime);
                walking = false;
            }
            else if (big && player.IsCrouching)
            {
                frame = bigCrouch;
                walking = false;
            }
            else if (!player.Grounded)
            {
                frame = big ? bigJump : smallJump;
                walking = false;
            }
            else if (player.IsSkidding && speed > idleSpeed)
            {
                frame = big ? bigSkid : smallSkid;
                // In the supplied sheet skid art already faces left.
                flip = player.Facing > 0;
                walking = false;
            }
            else if (speed > idleSpeed)
            {
                Sprite[] cycle = big ? bigWalk : smallWalk;
                if (!walking) walkPhase = startWithThirdFrame ? 2.75f : 0f;
                walking = true;
                frame = cycle != null && cycle.Length > 0 ? cycle[Mathf.FloorToInt(walkPhase) % cycle.Length] : null;
                float fps = Mathf.Clamp(speed * framesPerUnit, minimumWalkFPS, Mathf.Max(minimumWalkFPS, maxWalkFPS));
                walkPhase = Mathf.Repeat(walkPhase + Time.deltaTime * fps, cycle != null && cycle.Length > 0 ? cycle.Length : 3);
            }
            else
            {
                frame = big ? bigIdle : smallIdle;
                walking = false;
            }
            if (!frame) frame = big ? bigIdle : smallIdle;
            if (!frame) return;
            r.sprite = frame;
            r.flipX = flip;
            // Existing controller's Visual child is centered/scaled to its collider.
            // Keep the original 16px/unit proportions and anchor every pose at the feet.
            // Never reposition or rescale the actual player root.
            if (r.transform != transform && r.transform.parent == transform)
            {
                float h = big && !player.IsCrouching ? player.bigHeight : player.smallHeight;
                Vector3 scale = nativePixelSize ? Vector3.one : new Vector3(1f / frame.bounds.size.x, h / frame.bounds.size.y, 1f);
                scale.y *= player.UpSign;
                r.transform.localScale = scale;
                r.transform.localPosition = new Vector3(visualOffset.x, visualOffset.y * player.UpSign - frame.bounds.min.y * scale.y, r.transform.localPosition.z);
            }
        }
    }
}
