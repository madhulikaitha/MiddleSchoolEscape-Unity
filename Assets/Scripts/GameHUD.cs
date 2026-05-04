using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject in the scene (e.g. an empty "HUD" object).
/// Creates its own Canvas, loads heart sprites from Resources/Hearts/,
/// and displays the timer top-centre.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class GameHUD : MonoBehaviour
{
    // ── Sprites loaded from Resources/Hearts/ ──────────────────────────
    private static readonly string[] HeartSpritePaths =
    {
        "Hearts/hearts_0",
        "Hearts/hearts_1",
        "Hearts/hearts_2",
        "Hearts/hearts_3",
        "Hearts/hearts_4",
        "Hearts/hearts_5",
    };

    [Header("Layout")]
    public float heartBarWidth  = 220f;
    public float heartBarHeight = 60f;
    public float edgePadding    = 20f;

    public float timerFontSize  = 32f;

    // ── Runtime references ─────────────────────────────────────────────
    private Image  heartImage;
    private Text   timerText;
    private Sprite[] heartSprites;   // index 0 = 0 hearts … index 5 = full

    private float elapsedTime;
    private bool wasRunActiveLastFrame;

    // ── Unity lifecycle ────────────────────────────────────────────────
    private void Awake()
    {
        SetupCanvas();
        LoadHeartSprites();
        BuildHeartBar();
        BuildTimer();
    }

    private void Start()
    {
        wasRunActiveLastFrame = PlayerSessionData.IsRunActive;
    }

    private void OnEnable()
    {
        wasRunActiveLastFrame = PlayerSessionData.IsRunActive;
        SetHudVisible(PlayerSessionData.IsRunActive);
        RefreshHeartImage();

        PlayerSessionData.OnRunTimerReset -= OnRunTimerReset;
        PlayerSessionData.OnRunTimerReset += OnRunTimerReset;

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged -= RefreshHeartImage;
            PlayerHealth.Instance.OnHealthChanged += RefreshHeartImage;
        }
    }

    private void OnDisable()
    {
        PlayerSessionData.OnRunTimerReset -= OnRunTimerReset;
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= RefreshHeartImage;
    }

    private void OnRunTimerReset()
    {
        elapsedTime = 0f;
        PlayerSessionData.UpdateRunTime(0f);
        if (timerText != null)
            timerText.text = "00:00";
    }

    private void Update()
    {
        bool isRunActive = PlayerSessionData.IsRunActive;
        if (isRunActive != wasRunActiveLastFrame)
        {
            SetHudVisible(isRunActive);
            if (isRunActive)
            {
                elapsedTime = 0f;
            }
            else
            {
                elapsedTime = 0f;
                if (timerText != null)
                    timerText.text = "00:00";
            }

            wasRunActiveLastFrame = isRunActive;
        }

        if (!isRunActive)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        PlayerSessionData.UpdateRunTime(elapsedTime);
        int minutes = (int)(elapsedTime / 60f);
        int seconds = (int)(elapsedTime % 60f);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // ── Canvas setup ───────────────────────────────────────────────────
    private void SetupCanvas()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // ── Sprite loading ─────────────────────────────────────────────────
    private void LoadHeartSprites()
    {
        heartSprites = new Sprite[HeartSpritePaths.Length];
        for (int i = 0; i < HeartSpritePaths.Length; i++)
        {
            var tex = Resources.Load<Texture2D>(HeartSpritePaths[i]);
            if (tex != null)
                heartSprites[i] = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            else
                Debug.LogWarning($"[GameHUD] Missing sprite at Resources/{HeartSpritePaths[i]}");
        }
    }

    // ── Heart bar (top-left) ───────────────────────────────────────────
    private void BuildHeartBar()
    {
        var go = new GameObject("HeartBar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(edgePadding, -edgePadding);
        rt.sizeDelta = new Vector2(heartBarWidth, heartBarHeight);

        heartImage = go.GetComponent<Image>();
        heartImage.preserveAspect = true;
    }

    // ── Timer (top-centre) ─────────────────────────────────────────────
    private void BuildTimer()
    {
        var go = new GameObject("Timer", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -edgePadding);
        rt.sizeDelta = new Vector2(200f, 60f);

        timerText = go.GetComponent<Text>();
        timerText.alignment = TextAnchor.UpperCenter;
        timerText.fontSize  = (int)timerFontSize;
        timerText.fontStyle = FontStyle.Bold;
        timerText.color     = Color.white;
        timerText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.text      = "00:00";

        // Drop-shadow outline so it's readable over any background.
        var outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    // ── Update heart image ─────────────────────────────────────────────
    private void RefreshHeartImage()
    {
        if (heartImage == null || heartSprites == null) return;

        int hearts = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHearts : 5;
        hearts = Mathf.Clamp(hearts, 0, heartSprites.Length - 1);

        if (heartSprites[hearts] != null)
            heartImage.sprite = heartSprites[hearts];
    }

    private void SetHudVisible(bool isVisible)
    {
        if (heartImage != null)
            heartImage.enabled = isVisible;
        if (timerText != null)
            timerText.enabled = isVisible;
    }
}
