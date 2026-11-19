using UnityEngine;

namespace RetroPlatformer
{
    [DisallowMultipleComponent]
    public sealed class FireBreakable : MonoBehaviour
    {
        [Tooltip("Off uses the same hit sound as a Goomba. On uses the brick-breaking sound.")]
        public bool useBrickBreakSound = false;

        bool broken;

        public bool TryBreak()
        {
            if (broken) return true;

            // Never destroy question/power-up blocks, even if marked by mistake.
            var block = GetComponentInParent<Block>();
            if (block && block.kind != Block.Kind.Brick && block.kind != Block.Kind.Solid)
                return false;

            broken = true;
            AudioManager.Play(useBrickBreakSound ? AudioManager.Cue.BreakBlock : AudioManager.Cue.Kick);

            // Remove the tile and its collision immediately. The fireball owns the explosion.
            foreach (var collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;
            if (block && block.gameObject == gameObject)
                block.onBroken?.Invoke();
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }
    }
}
