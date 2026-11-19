using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RetroPlatformer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroPlayer")]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Horizontal Movement")]
        public float walkSpeed = 5.625f;
        public float runSpeed = 9.375f;
        public float walkAcceleration = 8.35f;
        public float runAcceleration = 12.52f;
        public float runBrake = 11.43f;
        public float turnMultiplier = 2f;
        public float runReleaseTime = .15f;
        [Header("Jump and Gravity")]
        public float jumpSpeed = 15f;
        public float runningJumpSpeed = 18.75f;
        public float heldGravity = 28.125f;
        public float walkingJumpGravity = 26.37f;
        public float runningJumpGravity = 35.16f;
        public float releasedGravity = 98.44f;
        public float walkingFallGravity = 84.375f;
        public float runningFallGravity = 126.56f;
        public float initialFallGravity = 35.16f;
        public float maxFallSpeed = 15.9f;
        public float stompBounce = 8.5f;
        [Header("Input Assists")]
        public float coyoteTime = 0f;
        public float jumpBuffer = 0f;
        [Header("Sprites")]
        public SpriteRenderer artwork;
        public Sprite smallSprite;
        public Sprite bigSprite;
        public Sprite crouchSprite;
        [Header("Collision")]
        public float bodyWidth = .72f;
        public float smallHeight = .9f;
        public float bigHeight = 1.8f;
        public LayerMask terrainLayers = ~0;
        [Header("Damage and Death")]
        public float invincibilityTime = 2.5f;
        public float deathY = -4f;
        [Tooltip("Trigger boxes where Mario may safely go below Death Y. Empty on normal levels.")]
        public BoxCollider2D[] safeDropZones = new BoxCollider2D[0];
        [Header("Gravity Inversion")]
        [Tooltip("Death height when falling upward. Place above the highest ceiling.")]
        public float invertedDeathY = 40f;
        public bool IsInverted { get; private set; }
        public int UpSign => IsInverted ? -1 : 1;
        [Header("Camera")]
        public CameraFollow scrollingCamera;
        public bool IsBig { get; private set; }
        public bool IsFire { get; private set; }
        public bool Grounded { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSkidding { get; private set; }
        public bool ControlEnabled { get; set; } = true;
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        public event System.Action<bool> PowerChanged;
        public event System.Action Died;
        Rigidbody2D body;
        BoxCollider2D box;
        float axis, buffer, coyote, runTimer, invincible, riseGravity, fallGravity, preY, fireGrace;
        bool jumpHeld, jumpQueued, running, down, released, headConsumed;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        readonly Collider2D[] overlaps = new Collider2D[16];
        ContactFilter2D Filter => new ContactFilter2D { useLayerMask = true, layerMask = terrainLayers, useTriggers = false };

        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            body.gravityScale = 0; body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.linearDamping = 0;
            riseGravity = heldGravity; fallGravity = initialFallGravity;
            if (!artwork) artwork = GetComponentInChildren<SpriteRenderer>();
            ApplySize();
        }
        void Update()
        {
            if (IsDead) return;
            bool left = false, right = false, press = false, hold = false, run = false, crouch = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                left = k.aKey.isPressed || k.leftArrowKey.isPressed;
                right = k.dKey.isPressed || k.rightArrowKey.isPressed;
                press = k.spaceKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame;
                hold = k.spaceKey.isPressed || k.zKey.isPressed;
                run = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed || k.xKey.isPressed;
                crouch = k.sKey.isPressed || k.downArrowKey.isPressed;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                left |= pad.dpad.left.isPressed || pad.leftStick.x.ReadValue() < -.4f;
                right |= pad.dpad.right.isPressed || pad.leftStick.x.ReadValue() > .4f;
                press |= pad.buttonSouth.wasPressedThisFrame; hold |= pad.buttonSouth.isPressed;
                run |= pad.buttonWest.isPressed; crouch |= pad.dpad.down.isPressed || pad.leftStick.y.ReadValue() < -.4f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            press = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z);
            hold = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Z);
            run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.X);
            crouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
#endif
            axis = ControlEnabled ? (right ? 1f : 0f) - (left ? 1f : 0f) : 0;
            jumpHeld = ControlEnabled && hold; running = ControlEnabled && run; down = ControlEnabled && crouch;
            if (ControlEnabled && press) { jumpQueued = true; buffer = jumpBuffer; }
            if (artwork) artwork.enabled = invincible <= 0 || Mathf.FloorToInt(invincible * 16) % 2 == 0;
        }
        bool IsTerrain(Collider2D other) => other && other != box && !other.GetComponentInParent<PlayerMovement>() && !other.GetComponentInParent<Walker>() && !other.isTrigger;
        bool IsOnGround()
        {
            int count = box.Cast(Vector2.down * UpSign, Filter, hits, .055f);
            for (int i = 0; i < count; i++) if (IsTerrain(hits[i].collider) && hits[i].normal.y * UpSign > .55f) return true;
            return false;
        }
        bool CanStand()
        {
            Vector2 center = body.position + Vector2.up * (UpSign * bigHeight * .5f);
            int count = Physics2D.OverlapBox(center, new Vector2(bodyWidth - .04f, bigHeight - .06f), 0, Filter, overlaps);
            for (int i = 0; i < count; i++) if (IsTerrain(overlaps[i])) return false;
            return true;
        }
        public bool IsInSafeDropZone(Vector2 point)
        {
            if (safeDropZones == null) return false;
            foreach (var zone in safeDropZones)
            {
                if (zone && zone.enabled && zone.gameObject.activeInHierarchy && zone.isTrigger && zone.OverlapPoint(point))
                    return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            if (IsDead) return;
            float dt = Time.fixedDeltaTime;
            invincible = Mathf.Max(0, invincible - dt); fireGrace = Mathf.Max(0, fireGrace - dt);
            if ((body.position.y < deathY && !IsInSafeDropZone(body.position)) || (IsInverted && body.position.y > invertedDeathY)) { Die(); return; }
            Vector2 v = body.linearVelocity;
            Grounded = v.y * UpSign <= .1f && IsOnGround();
            coyote = Grounded ? coyoteTime : Mathf.Max(0, coyote - dt);
            if (IsBig)
            {
                bool want = IsCrouching ? (down || !CanStand()) : (Grounded && down);
                if (want != IsCrouching) { IsCrouching = want; ApplySize(); }
            }
            bool jump = ControlEnabled && jumpQueued && (Grounded || coyote > 0);
            if (jump)
            {
                AudioManager.Play(IsBig ? AudioManager.Cue.JumpSuper : AudioManager.Cue.JumpSmall);
                float speed = Mathf.Abs(v.x);
                bool fast = speed >= 5.86f, walking = speed >= 3.75f;
                v.y = UpSign * (fast ? runningJumpSpeed : jumpSpeed);
                riseGravity = fast ? runningJumpGravity : walking ? walkingJumpGravity : heldGravity;
                fallGravity = fast ? runningFallGravity : walking ? walkingFallGravity : releasedGravity;
                Grounded = false; coyote = 0; released = !jumpHeld; headConsumed = false;
                jumpQueued = false;
            }
            else if (jumpQueued) { buffer -= dt; if (buffer <= 0) jumpQueued = false; }
            if (!ControlEnabled) { jumpQueued = false; v.x = 0; }
            else
            {
                float input = Grounded && IsCrouching ? 0 : axis;
                if (Grounded && input != 0) Facing = input > 0 ? 1 : -1;
                if (Grounded && running && input != 0) runTimer = runReleaseTime;
                else runTimer = Mathf.Max(0, runTimer - dt);
                bool fast = Grounded ? runTimer > 0 : Mathf.Abs(v.x) >= 5.86f;
                float cap = fast ? runSpeed : walkSpeed;
                float accel = fast ? runAcceleration : Mathf.Abs(v.x) > walkSpeed ? runBrake : walkAcceleration;
                IsSkidding = Grounded && input != 0 && Mathf.Abs(v.x) > 1f && Mathf.Sign(v.x) != Mathf.Sign(input);
                if (input != 0)
                    v.x = Mathf.MoveTowards(v.x, input * cap, accel * (IsSkidding ? turnMultiplier : 1f) * dt);
                else if (Grounded) v.x = Mathf.MoveTowards(v.x, 0, accel * dt);
            }
            if (!Grounded && !jump)
            {
                if (!jumpHeld || v.y * UpSign <= 0) released = true;
                v.y = UpSign * Mathf.Max(-maxFallSpeed, v.y * UpSign - (released ? fallGravity : riseGravity) * dt);
            }
            else if (Grounded) { v.y = -.15f * UpSign; released = true; headConsumed = false; }
            if (scrollingCamera && scrollingCamera.oneWay)
            {
                float min = scrollingCamera.LeftEdge + bodyWidth * .5f;
                if (body.position.x < min) body.position = new Vector2(min, body.position.y);
                if (body.position.x + v.x * dt < min) v.x = Mathf.Max(0, v.x);
            }
            if (artwork) artwork.flipX = Facing < 0;
            preY = v.y; body.linearVelocity = v;
        }
        void OnCollisionEnter2D(Collision2D collision) => HandleCollision(collision);
        void OnCollisionStay2D(Collision2D collision) => HandleCollision(collision);
        void HandleCollision(Collision2D collision)
        {
            if (IsDead) return;
            var block = collision.collider.GetComponentInParent<Block>();
            var walker = collision.collider.GetComponentInParent<Walker>();
            if (block && preY * UpSign > 0 && !headConsumed)
            {
                foreach (var c in collision.contacts)
                    if (c.normal.y * UpSign < -.5f) { headConsumed = true; released = true; body.linearVelocity = new Vector2(body.linearVelocity.x, -.5f * UpSign); block.Bump(this); break; }
            }
            if (!block && !walker && preY * UpSign > 0 && !headConsumed && IsTerrain(collision.collider))
            {
                foreach (var c in collision.contacts)
                    if (c.normal.y * UpSign < -.5f) { headConsumed = true; AudioManager.Play(AudioManager.Cue.Bump); break; }
            }
            if (walker && walker.Active)
            {
                if (walker.kind != Walker.Kind.Enemy) { walker.Collect(this); return; }
                bool above = false;
                foreach (var c in collision.contacts) if (c.normal.y * UpSign > .45f) above = true;
                if (above && preY * UpSign < -.1f && (UpSign > 0 ? box.bounds.min.y >= walker.transform.position.y - .08f : box.bounds.max.y <= walker.transform.position.y + .08f))
                { walker.Stomp(); Bounce(); }
                else Hurt();
            }
        }
        public void Bounce() { released = true; headConsumed = false; body.linearVelocity = new Vector2(body.linearVelocity.x, stompBounce * UpSign); }
        public void InvertGravity()
        {
            if (IsDead || IsInverted || !ControlEnabled) return;
            // Move the feet to the old head position so the mirrored collider occupies the same space.
            float height = box.size.y;
            body.position += Vector2.up * height;
            transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
            IsInverted = true; Grounded = false; coyote = 0; jumpQueued = false;
            released = true; headConsumed = false;
            riseGravity = heldGravity; fallGravity = initialFallGravity;
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0);
            ApplySize();
        }
        public void Grow()
        {
            if (IsDead || IsBig) return;
            IsBig = true; IsCrouching = !CanStand(); ApplySize(); PowerChanged?.Invoke(true);
        }
        public void GainFire()
        {
            if (IsDead || (GameManager.Instance && GameManager.Instance.Finished)) return;
            if (!IsBig) { Grow(); return; }
            if (IsFire) return;
            IsFire = true;
            fireGrace = .65f;
        }
        void OnTriggerEnter2D(Collider2D other) => CollectFlower(other);
        void OnTriggerStay2D(Collider2D other) => CollectFlower(other);
        void CollectFlower(Collider2D other)
        {
            var item = other.GetComponentInParent<Walker>();
            if (item && (item.kind == Walker.Kind.Flower || item.kind == Walker.Kind.GravityMushroom)) item.Collect(this);
        }
        public void Hurt()
        {
            if (IsDead || invincible > 0 || fireGrace > 0 || (GameManager.Instance && GameManager.Instance.Finished)) return;
            if (IsBig) { AudioManager.Play(AudioManager.Cue.Pipe); IsFire = false; IsBig = false; IsCrouching = false; ApplySize(); invincible = invincibilityTime; PowerChanged?.Invoke(false); }
            else Die();
        }
        public void Die()
        {
            if (IsDead || (GameManager.Instance && GameManager.Instance.Finished)) return;
            IsFire = false; IsDead = true; ControlEnabled = false; box.enabled = false;
            if (artwork) artwork.enabled = true;
            body.gravityScale = 3 * UpSign; body.linearVelocity = new Vector2(0, 10 * UpSign);
            Died?.Invoke();
            if (GameManager.Instance) GameManager.Instance.RestartAfterDeath();
            else AudioManager.Death(false);
        }
        void ApplySize()
        {
            float h = IsBig && !IsCrouching ? bigHeight : smallHeight;
            box.size = new Vector2(bodyWidth, h); box.offset = new Vector2(0, h * .5f * UpSign);
            if (!artwork) return;
            Sprite sprite = IsBig ? (IsCrouching && crouchSprite ? crouchSprite : bigSprite) : smallSprite;
            if (sprite) artwork.sprite = sprite;
            if (artwork.transform != transform) artwork.transform.localPosition = Vector3.up * (h * .5f * UpSign);
            if (artwork.sprite && artwork.transform != transform)
                artwork.transform.localScale = new Vector3(1f / artwork.sprite.bounds.size.x, h / artwork.sprite.bounds.size.y * UpSign, 1);
        }
    }
}
