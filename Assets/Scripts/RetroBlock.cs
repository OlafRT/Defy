using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RetroPlatformer
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RetroBlock : MonoBehaviour
    {
        public enum Kind { Brick, Coin, Mushroom, ExtraLife, Solid }
        public Kind kind;
        public SpriteRenderer artwork;
        public Sprite usedSprite;
        public RetroWalker mushroomPrefab, extraLifePrefab;
        public RetroWalker fireFlowerPrefab;
        public bool upgradeMushroomToFlower = true;
        public Sprite coinSprite;
        public float bumpHeight = .22f, bumpDuration = .16f;
        public UnityEvent onBumped, onEmptied, onBroken;
        public bool Used { get; private set; }
        bool busy;
        void Awake() { if (!artwork) artwork = GetComponentInChildren<SpriteRenderer>(); }
        public void Bump(RetroPlayer player)
        {
            if (busy) return;
            if (Used || kind == Kind.Solid) { RetroAudio.Play(RetroAudio.Cue.Bump); return; }
            busy = true; onBumped?.Invoke();
            // Kill enemies standing on top of a bumped block.
            var b = GetComponent<BoxCollider2D>().bounds;
            foreach (var c in Physics2D.OverlapBoxAll(new Vector2(b.center.x,b.max.y + .14f),new Vector2(.9f,.35f),0))
            { var w = c.GetComponentInParent<RetroWalker>(); if (w && w.kind == RetroWalker.Kind.Enemy) w.HitFromBelow(); }
            if (kind == Kind.Brick && player.IsBig)
            {
                if (RetroSession.Instance) RetroSession.Instance.AddScore(50);
                RetroScorePopups.Brick(transform.position + Vector3.up * .5f);
                RetroAudio.Play(RetroAudio.Cue.BreakBlock);
                onBroken?.Invoke(); Break(); return;
            }
            if (kind == Kind.Brick) RetroAudio.Play(RetroAudio.Cue.Bump);
            if (kind != Kind.Brick)
            {
                Used = true; onEmptied?.Invoke();
                if (artwork && usedSprite) artwork.sprite = usedSprite;
                if (kind == Kind.Coin)
                {
                    if (RetroSession.Instance) RetroSession.Instance.AddCoin();
                    RetroScorePopups.BlockCoin(transform.position + Vector3.up * 1.05f, coinSprite ? .42f : 0f);
                    PopCoin();
                }
                else
                {
                    RetroWalker prefab = kind == Kind.ExtraLife ? extraLifePrefab :
                        (upgradeMushroomToFlower && player.IsBig && fireFlowerPrefab ? fireFlowerPrefab : mushroomPrefab);
                    if (prefab)
                    {
                        var item = Instantiate(prefab, new Vector3(b.center.x, b.max.y - .45f, 0), Quaternion.identity);
                        item.Emerge(b.max.y + .48f);
                    }
                }
            }
            StartCoroutine(BumpAnimation());
        }
        IEnumerator BumpAnimation()
        {
            Transform visual = artwork && artwork.transform != transform ? artwork.transform : null;
            Vector3 start = visual ? visual.localPosition : Vector3.zero;
            for (float t = 0; t < bumpDuration; t += Time.deltaTime)
            { if (visual) visual.localPosition = start + Vector3.up * (Mathf.Sin(t / bumpDuration * Mathf.PI) * bumpHeight); yield return null; }
            if (visual) visual.localPosition = start;
            busy = false;
        }
        void PopCoin()
        {
            if (!coinSprite) return;
            var go = new GameObject("Coin pop"); go.transform.position = transform.position + Vector3.up * .6f;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = coinSprite; sr.sortingOrder = 3; if (artwork) sr.sharedMaterial = artwork.sharedMaterial;
            go.AddComponent<RetroEffect>().Launch(Vector2.up * 6f, .42f, 25f);
        }
        void Break()
        {
            if (artwork && artwork.sprite)
                for (int i = 0; i < 4; i++)
                {
                    var p = new GameObject("Brick debris"); p.transform.position = transform.position + new Vector3(i % 2 == 0 ? -.25f : .25f, i < 2 ? .25f : -.25f);
                    p.transform.localScale = Vector3.one * .42f;
                    var sr = p.AddComponent<SpriteRenderer>(); sr.sprite = artwork.sprite; sr.sortingOrder = 3; sr.sharedMaterial = artwork.sharedMaterial;
                    p.AddComponent<RetroEffect>().Launch(new Vector2(i % 2 == 0 ? -3 : 3, i < 2 ? 9 : 5), .7f, 28f);
                }
            GetComponent<BoxCollider2D>().enabled = false; Destroy(gameObject);
        }
    }
}
