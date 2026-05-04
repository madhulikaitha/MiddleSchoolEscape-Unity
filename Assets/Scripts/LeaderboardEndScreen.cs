using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Full-screen TOP 10 leaderboard after a successful run. Built at runtime (no prefab required).
/// </summary>
public class LeaderboardEndScreen : MonoBehaviour
{
    private const int TopCount = 10;

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
        var gold = new Color(0.92f, 0.78f, 0.28f, 1f);
        var white = Color.white;
        var highlightBg = new Color(0.18f, 0.28f, 0.55f, 1f);

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

        _ = UiRect.Image(canvasGo.transform, "Backdrop", fullStretch: true, navy);

        var board = UiRect.Create(canvasGo.transform, "Board", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), size: new Vector2(920f, 780f), anchoredPos: Vector2.zero);
        var boardImg = board.gameObject.AddComponent<Image>();
        boardImg.color = new Color(0.06f, 0.09f, 0.22f, 1f);
        var boardOutline = board.gameObject.AddComponent<Outline>();
        boardOutline.effectColor = gold;
        boardOutline.effectDistance = new Vector2(3f, -3f);

        float y = 318f;
        UiRect.Text(board, "Title", "LEADERBOARD", 40, gold, font, new Vector2(0f, y), new Vector2(860f, 52f), TextAnchor.MiddleCenter, true);
        y -= 52f;
        UiRect.Text(board, "Subtitle", "TOP 10", 26, gold, font, new Vector2(0f, y), new Vector2(400f, 36f), TextAnchor.MiddleCenter, true);
        y -= 40f;
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            UiRect.Text(board, "Congrats", $"ESCAPED — {playerName.Trim().ToUpperInvariant()}", 20, white, font, new Vector2(0f, y), new Vector2(860f, 32f), TextAnchor.MiddleCenter, true);
            y -= 34f;
        }

        var headerRow = UiRect.Create(board, "HeaderRow", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), size: new Vector2(840f, 32f), anchoredPos: new Vector2(0f, y));
        LayoutHeader(headerRow, font, gold);
        y -= 38f;

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

        float rowH = 46f;
        for (var r = 0; r < TopCount; r++)
        {
            bool isHi = r == highlightRow;
            var rowRt = UiRect.Create(board, $"Row_{r + 1}", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f), size: new Vector2(840f, rowH - 2f), anchoredPos: new Vector2(0f, y));
            var rowBg = rowRt.gameObject.AddComponent<Image>();
            rowBg.color = isHi ? highlightBg : new Color(0f, 0f, 0f, 0.25f);
            if (r < sorted.Count)
            {
                var e = sorted[r];
                LayoutRow(rowRt, r + 1, e.playerName, FormatTime(e.timeSeconds), font, white, gold);
            }
            else
            {
                LayoutRow(rowRt, r + 1, "—", "—", font, new Color(1f, 1f, 1f, 0.35f), gold);
            }

            y -= rowH;
        }

        y -= 20f;
        string yourLine = rankOneBased <= TopCount
            ? $"YOUR RANK: #{rankOneBased}   •   {FormatTime(elapsedSeconds)}"
            : $"YOUR RANK: #{rankOneBased} (outside top {TopCount})   •   {FormatTime(elapsedSeconds)}";
        UiRect.Text(board, "YourRank", yourLine, 20, gold, font, new Vector2(0f, y), new Vector2(860f, 36f), TextAnchor.MiddleCenter, true);
        y -= 48f;

        var btnRt = UiRect.Create(board, "PlayAgain", anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), size: new Vector2(280f, 48f), anchoredPos: new Vector2(0f, y));
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.22f, 0.45f, 1f);
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(GameVictoryFlow.OnPlayAgainClicked);
        var btnOutline = btnRt.gameObject.AddComponent<Outline>();
        btnOutline.effectColor = gold;
        btnOutline.effectDistance = new Vector2(2f, -2f);
        UiRect.Text(btnRt, "Label", "PLAY AGAIN", 26, gold, font, Vector2.zero, new Vector2(260f, 48f), TextAnchor.MiddleCenter, false);
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    private static void LayoutHeader(RectTransform parent, Font font, Color gold)
    {
        UiRect.Text(parent, "H_Rank", "RANK", 22, gold, font, new Vector2(-330f, 0f), new Vector2(120f, 32f), TextAnchor.MiddleCenter, false);
        UiRect.Text(parent, "H_Name", "NAME", 22, gold, font, new Vector2(0f, 0f), new Vector2(360f, 32f), TextAnchor.MiddleCenter, false);
        UiRect.Text(parent, "H_Time", "TIME", 22, gold, font, new Vector2(330f, 0f), new Vector2(140f, 32f), TextAnchor.MiddleCenter, false);
    }

    private static void LayoutRow(RectTransform parent, int rank, string name, string time, Font font, Color textCol, Color gold)
    {
        var rankStr = rank.ToString();
        UiRect.Text(parent, "Rank", rankStr, 20, gold, font, new Vector2(-330f, 0f), new Vector2(72f, 40f), TextAnchor.MiddleCenter, true);
        UiRect.Text(parent, "Name", name ?? string.Empty, 20, textCol, font, new Vector2(0f, 0f), new Vector2(360f, 40f), TextAnchor.MiddleCenter, true);
        UiRect.Text(parent, "Time", time, 20, textCol, font, new Vector2(330f, 0f), new Vector2(140f, 40f), TextAnchor.MiddleCenter, true);
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

        public static RectTransform Image(Transform parent, string name, bool fullStretch, Color color)
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
            img.raycastTarget = true;
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
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }
    }
}
