using UnityEngine;

namespace RetroPlatformer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Block))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroQuestionAnimation")]
    public sealed class QuestionBlockAnimation : MonoBehaviour
    {
        [Header("Frames")]
        public Sprite lightFrame;
        public Sprite mediumFrame;
        public Sprite darkFrame;
        [Header("Timing")]
        [Min(.01f)] public float lightDuration = .4f;
        [Min(.01f)] public float transitionDuration = 8f / 60f;
        public bool animate = true;
        Block block;
        void Awake() { block = GetComponent<Block>(); }
        void LateUpdate()
        {
            if (!animate || !block || block.Used) return;
            if (block.kind != Block.Kind.Coin && block.kind != Block.Kind.Mushroom && block.kind != Block.Kind.ExtraLife && block.kind != Block.Kind.GravityMushroom) return;
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
