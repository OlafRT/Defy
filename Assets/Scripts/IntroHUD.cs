using UnityEngine;
using UnityEngine.UI;

namespace RetroPlatformer
{
    // Only owns intro presentation. The regular HUD continues updating its own labels.
    [DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public class IntroHUD : MonoBehaviour
    {
        [Header("Existing HUD")]
        public HUD hud;
        [Header("Intro Only")]
        public GameObject blackBackground;
        public GameObject livesRow;
        public Text livesText;
        [Header("World Title")]
        public bool positionTitleForIntro = true;
        public Vector2 titleAnchor = new Vector2(.5f, .6375f);
        public Vector2 titleOffset;

        GameManager session;
        Text title;
        bool showing, positioned, originalActive;
        Vector2 originalMin, originalMax, originalPivot, originalSize;
        Vector3 originalPosition;
        TextAnchor originalAlignment;
        string originalText;

        public bool Begin(GameManager game)
        {
            if (showing) return true;
            if (!isActiveAndEnabled || !game || !hud || !hud.centerMessageText || !blackBackground || !livesRow || !livesText)
                return false;
            session = game;
            title = hud.centerMessageText;
            originalActive = title.gameObject.activeSelf;
            originalText = title.text;
            showing = true;
            positioned = false;
            blackBackground.SetActive(true);
            livesRow.SetActive(true);
            title.gameObject.SetActive(true);
            return true;
        }

        // HUD.LateUpdate normally clears CenterMessage. Run after it, only during the intro.
        void LateUpdate() { Refresh(); }

        public void Refresh()
        {
            if (!showing || !session || !title) return;
            if (positionTitleForIntro && !positioned)
            {
                var rect = title.rectTransform;
                originalMin = rect.anchorMin; originalMax = rect.anchorMax;
                originalPivot = rect.pivot; originalSize = rect.sizeDelta;
                originalPosition = rect.anchoredPosition3D;
                originalAlignment = title.alignment;
                Vector2 size = rect.rect.size;
                rect.anchorMin = rect.anchorMax = titleAnchor;
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = titleOffset;
                title.alignment = TextAnchor.MiddleCenter;
                positioned = true;
            }
            title.text = $"WORLD  {session.worldLabel}";
            if (livesText) livesText.text = $"×  {session.Lives}";
        }

        public void End()
        {
            if (!showing) return;
            showing = false;
            if (blackBackground) blackBackground.SetActive(false);
            if (livesRow) livesRow.SetActive(false);
            if (title)
            {
                if (positioned)
                {
                    var rect = title.rectTransform;
                    rect.anchorMin = originalMin; rect.anchorMax = originalMax;
                    rect.pivot = originalPivot; rect.sizeDelta = originalSize;
                    rect.anchoredPosition3D = originalPosition;
                    title.alignment = originalAlignment;
                }
                title.text = originalText;
                title.gameObject.SetActive(originalActive);
            }
            positioned = false;
        }

        void OnDisable() { End(); }
    }
}
