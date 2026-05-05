using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Full-screen TOP 10 leaderboard after a successful run. Uses Resources/Leaderboard/LeaderboardBackground as the artwork (optional fallback panel).
/// </summary>
public class LeaderboardEndScreen : MonoBehaviour
{
    private const int TopCount = 10;

    private static Sprite _uiBlockSprite;

    private static Sprite UiBlockSprite()
    {
        if (_uiBlockSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            _uiBlockSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _uiBlockSprite.name = "Leaderboard_UIBlock";
        }

        return _uiBlockSprite;
    }

    /// <summary>Reference resolution 1920×1080 — table uses proportional columns so rows align cleanly.</summary>
    private static class ArtLayout
    {
        public const float TableWidth = 780f;
        public const float TableHeight = 440f;
        public const float TableVerticalOffset = -18f;
        public const float FirstRowY = 120f;
        public const float RowStep = 40f;
        public const int RowFontSize = 26;
        public const int FooterFontSize = 25;
        public const int CongratsFontSize = 22;
        // Normalized horizontal bands inside each row (0–1): rank | name | time
        public const float RankBandMax = 0.12f;
        public const float NameBandMin = 0.14f;
        public const float NameBandMax = 0.72f;
        public const float TimeBandMin = 0.74f;
    }

    public static void Show(int rankOneBased, string highlightRunId, string playerName, float elapsedSeconds)
    {
        var go = new GameObject("LeaderboardEndScreen");
        var screen = go.AddComponent<LeaderboardEndScreen>();
        screen.Build(rankOneBased, highlightRunId, playerName, elapsedSeconds);
    }

    private void Build(int rankOneBased, string highlightRunId, string playerName, float elapsedSeconds)
    {
        EnsureEventSystem();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var navy = new Color(0.08f, 0.12f, 0.28f, 0.96f);
        var gold = new Color(0.96f, 0.88f, 0.55f, 1f);
        var cream = new Color(0.98f, 0.96f, 0.92f, 1f);
        var highlightTint = new Color(0.15f, 0.35f, 0.75f, 0.42f);

        var canvasGo = new GameObject("LeaderboardCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 2500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bgSprite = Resources.Load<Sprite>("Leaderboard/LeaderboardBackground");
        if (bgSprite != null)
        {
            UiRect.BackgroundSprite(canvasGo.transform, "LeaderboardArt", bgSprite);
        }
        else
        {
            Debug.LogWarning(
                "LeaderboardEndScreen: Missing sprite Resources/Leaderboard/LeaderboardBackground — assign texture import to Sprite (2D and UI), or use fallback.");
            _ = UiRect.SolidImage(canvasGo.transform, "Backdrop", fullStretch: true, navy);
        }

        var table = UiRect.Create(canvasGo.transform, "TableOverlay", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), size: new Vector2(ArtLayout.TableWidth, ArtLayout.TableHeight),
            anchoredPos: new Vector2(0f, ArtLayout.TableVerticalOffset));

        if (!string.IsNullOrWhiteSpace(playerName))
            UiRect.TextAnchoredStretch(table.transform, "Congrats", $"ESCAPED — {playerName.Trim().ToUpperInvariant()}",
                ArtLayout.CongratsFontSize, cream, font, TextAnchor.MiddleCenter, true, addOutline: true,
                anchorMin: new Vector2(0.04f, 1f), anchorMax: new Vector2(0.96f, 1f), pivot: new Vector2(0.5f, 1f),
                anchoredPos: new Vector2(0f, -8f), sizeDelta: new Vector2(0f, 36f));

        List<LeaderboardStorage.LeaderboardEntryData> sorted = LeaderboardStorage.LoadAllSorted();
        int highlightRow = -1;
        for (var i = 0; i < Mathf.Min(TopCount, sorted.Count); i++)
        {
            if (!string.IsNullOrEmpty(highlightRunId) && sorted[i].runId == highlightRunId)
            {
                highlightRow = i;
                break;
            }
        }

        float y = ArtLayout.FirstRowY;
        for (var r = 0; r < TopCount; r++)
        {
            bool isHi = r == highlightRow;
            var rowRt = UiRect.Create(table, $"Row_{r + 1}", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f), size: new Vector2(ArtLayout.TableWidth - 24f, ArtLayout.RowStep - 1f),
                anchoredPos: new Vector2(0f, y));
            if (isHi)
            {
                var hi = rowRt.gameObject.AddComponent<Image>();
                hi.sprite = UiBlockSprite();
                hi.type = Image.Type.Simple;
                hi.color = highlightTint;
                hi.raycastTarget = false;
            }

            if (r < sorted.Count)
            {
                var e = sorted[r];
                LayoutRowProportional(rowRt, r + 1, e.playerName, FormatTime(e.timeSeconds), font, cream, gold, emptySlot: false);
            }
            else
            {
                LayoutRowProportional(rowRt, r + 1, string.Empty, string.Empty, font, cream, gold, emptySlot: true);
            }

            y -= ArtLayout.RowStep;
        }

        string yourLine = rankOneBased <= TopCount
            ? $"YOUR RANK: #{rankOneBased}   •   {FormatTime(elapsedSeconds)}"
            : $"YOUR RANK: #{rankOneBased} (outside top {TopCount})   •   {FormatTime(elapsedSeconds)}";
        UiRect.TextAnchoredStretch(canvasGo.transform, "YourRank", yourLine, ArtLayout.FooterFontSize, gold, font,
            TextAnchor.MiddleCenter, true, addOutline: true,
            anchorMin: new Vector2(0.08f, 0f), anchorMax: new Vector2(0.92f, 0f), pivot: new Vector2(0.5f, 0f),
            anchoredPos: new Vector2(0f, 118f), sizeDelta: new Vector2(0f, 44f));

        AddMenuButton(canvasGo.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(-170f, 28f), font, cream, OnPlayAgainClicked);
        AddMenuButton(canvasGo.transform, "QuitButton", "QUIT", new Vector2(170f, 28f), font, cream, OnQuitClicked);
    }

    private void OnPlayAgainClicked()
    {
        Time.timeScale = 1f;
        GameVictoryFlow.ResetSequenceLock();
        PlayerSessionData.ClearForRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void AddMenuButton(Transform parent, string name, string label, Vector2 anchoredPos, Font font, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300f, 58f);
        var img = go.GetComponent<Image>();
        img.sprite = UiBlockSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.12f, 0.16f, 0.32f, 0.96f);
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.28f, 0.5f, 1f);
        colors.pressedColor = new Color(0.08f, 0.1f, 0.2f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var t = textGo.GetComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = ArtLayout.FooterFontSize;
        t.color = textColor;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    private static void LayoutRowProportional(RectTransform parent, int rank, string name, string time, Font font, Color textCol, Color gold,
        bool emptySlot)
    {
        var nameTint = emptySlot ? new Color(textCol.r, textCol.g, textCol.b, 0f) : textCol;
        var timeTint = emptySlot ? new Color(textCol.r, textCol.g, textCol.b, 0f) : textCol;

        var pad = new Vector4(8f, 4f, 8f, 4f);
        UiRect.TextAnchoredStretch(parent.transform, "Rank", rank.ToString(),
            ArtLayout.RowFontSize, gold, font, TextAnchor.MiddleCenter, true, addOutline: false,
            anchorMin: new Vector2(0.02f, 0f), anchorMax: new Vector2(ArtLayout.RankBandMax, 1f),
            pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, sizeDelta: Vector2.zero, stretchPadding: pad);

        UiRect.TextAnchoredStretch(parent.transform, "Name", name ?? string.Empty,
            ArtLayout.RowFontSize, nameTint, font, TextAnchor.MiddleCenter, true, addOutline: false,
            anchorMin: new Vector2(ArtLayout.NameBandMin, 0f), anchorMax: new Vector2(ArtLayout.NameBandMax, 1f),
            pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, sizeDelta: Vector2.zero, stretchPadding: pad);

        UiRect.TextAnchoredStretch(parent.transform, "Time", time ?? string.Empty,
            ArtLayout.RowFontSize, timeTint, font, TextAnchor.MiddleCenter, true, addOutline: false,
            anchorMin: new Vector2(ArtLayout.TimeBandMin, 0f), anchorMax: new Vector2(0.98f, 1f),
            pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, sizeDelta: Vector2.zero, stretchPadding: pad);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            Destroy(standaloneModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static class UiRect
    {
        public static RectTransform Create(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size,
            Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return rt;
        }

        public static void BackgroundSprite(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        public static RectTransform SolidImage(Transform parent, string name, bool fullStretch, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (fullStretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var img = go.GetComponent<Image>();
            img.sprite = UiBlockSprite();
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            rt.SetAsFirstSibling();
            return rt;
        }

        public static Text Text(Transform parent, string name, string content, int fontSize, Color color, Font font, Vector2 anchoredPos,
            Vector2 size, TextAnchor align, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        /// <summary>stretchPadding: x=left, y=bottom, z=right, w=top (insets, all positive).</summary>
        public static Text TextAnchoredStretch(Transform parent, string name, string content, int fontSize, Color color, Font font,
            TextAnchor align, bool bold, bool addOutline, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta, Vector4 stretchPadding = default)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            if (stretchPadding != Vector4.zero)
            {
                rt.offsetMin = new Vector2(stretchPadding.x, stretchPadding.y);
                rt.offsetMax = new Vector2(-stretchPadding.z, -stretchPadding.w);
            }

            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;

            if (addOutline)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
                outline.effectDistance = new Vector2(1.1f, -1.1f);
            }

            return t;
        }
    }
}
