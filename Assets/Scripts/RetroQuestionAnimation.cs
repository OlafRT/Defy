using UnityEngine;

namespace RetroPlatformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RetroBlock))]
    public sealed class RetroQuestionAnimation : MonoBehaviour
    {
        public Sprite lightFrame, mediumFrame, darkFrame;
        [Min(.01f)] public float lightDuration = .4f;
        [Min(.01f)] public float transitionDuration = 8f / 60f;
        public bool animate = true;
        RetroBlock block;
        void Awake() { block = GetComponent<RetroBlock>(); }
        void LateUpdate()
        {
            if (!animate || !block || block.Used) return;
            if (block.kind != RetroBlock.Kind.Coin && block.kind != RetroBlock.Kind.Mushroom && block.kind != RetroBlock.Kind.ExtraLife) return;
            if (!block.artwork || !lightFrame || !mediumFrame || !darkFrame) return;
            // A common scene clock keeps all question blocks in sync.
            float light = Mathf.Max(.01f, lightDuration);
            float step = Mathf.Max(.01f, transitionDuration);
            float phase = Mathf.Repeat(Time.timeSinceLevelLoad, light + step * 3f);
            Sprite next = phase < light ? lightFrame : phase < light + step ? mediumFrame : phase < light + 2f * step ? darkFrame : mediumFrame;
            if (block.artwork.sprite != next) block.artwork.sprite = next;
        }
    }
}
