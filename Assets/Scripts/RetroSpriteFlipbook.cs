using UnityEngine;

namespace RetroPlatformer
{
    public class RetroSpriteFlipbook : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        public bool playOnlyWhenMoving = true;
        public float minSpeed = 0.05f;

        Rigidbody2D body;
        int lastFrame = -1;

        void Awake()
        {
            if (!target) target = GetComponentInChildren<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (!target || frames == null || frames.Length == 0) return;
            bool animate = true;
            if (playOnlyWhenMoving && body) animate = Mathf.Abs(body.linearVelocity.x) > minSpeed;
            int frame = animate ? (int)(Time.time * framesPerSecond) % frames.Length : 0;
            if (frame == lastFrame) return;
            lastFrame = frame;
            target.sprite = frames[frame];
        }
    }
}
