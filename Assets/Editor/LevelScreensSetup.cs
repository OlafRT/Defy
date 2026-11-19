using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RetroPlatformer
{
    public static class LevelScreensSetup
    {
        [MenuItem("Tools/Level Setup/Install Intro and Victory Screens")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            GameManager session = null;
            foreach (var candidate in Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == scene) { session = candidate; break; }
            if (!session) { Debug.LogWarning("Open a level containing Game Session first."); return; }
            HUD hud = null;
            foreach (var candidate in Object.FindObjectsByType<HUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != scene || !candidate.centerMessageText) continue;
                if (candidate.session == session) { hud = candidate; break; }
                if (!hud && !candidate.session) hud = candidate;
            }
            if (!hud || !hud.centerMessageText)
            { Debug.LogWarning("Assign Center Message Text on your existing HUD first."); return; }
            var area = hud.centerMessageText.transform.parent as RectTransform;
            var hudCanvas = hud.centerMessageText.GetComponentInParent<Canvas>(true);
            if (!area || !hudCanvas) { Debug.LogWarning("CenterMessage must be inside your existing UI Canvas."); return; }
            var canvasRoot = hudCanvas.rootCanvas ? hudCanvas.rootCanvas.transform : hudCanvas.transform;
            var screens = session.GetComponent<LevelScreens>();
            var oldIntro = screens ? screens.introCanvas : null;
            if (oldIntro && (canvasRoot == oldIntro.transform || canvasRoot.IsChildOf(oldIntro.transform)))
            { Debug.LogError("The legacy intro canvas contains the regular HUD. Migration stopped to avoid deleting your UI."); return; }

            var fontSource = hud.coinsLivesText ? hud.coinsLivesText : hud.centerMessageText;
            Font font = fontSource.font ? fontSource.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite mario = null;
            if (oldIntro)
                foreach (var image in oldIntro.GetComponentsInChildren<Image>(true))
                    if (image.name == "Mario Icon" && image.sprite) { mario = image.sprite; break; }
            if (!mario && session.player)
            {
                var animation = session.player.GetComponent<MarioAnimation>();
                mario = animation && animation.smallIdle ? animation.smallIdle : session.player.smallSprite;
            }

            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Use Existing HUD for Level Intro");
            if (!screens) screens = Undo.AddComponent<LevelScreens>(session.gameObject);
            var intro = session.GetComponent<IntroHUD>();
            if (!intro) intro = Undo.AddComponent<IntroHUD>(session.gameObject);
            Undo.RecordObjects(new Object[] { screens, intro, session }, "Configure Shared Intro");
            session.showDebugHUD = false;
            intro.hud = hud;
            screens.introHUD = intro;

            if (!intro.blackBackground)
            {
                var black = MakeStretch(canvasRoot, "Intro Background", typeof(Image));
                black.SetAsFirstSibling();
                var image = black.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
                intro.blackBackground = black.gameObject;
                black.gameObject.SetActive(false);
            }
            if (!intro.livesRow)
            {
                var row = MakeStretch(area, "Intro Lives");
                intro.livesRow = row.gameObject;
                var icon = MakeNormalized(row, "Mario Icon", new Vector2(106f / 256f, 121f / 240f), new Vector2(16f / 256f, 16f / 240f), typeof(Image));
                var image = icon.GetComponent<Image>(); image.sprite = mario; image.preserveAspect = true; image.raycastTarget = false;
                if (!mario) image.enabled = false;
                row.gameObject.SetActive(false);
            }
            if (!intro.livesText)
            {
                var rect = MakeNormalized(intro.livesRow.transform, "Lives", new Vector2(145f / 256f, 121f / 240f), new Vector2(55f / 256f, 20f / 240f), typeof(Text));
                var text = rect.GetComponent<Text>();
                text.font = font; text.fontSize = fontSource.fontSize; text.fontStyle = fontSource.fontStyle;
                text.color = fontSource.color; text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
                text.supportRichText = false; text.raycastTarget = false; text.text = "×  3";
                intro.livesText = text;
            }
            if (!screens.victoryCanvas)
            {
                screens.victoryCanvas = MakeCanvas("Victory Canvas", out RectTransform win);
                screens.victoryHeading = MakeText(win, "Victory Title", font, "YOU WIN!", 128, 45, 230, 30, 13);
                screens.finalScore = MakeText(win, "Final Score", font, "FINAL SCORE\n000000", 128, 87, 224, 38, 9);
                screens.victoryCredits = MakeText(win, "Credits", font, "", 128, 143, 224, 68, 8);
                screens.restartButton = MakeRestartButton(win, font);
                MakeText(win, "Controls", font, "ENTER / SPACE / START", 128, 221, 250, 20, 7);
                screens.victoryCanvas.gameObject.SetActive(false);
            }
            // Remove only the old generated intro canvas, never the existing HUD or victory canvas.
            screens.introCanvas = null; screens.introScore = null; screens.introCoins = null;
            screens.introWorld = null; screens.introTitle = null; screens.introLives = null;
            if (oldIntro && oldIntro != screens.victoryCanvas)
            {
                if (oldIntro.name == "Level Intro Canvas" && !oldIntro.transform.parent &&
                    !area.IsChildOf(oldIntro.transform) && (!screens.victoryCanvas || !screens.victoryCanvas.transform.IsChildOf(oldIntro.transform)))
                    Undo.DestroyObjectImmediate(oldIntro.gameObject);
                else
                {
                    Undo.RecordObject(oldIntro.gameObject, "Disable Legacy Intro");
                    oldIntro.gameObject.SetActive(false);
                    Debug.LogWarning("A renamed or reparented legacy intro was disabled, not deleted. You may remove it manually.", oldIntro);
                }
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(session);
            PrefabUtility.RecordPrefabInstancePropertyModifications(screens);
            PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = session.gameObject;
            Debug.Log("Level intro now uses the existing HUD. Save the scene.", screens);
        }

        static RectTransform MakeStretch(Transform parent, string name, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create Intro UI");
            go.transform.SetParent(parent, false);
            foreach (var type in components) go.AddComponent(type);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        static RectTransform MakeNormalized(Transform parent, string name, Vector2 center, Vector2 size, params System.Type[] components)
        {
            var r = MakeStretch(parent, name, components);
            r.anchorMin = center - size * .5f; r.anchorMax = center + size * .5f;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        static Canvas MakeCanvas(string name, out RectTransform frame)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Screen Canvas");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(256, 240);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var black = new GameObject("Black Background", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(go.transform, false);
            var rect = black.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = black.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
            var area = new GameObject("NES Screen Area", typeof(RectTransform));
            area.transform.SetParent(go.transform, false);
            frame = area.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero; frame.sizeDelta = new Vector2(256, 240);
            return canvas;
        }

        static RectTransform MakeRect(Transform parent, string name, float x, float y, float width, float height, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (var component in components) go.AddComponent(component);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        static Text MakeText(Transform parent, string name, Font font, string value, float x, float y, float width, float height, int size)
        {
            var r = MakeRect(parent, name, x, y, width, height, typeof(Text));
            var text = r.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false; text.raycastTarget = false;
            return text;
        }

        static Button MakeRestartButton(Transform parent, Font font)
        {
            var r = MakeRect(parent, "Restart Button", 128, 199, 152, 24, typeof(Image), typeof(Button));
            var image = r.GetComponent<Image>(); image.color = new Color(.17f, .17f, .17f, 1);
            var button = r.GetComponent<Button>(); button.targetGraphic = image;
            var text = MakeText(r, "Label", font, "PLAY AGAIN", 76, 12, 140, 22, 9);
            return button;
        }
    }
}
