using UnityEngine;

namespace RetroPlatformer
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class HUDGravity : MonoBehaviour
    {
        [Tooltip("Optional. Defaults to the player assigned to GameManager.")]
        public PlayerMovement player;

        Quaternion normalRotation;
        RectTransform area;

        void OnEnable()
        {
            area = GetComponent<RectTransform>();
            normalRotation = area.localRotation;
        }

        void LateUpdate()
        {
            PlayerMovement target = player;
            if (!target && GameManager.Instance)
                target = GameManager.Instance.player;

            bool inverted = target && target.IsInverted;
            area.localRotation = normalRotation * Quaternion.Euler(0f, 0f, inverted ? 180f : 0f);
        }

        void OnDisable()
        {
            if (area) area.localRotation = normalRotation;
        }
    }
}
