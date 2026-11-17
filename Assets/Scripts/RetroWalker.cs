using System.Collections;
using UnityEngine;
namespace RetroPlatformer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class RetroWalker : MonoBehaviour
    {
        public enum Kind { Enemy, Mushroom, ExtraLife, Flower }
        public Kind kind;
        public float speed = 1.875f, gravity = 3.5f, fallLimit = 12f;
        public int startDirection = -1;
        public bool waitForCamera = true;
        public float activationMargin = 1f;
        public SpriteRenderer artwork;
        public Sprite stompedSprite;
        public Sprite[] flowerFrames;
        public float flowerFPS = 12f;
        public bool Active { get; private set; }
        Rigidbody2D body; BoxCollider2D box; int direction; bool emerging, consumed;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            body.freezeRotation = true; body.gravityScale = gravity; body.linearDamping = 0;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            direction = startDirection < 0 ? -1 : 1;
            if (!artwork) artwork = GetComponentInChildren<SpriteRenderer>();
            if (kind == Kind.Flower) { body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0; box.isTrigger = true; }
            Active = !waitForCamera || kind != Kind.Enemy;
            body.simulated = Active;
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
                if (!cam || transform.position.x <= cam.transform.position.x + cam.orthographicSize * cam.aspect + activationMargin)
                { Active = true; body.simulated = true; }
                else return;
            }
            var v = body.linearVelocity;
            body.linearVelocity = new Vector2(direction * speed, Mathf.Max(-fallLimit,v.y));
            if (transform.position.y < -5) Destroy(gameObject);
            if (artwork) artwork.flipX = direction > 0;
        }
        void OnCollisionEnter2D(Collision2D c) => Turn(c);
        void OnCollisionStay2D(Collision2D c) => Turn(c);
        void Turn(Collision2D c)
        {
            if (!Active || consumed || c.collider.GetComponentInParent<RetroPlayer>()) return;
            foreach (var contact in c.contacts)
                if (Mathf.Abs(contact.normal.x) > .6f && direction * contact.normal.x < 0)
                { direction = contact.normal.x > 0 ? 1 : -1; break; }
        }
        public void Emerge(float targetY)
        {
            RetroAudio.Play(RetroAudio.Cue.PowerupAppears);
            emerging = true; Active = false; body.simulated = false;
            if (artwork) artwork.sortingOrder = -1;
            StartCoroutine(Rise(targetY));
        }
        IEnumerator Rise(float targetY)
        {
            while (transform.position.y < targetY - .001f)
            { transform.position = Vector3.MoveTowards(transform.position,new Vector3(transform.position.x,targetY,transform.position.z),1.2f*Time.deltaTime); yield return null; }
            direction = 1; emerging = false; Active = true; body.position = transform.position; body.simulated = true;
            if (artwork) artwork.sortingOrder = 2;
        }
        public void Collect(RetroPlayer player)
        {
            if (!Active || consumed || kind == Kind.Enemy || player.IsDead) return;
            consumed = true; Active = false; body.simulated = false;
            if (kind == Kind.Mushroom) { RetroAudio.Play(RetroAudio.Cue.Powerup); player.Grow(); if (RetroSession.Instance) RetroSession.Instance.AddScore(1000); RetroScorePopups.PowerUp("1000", player.transform.position + Vector3.up * (player.IsBig ? 1.65f : .85f)); }
            else if (kind == Kind.Flower) { RetroAudio.Play(RetroAudio.Cue.Powerup); player.GainFire(); if (RetroSession.Instance) RetroSession.Instance.AddScore(1000); RetroScorePopups.PowerUp("1000", player.transform.position + Vector3.up * (player.IsBig ? 1.65f : .85f)); }
            else { if (RetroSession.Instance) RetroSession.Instance.AddLife(); RetroScorePopups.PowerUp("1UP", player.transform.position + Vector3.up * (player.IsBig ? 1.65f : .85f)); }
            Destroy(gameObject);
        }
        public void HitByFire(int travelDirection)
        {
            if (!Active || consumed || kind != Kind.Enemy) return;
            consumed = true; Active = false; body.simulated = false; box.enabled = false;
            var flipbook = GetComponent<RetroSpriteFlipbook>(); if (flipbook) flipbook.enabled = false;
            RetroAudio.Play(RetroAudio.Cue.Kick);
            if (RetroSession.Instance) RetroSession.Instance.AddScore(100);
            RetroScorePopups.Enemy(100, transform.position + Vector3.up * .5f);
            if (artwork) artwork.flipY = true;
            StartCoroutine(FireDeath(travelDirection));
        }
        IEnumerator FireDeath(int direction)
        {
            Vector2 v = new Vector2(direction * 3f, 6f);
            for (float t = 0; t < .65f; t += Time.deltaTime)
            { transform.position += (Vector3)(v * Time.deltaTime); v.y -= 26f * Time.deltaTime; yield return null; }
            Destroy(gameObject);
        }
        public void Stomp() => Defeat(RetroAudio.Cue.Stomp);
        public void HitFromBelow() => Defeat(RetroAudio.Cue.Kick);
        void Defeat(RetroAudio.Cue cue)
        {
            if (!Active || consumed || kind != Kind.Enemy) return;
            consumed = true; Active = false; body.simulated = false;
            var anim = GetComponent<RetroSpriteFlipbook>(); if (anim) anim.enabled = false;
            RetroAudio.Play(cue);
            if (RetroSession.Instance) RetroSession.Instance.AddScore(100);
            RetroScorePopups.Enemy(100, transform.position + Vector3.up * .5f);
            if (artwork)
            {
                if (stompedSprite) artwork.sprite = stompedSprite;
                else { var s = artwork.transform.localScale; s.y *= .35f; artwork.transform.localScale = s; }
                artwork.transform.position += Vector3.down * .25f;
            }
            Destroy(gameObject,.35f);
        }
    }
}
