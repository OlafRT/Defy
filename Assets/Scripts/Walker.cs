using System.Collections;
using UnityEngine;
namespace RetroPlatformer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroWalker")]
    public class Walker : MonoBehaviour
    {
        public enum Kind { Enemy, Mushroom, ExtraLife, Flower, GravityMushroom }
        [Header("Type")]
        public Kind kind;
        [Header("Movement")]
        public float speed = 1.875f;
        public float gravity = 3.5f;
        public float fallLimit = 12f;
        public int startDirection = -1;
        [Header("Gravity Inversion")]
        [Tooltip("Walk along ceilings instead of floors. Keep the root unrotated.")]
        public bool upsideDown;
        public float upperDespawnY = 50f;
        public int UpSign => upsideDown ? -1 : 1;
        [Header("Activation")]
        public bool waitForCamera = true;
        public float activationMargin = 1f;
        [Header("Sprites")]
        public SpriteRenderer artwork;
        public Sprite stompedSprite;
        [Header("Flower Animation (Flower Only)")]
        public Sprite[] flowerFrames;
        public float flowerFPS = 12f;
        public bool Active { get; private set; }
        Rigidbody2D body; BoxCollider2D box; int direction; bool emerging, consumed;
        Vector3 uprightArtworkPosition;
        bool uprightFlipY;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            body.freezeRotation = true; body.gravityScale = gravity * UpSign; body.linearDamping = 0;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            direction = startDirection < 0 ? -1 : 1;
            if (!artwork) artwork = GetComponentInChildren<SpriteRenderer>();
            if (artwork) { uprightArtworkPosition = artwork.transform.localPosition; uprightFlipY = artwork.flipY; }
            ApplyOrientation();
            if (kind == Kind.Flower) { body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0; box.isTrigger = true; }
            Active = !waitForCamera || kind != Kind.Enemy;
            body.simulated = Active;
        }
        public void SetGravity(bool inverted)
        {
            if (upsideDown == inverted) return;
            upsideDown = inverted;
            if (body && kind != Kind.Flower) body.gravityScale = gravity * UpSign;
            ApplyOrientation();
        }
        void ApplyOrientation()
        {
            if (!artwork) return;
            artwork.flipY = uprightFlipY ^ upsideDown;
            if (artwork.transform.parent == transform)
                artwork.transform.localPosition = new Vector3(uprightArtworkPosition.x, uprightArtworkPosition.y * UpSign, uprightArtworkPosition.z);
        }
        void LateUpdate()
        {
            if (kind == Kind.Flower && artwork && flowerFrames != null && flowerFrames.Length > 0)
                artwork.sprite = flowerFrames[Mathf.FloorToInt(Time.time * Mathf.Max(1, flowerFPS)) % flowerFrames.Length];
        }
        void FixedUpdate()
        {
            if (consumed || emerging || kind == Kind.Flower) return;
            if (!Active)
            {
                Camera cam = Camera.main;
                if (!cam || (upsideDown ? Mathf.Abs(transform.position.x - cam.transform.position.x) <= cam.orthographicSize * cam.aspect + activationMargin && Mathf.Abs(transform.position.y - cam.transform.position.y) <= cam.orthographicSize + activationMargin : transform.position.x <= cam.transform.position.x + cam.orthographicSize * cam.aspect + activationMargin))
                { Active = true; body.simulated = true; }
                else return;
            }
            var v = body.linearVelocity;
            body.linearVelocity = new Vector2(direction * speed, UpSign * Mathf.Max(-fallLimit,v.y * UpSign));
            if (upsideDown ? transform.position.y > upperDespawnY : transform.position.y < -5) Destroy(gameObject);
            if (artwork) artwork.flipX = direction > 0;
        }
        void OnCollisionEnter2D(Collision2D c) => Turn(c);
        void OnCollisionStay2D(Collision2D c) => Turn(c);
        void Turn(Collision2D c)
        {
            if (!Active || consumed || c.collider.GetComponentInParent<PlayerMovement>()) return;
            foreach (var contact in c.contacts)
                if (Mathf.Abs(contact.normal.x) > .6f && direction * contact.normal.x < 0)
                { direction = contact.normal.x > 0 ? 1 : -1; break; }
        }
        public void Emerge(float targetY)
        {
            AudioManager.Play(AudioManager.Cue.PowerupAppears);
            emerging = true; Active = false; body.simulated = false;
            if (artwork) artwork.sortingOrder = -1;
            StartCoroutine(Rise(targetY));
        }
        IEnumerator Rise(float targetY)
        {
            while (Mathf.Abs(transform.position.y - targetY) > .001f)
            { transform.position = Vector3.MoveTowards(transform.position,new Vector3(transform.position.x,targetY,transform.position.z),1.2f*Time.deltaTime); yield return null; }
            direction = kind == Kind.GravityMushroom ? (startDirection < 0 ? -1 : 1) : 1; emerging = false; Active = true; body.position = transform.position; body.simulated = true;
            if (artwork) artwork.sortingOrder = 2;
        }
        public void Collect(PlayerMovement player)
        {
            if (!Active || consumed || kind == Kind.Enemy || player.IsDead) return;
            consumed = true; Active = false; body.simulated = false;
            if (kind == Kind.Mushroom) { AudioManager.Play(AudioManager.Cue.Powerup); player.Grow(); if (GameManager.Instance) GameManager.Instance.AddScore(1000); ScoreManager.PowerUp("1000", player.transform.position + Vector3.up * ((player.IsBig ? 1.65f : .85f) * player.UpSign), player.UpSign); }
            else if (kind == Kind.GravityMushroom) { AudioManager.Play(AudioManager.Cue.Powerup); player.InvertGravity(); if (GameManager.Instance) GameManager.Instance.AddScore(1000); ScoreManager.PowerUp("1000", transform.position + Vector3.up * .5f * UpSign, UpSign); }
            else if (kind == Kind.Flower) { AudioManager.Play(AudioManager.Cue.Powerup); player.GainFire(); if (GameManager.Instance) GameManager.Instance.AddScore(1000); ScoreManager.PowerUp("1000", player.transform.position + Vector3.up * ((player.IsBig ? 1.65f : .85f) * player.UpSign), player.UpSign); }
            else { if (GameManager.Instance) GameManager.Instance.AddLife(); ScoreManager.PowerUp("1UP", player.transform.position + Vector3.up * ((player.IsBig ? 1.65f : .85f) * player.UpSign), player.UpSign); }
            Destroy(gameObject);
        }
        public void HitByFire(int travelDirection)
        {
            if (!Active || consumed || kind != Kind.Enemy) return;
            consumed = true; Active = false; body.simulated = false; box.enabled = false;
            var flipbook = GetComponent<SpriteAnimation>(); if (flipbook) flipbook.enabled = false;
            AudioManager.Play(AudioManager.Cue.Kick);
            if (GameManager.Instance) GameManager.Instance.AddScore(100);
            ScoreManager.Enemy(100, transform.position + Vector3.up * (.5f * UpSign), UpSign);
            if (artwork) artwork.flipY = !artwork.flipY;
            StartCoroutine(FireDeath(travelDirection));
        }
        IEnumerator FireDeath(int direction)
        {
            Vector2 v = new Vector2(direction * 3f, 6f * UpSign);
            for (float t = 0; t < .65f; t += Time.deltaTime)
            { transform.position += (Vector3)(v * Time.deltaTime); v.y -= 26f * UpSign * Time.deltaTime; yield return null; }
            Destroy(gameObject);
        }
        public void Stomp() => Defeat(AudioManager.Cue.Stomp);
        public void HitFromBelow() => Defeat(AudioManager.Cue.Kick);
        void Defeat(AudioManager.Cue cue)
        {
            if (!Active || consumed || kind != Kind.Enemy) return;
            consumed = true; Active = false; body.simulated = false;
            var anim = GetComponent<SpriteAnimation>(); if (anim) anim.enabled = false;
            AudioManager.Play(cue);
            if (GameManager.Instance) GameManager.Instance.AddScore(100);
            ScoreManager.Enemy(100, transform.position + Vector3.up * (.5f * UpSign), UpSign);
            if (artwork)
            {
                if (stompedSprite) artwork.sprite = stompedSprite;
                else { var s = artwork.transform.localScale; s.y *= .35f; artwork.transform.localScale = s; }
                artwork.transform.position += Vector3.down * (.25f * UpSign);
            }
            Destroy(gameObject,.35f);
        }
    }
}
