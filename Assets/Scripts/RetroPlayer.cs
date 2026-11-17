using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RetroPlatformer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class RetroPlayer : MonoBehaviour
    {
        [Header("One Unity unit = one 16px tile")]
        public float walkSpeed = 5.625f, runSpeed = 9.375f;
        public float walkAcceleration = 8.35f, runAcceleration = 12.52f, runBrake = 11.43f;
        public float turnMultiplier = 2f, runReleaseTime = .15f;
        [Header("Jump: units / second")]
        public float jumpSpeed = 15f, runningJumpSpeed = 18.75f;
        public float heldGravity = 28.125f, walkingJumpGravity = 26.37f, runningJumpGravity = 35.16f;
        public float releasedGravity = 98.44f, walkingFallGravity = 84.375f, runningFallGravity = 126.56f;
        public float initialFallGravity = 35.16f, maxFallSpeed = 15.9f;
        public float stompBounce = 8.5f;
        [Header("Optional modern assists (zero = NES style)")]
        public float coyoteTime = 0f, jumpBuffer = 0f;
        [Header("Appearance / collision")]
        public SpriteRenderer artwork;
        public Sprite smallSprite, bigSprite, crouchSprite;
        public float bodyWidth = .72f, smallHeight = .9f, bigHeight = 1.8f;
        public LayerMask terrainLayers = ~0;
        public float invincibilityTime = 2.5f, deathY = -4f;
        public RetroCamera scrollingCamera;
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
        bool IsTerrain(Collider2D other) => other && other != box && !other.GetComponentInParent<RetroPlayer>() && !other.GetComponentInParent<RetroWalker>() && !other.isTrigger;
        bool IsOnGround()
        {
            int count = box.Cast(Vector2.down, Filter, hits, .055f);
            for (int i = 0; i < count; i++) if (IsTerrain(hits[i].collider) && hits[i].normal.y > .55f) return true;
            return false;
        }
        bool CanStand()
        {
            Vector2 center = body.position + Vector2.up * bigHeight * .5f;
            int count = Physics2D.OverlapBox(center, new Vector2(bodyWidth - .04f, bigHeight - .06f), 0, Filter, overlaps);
            for (int i = 0; i < count; i++) if (IsTerrain(overlaps[i])) return false;
            return true;
        }
        void FixedUpdate()
        {
            if (IsDead) return;
            float dt = Time.fixedDeltaTime;
            invincible = Mathf.Max(0, invincible - dt); fireGrace = Mathf.Max(0, fireGrace - dt);
            if (body.position.y < deathY) { Die(); return; }
            Vector2 v = body.linearVelocity;
            Grounded = v.y <= .1f && IsOnGround();
            coyote = Grounded ? coyoteTime : Mathf.Max(0, coyote - dt);
            if (IsBig)
            {
                bool want = IsCrouching ? (down || !CanStand()) : (Grounded && down);
                if (want != IsCrouching) { IsCrouching = want; ApplySize(); }
            }
            bool jump = ControlEnabled && jumpQueued && (Grounded || coyote > 0);
            if (jump)
            {
                RetroAudio.Play(IsBig ? RetroAudio.Cue.JumpSuper : RetroAudio.Cue.JumpSmall);
                float speed = Mathf.Abs(v.x);
                bool fast = speed >= 5.86f, walking = speed >= 3.75f;
                v.y = fast ? runningJumpSpeed : jumpSpeed;
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
                if (!jumpHeld || v.y <= 0) released = true;
                v.y = Mathf.Max(-maxFallSpeed, v.y - (released ? fallGravity : riseGravity) * dt);
            }
            else if (Grounded) { v.y = -.15f; released = true; headConsumed = false; }
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
            var block = collision.collider.GetComponentInParent<RetroBlock>();
            var walker = collision.collider.GetComponentInParent<RetroWalker>();
            if (block && preY > 0 && !headConsumed)
            {
                foreach (var c in collision.contacts)
                    if (c.normal.y < -.5f) { headConsumed = true; released = true; body.linearVelocity = new Vector2(body.linearVelocity.x, -.5f); block.Bump(this); break; }
            }
            if (!block && !walker && preY > 0 && !headConsumed && IsTerrain(collision.collider))
            {
                foreach (var c in collision.contacts)
                    if (c.normal.y < -.5f) { headConsumed = true; RetroAudio.Play(RetroAudio.Cue.Bump); break; }
            }
            if (walker && walker.Active)
            {
                if (walker.kind != RetroWalker.Kind.Enemy) { walker.Collect(this); return; }
                bool above = false;
                foreach (var c in collision.contacts) if (c.normal.y > .45f) above = true;
                if (above && preY < -.1f && box.bounds.min.y >= walker.transform.position.y - .08f)
                { walker.Stomp(); Bounce(); }
                else Hurt();
            }
        }
        public void Bounce() { released = true; headConsumed = false; body.linearVelocity = new Vector2(body.linearVelocity.x, stompBounce); }
        public void Grow()
        {
            if (IsDead || IsBig) return;
            IsBig = true; IsCrouching = !CanStand(); ApplySize(); PowerChanged?.Invoke(true);
        }
        public void GainFire()
        {
            if (IsDead || (RetroSession.Instance && RetroSession.Instance.Finished)) return;
            if (!IsBig) { Grow(); return; }
            if (IsFire) return;
            IsFire = true;
            fireGrace = .65f;
        }
        void OnTriggerEnter2D(Collider2D other) => CollectFlower(other);
        void OnTriggerStay2D(Collider2D other) => CollectFlower(other);
        void CollectFlower(Collider2D other)
        {
            var item = other.GetComponentInParent<RetroWalker>();
            if (item && item.kind == RetroWalker.Kind.Flower) item.Collect(this);
        }
        public void Hurt()
        {
            if (IsDead || invincible > 0 || fireGrace > 0 || (RetroSession.Instance && RetroSession.Instance.Finished)) return;
            if (IsBig) { RetroAudio.Play(RetroAudio.Cue.Pipe); IsFire = false; IsBig = false; IsCrouching = false; ApplySize(); invincible = invincibilityTime; PowerChanged?.Invoke(false); }
            else Die();
        }
        public void Die()
        {
            if (IsDead || (RetroSession.Instance && RetroSession.Instance.Finished)) return;
            IsFire = false; IsDead = true; ControlEnabled = false; box.enabled = false;
            if (artwork) artwork.enabled = true;
            body.gravityScale = 3; body.linearVelocity = new Vector2(0, 10);
            Died?.Invoke();
            if (RetroSession.Instance) RetroSession.Instance.RestartAfterDeath();
            else RetroAudio.Death(false);
        }
        void ApplySize()
        {
            float h = IsBig && !IsCrouching ? bigHeight : smallHeight;
            box.size = new Vector2(bodyWidth, h); box.offset = new Vector2(0, h * .5f);
            if (!artwork) return;
            Sprite sprite = IsBig ? (IsCrouching && crouchSprite ? crouchSprite : bigSprite) : smallSprite;
            if (sprite) artwork.sprite = sprite;
            if (artwork.transform != transform) artwork.transform.localPosition = Vector3.up * h * .5f;
            if (artwork.sprite && artwork.transform != transform)
                artwork.transform.localScale = new Vector3(1f / artwork.sprite.bounds.size.x, h / artwork.sprite.bounds.size.y, 1);
        }
    }
}
