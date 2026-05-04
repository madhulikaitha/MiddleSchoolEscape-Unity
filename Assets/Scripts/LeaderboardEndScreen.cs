using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Full-screen TOP 10 leaderboard after a successful run. Uses Resources/Leaderboard/LeaderboardBackground as the artwork (optional fallback panel).
/// </summary>
public class LeaderboardEndScreen : MonoBehaviour
{
    private const int TopCount = 10;

    /// <summary>Reference resolution 1920×1080 — adjust if text does not line up with the painted columns.</summary>
    private static class ArtLayout
    {
        public const float TableWidth = 780f;
        public const float TableHeight = 440f;
        public const float TableVerticalOffset = -18f;
        public const float FirstRowY = 136f;
        public const float RowStep = 41f;
        public const float RankColumnX = -278f;
        public const float NameColumnX = -15f;
        public const float TimeColumnX = 268f;
        public const int RowFontSize = 27;
        public const int FooterFontSize = 25;
        public const int CongratsFontSize = 22;
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
        {
            UiRect.Text(table, "Congrats", $"ESCAPED — {playerName.Trim().ToUpperInvariant()}", ArtLayout.CongratsFontSize, cream, font,
                new Vector2(0f, ArtLayout.FirstRowY + 54f), new Vector2(ArtLayout.TableWidth - 40f, 34f), TextAnchor.MiddleCenter, true);
        }

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
                hi.color = highlightTint;
                hi.raycastTarget = false;
            }

            if (r < sorted.Count)
            {
                var e = sorted[r];
                LayoutRow(rowRt, r + 1, e.playerName, FormatTime(e.timeSeconds), font, cream, gold);
            }
            else
            {
                LayoutRow(rowRt, r + 1, "—", "—", font, new Color(1f, 1f, 1f, 0.4f), gold);
            }

            y -= ArtLayout.RowStep;
        }

        string yourLine = rankOneBased <= TopCount
            ? $"YOUR RANK: #{rankOneBased}   •   {FormatTime(elapsedSeconds)}"
            : $"YOUR RANK: #{rankOneBased} (outside top {TopCount})   •   {FormatTime(elapsedSeconds)}";
        UiRect.Text(canvasGo.transform, "YourRank", yourLine, ArtLayout.FooterFontSize, gold, font,
            new Vector2(0f, -388f), new Vector2(920f, 44f), TextAnchor.MiddleCenter, true);
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    private static void LayoutRow(RectTransform parent, int rank, string name, string time, Font font, Color textCol, Color gold)
    {
        var rankStr = rank.ToString();
        UiRect.Text(parent, "Rank", rankStr, ArtLayout.RowFontSize, gold, font, new Vector2(ArtLayout.RankColumnX, 0f),
            new Vector2(64f, 42f), TextAnchor.MiddleCenter, true);
        UiRect.Text(parent, "Name", name ?? string.Empty, ArtLayout.RowFontSize, textCol, font, new Vector2(ArtLayout.NameColumnX, 0f),
            new Vector2(360f, 42f), TextAnchor.MiddleCenter, true);
        UiRect.Text(parent, "Time", time, ArtLayout.RowFontSize, textCol, font, new Vector2(ArtLayout.TimeColumnX, 0f),
            new Vector2(132f, 42f), TextAnchor.MiddleCenter, true);
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
    }
}
