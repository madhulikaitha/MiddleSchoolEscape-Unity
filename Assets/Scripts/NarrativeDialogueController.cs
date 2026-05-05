using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scripted lines for zones, timers, and gameplay reactions. Uses a list queue so <see cref="EnqueuePriority"/> can jump ahead.
/// </summary>
public class NarrativeDialogueController : MonoBehaviour
{
    public static NarrativeDialogueController Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private float bottomBarHeight = 120f;
    [SerializeField] private float sidePadding = 48f;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private float queuePauseAfterLineSeconds = 0.72f;
    [SerializeField] private float displayHoldSeconds = 4f;

    [Header("Dialogue box")]
    [SerializeField] private Color panelFillPastelYellow = new Color(0.99f, 0.964f, 0.758f, 1f);
    [SerializeField] private Color dialogueTextDarkBlue = new Color(0.071f, 0.157f, 0.392f, 1f);

    [Tooltip("Resources path without extension (default: Fonts/DialoguePixel = Press Start 2P under Assets/Resources/).")]
    [SerializeField] private string dialogueFontResourcePath = "Fonts/DialoguePixel";

    [Header("Zone thresholds (player Y, align with WallBoundary rooms — tune in Inspector)")]
    [SerializeField] private float lobbyMaxY = 14f;
    [SerializeField] private float cafeteriaMaxY = 52f;
    [SerializeField] private float hallwayMaxY = 88f;

    private Text _lineText;
    private Canvas _canvas;

    private static Sprite _solidUiSprite;
    private readonly HashSet<string> _firedOnce = new HashSet<string>();
    private Coroutine _queueRoutine;
    private readonly List<string> _pending = new List<string>(16);
    private NarrativeZone _currentZone = NarrativeZone.Unknown;
    private Transform _player;

    private Coroutine _lobbyIntroCoroutine;
    private bool _lobbyIntroCancelled;

    private Coroutine _cafeteriaDodgeLineCoroutine;
    private Coroutine _cafeteriaTableBabbleCoroutine;
    private bool _cafeteriaDeparted;

    private Coroutine _hallwayScriptCoroutine;
    private bool _hallwayDeparted;

    private Coroutine _gasWildLineCoroutine;

    private bool _acceptSceneReloadEvents;

    public enum NarrativeZone
    {
        Unknown,
        Lobby,
        Cafeteria,
        Hallway,
        ScienceLab
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<NarrativeDialogueController>() != null)
            return;

        var go = new GameObject("NarrativeDialogue");
        go.AddComponent<NarrativeDialogueController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildUi();
    }

    private IEnumerator Start()
    {
        StartCoroutine(CoDeferAcceptReloadEvents());
        yield return null;
    }

    private IEnumerator CoDeferAcceptReloadEvents()
    {
        yield return null;
        yield return null;
        _acceptSceneReloadEvents = true;
        BeginLobbyOpeningSequence();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_acceptSceneReloadEvents)
            return;

        _firedOnce.Clear();
        StopNarrativeTimingCoroutines();
        StopAllCoroutines();
        ScienceLabHazardPlaybackGate.Reset();
        _queueRoutine = null;
        _pending.Clear();
        _player = null;
        _lobbyIntroCancelled = false;
        _cafeteriaDeparted = false;
        _hallwayDeparted = false;
        _acceptSceneReloadEvents = false;
        if (_lineText != null)
            _lineText.text = string.Empty;
        StartCoroutine(CoDeferAcceptReloadAfterReload());
    }

    private IEnumerator CoDeferAcceptReloadAfterReload()
    {
        yield return null;
        yield return null;
        _acceptSceneReloadEvents = true;
        if (_queueRoutine == null && _pending.Count > 0)
            _queueRoutine = StartCoroutine(ProcessQueue());
        BeginLobbyOpeningSequence();
    }

    private void StopNarrativeTimingCoroutines()
    {
        if (_lobbyIntroCoroutine != null)
        {
            StopCoroutine(_lobbyIntroCoroutine);
            _lobbyIntroCoroutine = null;
        }

        if (_cafeteriaDodgeLineCoroutine != null)
        {
            StopCoroutine(_cafeteriaDodgeLineCoroutine);
            _cafeteriaDodgeLineCoroutine = null;
        }

        if (_cafeteriaTableBabbleCoroutine != null)
        {
            StopCoroutine(_cafeteriaTableBabbleCoroutine);
            _cafeteriaTableBabbleCoroutine = null;
        }

        if (_hallwayScriptCoroutine != null)
        {
            StopCoroutine(_hallwayScriptCoroutine);
            _hallwayScriptCoroutine = null;
        }

        if (_gasWildLineCoroutine != null)
        {
            StopCoroutine(_gasWildLineCoroutine);
            _gasWildLineCoroutine = null;
        }
    }

    private void BeginLobbyOpeningSequence()
    {
        if (!_acceptSceneReloadEvents || !gameObject.activeInHierarchy)
            return;

        if (_lobbyIntroCoroutine != null)
            return;

        _lobbyIntroCancelled = false;
        _lobbyIntroCoroutine = StartCoroutine(CoLobbyOpeningTwoLines());
    }

    private IEnumerator CoLobbyOpeningTwoLines()
    {
        try
        {
            Enqueue("Ughhh… I should've just stayed home today.");
            yield return new WaitForSecondsRealtime(2f);
            if (_lobbyIntroCancelled)
                yield break;
            Enqueue("Why is it so quiet…? This is kinda sus.");
        }
        finally
        {
            _lobbyIntroCoroutine = null;
        }
    }

    private void CancelLobbyIntro()
    {
        _lobbyIntroCancelled = true;
        if (_lobbyIntroCoroutine != null)
        {
            StopCoroutine(_lobbyIntroCoroutine);
            _lobbyIntroCoroutine = null;
        }
    }

    private void Update()
    {
        ResolvePlayerIfNeeded();
        if (_player == null)
            return;

        UpdateZoneFromPosition(_player.position);

        if (PlayerHealth.Instance != null)
        {
            int h = PlayerHealth.Instance.CurrentHearts;
            if (h == 1)
                TryEnqueueUnique("lastLife", "If I mess up now it's actually over.");
        }
    }

    private void BuildUi()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;
        _canvas.overrideSorting = true;

        var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.sizeDelta = new Vector2(0f, bottomBarHeight);
        panelRt.anchoredPosition = Vector2.zero;

        var panelImg = panel.GetComponent<Image>();
        panelImg.sprite = UiSolidSprite();
        panelImg.type = Image.Type.Simple;
        panelImg.color = panelFillPastelYellow;
        panelImg.raycastTarget = false;

        var textGo = new GameObject("DialogueText", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(panel.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(sidePadding, 18f);
        textRt.offsetMax = new Vector2(-sidePadding, -18f);

        _lineText = textGo.GetComponent<Text>();
        _lineText.font = ResolveDialogueFont();
        _lineText.fontSize = fontSize;
        _lineText.color = dialogueTextDarkBlue;
        _lineText.alignment = TextAnchor.MiddleCenter;
        _lineText.fontStyle = FontStyle.Normal;
        _lineText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _lineText.verticalOverflow = VerticalWrapMode.Overflow;
        _lineText.supportRichText = true;
    }

    private static Sprite UiSolidSprite()
    {
        if (_solidUiSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            _solidUiSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _solidUiSprite.name = "NarrativeDialogue_UIBlock";
        }

        return _solidUiSprite;
    }

    private Font ResolveDialogueFont()
    {
        if (!string.IsNullOrWhiteSpace(dialogueFontResourcePath))
        {
            Font f = Resources.Load<Font>(dialogueFontResourcePath.Trim());
            if (f != null)
                return f;
            Debug.LogWarning(
                $"NarrativeDialogueController: Missing font Resources/{dialogueFontResourcePath}.ttf (or .otf). Using built-in Arial fallback.");
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private IEnumerator SpeakLine(string msg)
    {
        if (_lineText != null)
            _lineText.text = msg;
        yield return new WaitForSecondsRealtime(displayHoldSeconds);
        if (_lineText != null)
            _lineText.text = string.Empty;
    }

    public void NotifyFirstMovement()
    {
        TryEnqueuePriorityUnique("first_move", "Alright… let me just move around real quick.");
    }

    /// <summary>Tray SFX only; dodge line is timed from cafeteria entry.</summary>
    public void NotifyFirstTrayThrown()
    {
    }

    public void NotifyFirstCrackingTileExplosion()
    {
        if (!_firedOnce.Add("first_tile"))
            return;
        Enqueue("YO THE FLOOR—");
    }

    public void NotifyFirstGasSpreadBurst()
    {
        if (!_firedOnce.Add("first_gas_visual"))
            return;
        Enqueue("WHAT IS THAT GAS?? UGGGHHH I gotta avoid that");
        if (_gasWildLineCoroutine != null)
            StopCoroutine(_gasWildLineCoroutine);
        _gasWildLineCoroutine = StartCoroutine(CoGasWildFollowUp());
    }

    private IEnumerator CoGasWildFollowUp()
    {
        try
        {
            yield return new WaitForSecondsRealtime(2f);
            Enqueue("Everything is exploding… this is actually wild.");
        }
        finally
        {
            _gasWildLineCoroutine = null;
        }
    }

    public void NotifyGasDamagedPlayer()
    {
        Enqueue("Nah that gas is OP.");
    }

    public void NotifyHoleDamagedPlayer()
    {
        Enqueue("I KNEW I SHOULDN'T HAVE DONE THAT!!");
    }

    public void NotifyPlayerDamaged(NarrativeZone zoneContext)
    {
        if (zoneContext == NarrativeZone.Cafeteria)
            Enqueue("AINT NO WAY I JUST GOT HIT—");
    }

    public void NotifyVictoryExit()
    {
        TryEnqueueUnique("victory_exit_line", "I survived the worst school day ever.");
    }

    private IEnumerator CoCafeteriaDodgeLineAfterEntry()
    {
        try
        {
            yield return new WaitForSecondsRealtime(3f);
            if (_cafeteriaDeparted)
                yield break;
            Enqueue("OH— nah I gotta dodge that—");
        }
        finally
        {
            _cafeteriaDodgeLineCoroutine = null;
        }
    }

    private static readonly string[] CafeteriaTrayBabble =
    {
        "Bro they got aim assist or something??",
        "This is actually insane ",
        "I'm getting jumped by lunch trays…"
    };

    private IEnumerator CoCafeteriaTableBabbleLoop()
    {
        try
        {
            yield return new WaitForSecondsRealtime(2f);
            int i = 0;
            while (!_cafeteriaDeparted)
            {
                Enqueue(CafeteriaTrayBabble[i % CafeteriaTrayBabble.Length]);
                i++;
                yield return new WaitForSecondsRealtime(2f);
            }
        }
        finally
        {
            _cafeteriaTableBabbleCoroutine = null;
        }
    }

    private IEnumerator CoHallwayScriptedChain()
    {
        try
        {
            Enqueue("Alright… this should be easy—");
            yield return new WaitForSecondsRealtime(3f);
            if (_hallwayDeparted)
                yield break;
            Enqueue("……why does every hallway look the same.");
            yield return new WaitForSecondsRealtime(4f);
            if (_hallwayDeparted)
                yield break;
            Enqueue("Nah this is actually a maze. Who designed this??");
            yield return new WaitForSecondsRealtime(3f);
            if (_hallwayDeparted)
                yield break;
            Enqueue("This school is trolling me.");
        }
        finally
        {
            _hallwayScriptCoroutine = null;
        }
    }

    /// <summary>Called from <see cref="DialogueZoneTrigger"/>.</summary>
    public void FireZoneTrigger(DialogueZoneTrigger.NarrativeTriggerKind kind)
    {
        switch (kind)
        {
            case DialogueZoneTrigger.NarrativeTriggerKind.LobbyInterior:
            case DialogueZoneTrigger.NarrativeTriggerKind.LobbyCorridor:
            case DialogueZoneTrigger.NarrativeTriggerKind.CafeteriaNearExit:
            case DialogueZoneTrigger.NarrativeTriggerKind.HallwayMid:
            case DialogueZoneTrigger.NarrativeTriggerKind.ScienceLabMidFloor:
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.LobbyExit:
                CancelLobbyIntro();
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.LobbyDoor:
                CancelLobbyIntro();
                TryEnqueueUnique("lobby_door",
                    "Okay yeah… I definitely gotta get outta here. That door looks like the only way forward… bet.");
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.CafeteriaEntry:
                if (!_firedOnce.Add("cafeteria_entry"))
                    return;
                _cafeteriaDeparted = false;
                Enqueue("……nah. WHY ARE THEY THROWING TRAYS???");
                if (_cafeteriaDodgeLineCoroutine != null)
                    StopCoroutine(_cafeteriaDodgeLineCoroutine);
                _cafeteriaDodgeLineCoroutine = StartCoroutine(CoCafeteriaDodgeLineAfterEntry());
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.CafeteriaFlippedTables:
                if (!_firedOnce.Add("cafeteria_tables"))
                    return;
                Enqueue("Wait… those tables—If I hide behind those I won't get absolutely destroyed… okay big brain.");
                if (_cafeteriaTableBabbleCoroutine != null)
                    StopCoroutine(_cafeteriaTableBabbleCoroutine);
                _cafeteriaTableBabbleCoroutine = StartCoroutine(CoCafeteriaTableBabbleLoop());
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.CafeteriaExit:
                _cafeteriaDeparted = true;
                if (_cafeteriaDodgeLineCoroutine != null)
                {
                    StopCoroutine(_cafeteriaDodgeLineCoroutine);
                    _cafeteriaDodgeLineCoroutine = null;
                }

                if (_cafeteriaTableBabbleCoroutine != null)
                {
                    StopCoroutine(_cafeteriaTableBabbleCoroutine);
                    _cafeteriaTableBabbleCoroutine = null;
                }

                TryEnqueueUnique("cafeteria_exit", "Okay okay just make it to the hallway and I'm free…");
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.HallwayMazeEntry:
                if (!_firedOnce.Add("hall_entry"))
                    return;
                _hallwayDeparted = false;
                if (_hallwayScriptCoroutine != null)
                    StopCoroutine(_hallwayScriptCoroutine);
                _hallwayScriptCoroutine = StartCoroutine(CoHallwayScriptedChain());
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.HallwayExit:
                _hallwayDeparted = true;
                if (_hallwayScriptCoroutine != null)
                {
                    StopCoroutine(_hallwayScriptCoroutine);
                    _hallwayScriptCoroutine = null;
                }

                TryEnqueueUnique("hall_exit", "Finally. I'm free from the backrooms hallway.");
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.ScienceLabEntry:
                if (!_firedOnce.Add("science_lab_entry"))
                    return;
                Enqueue("Okay… why is it red and green in here.");
                ScienceLabHazardPlaybackGate.AllowFromScienceLabEntryTrigger();
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.ScienceLabExit:
                TryEnqueueUnique("science_lab_exit", "And I still have homework.");
                return;

            case DialogueZoneTrigger.NarrativeTriggerKind.ScienceLabNearVictory:
                TryEnqueueUnique("lab_near_victory",
                    "If something hits me now I'm uninstalling life.");
                return;
        }
    }

    private void Enqueue(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _pending.Add(line);
        EnsureQueueRunning();
    }

    private void EnqueuePriority(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _pending.Insert(0, line);
        EnsureQueueRunning();
    }

    private void EnsureQueueRunning()
    {
        if (_queueRoutine == null && gameObject.activeInHierarchy)
            _queueRoutine = StartCoroutine(ProcessQueue());
    }

    private void TryEnqueueUnique(string key, string line)
    {
        if (!_firedOnce.Add(key))
            return;
        Enqueue(line);
    }

    private void TryEnqueuePriorityUnique(string key, string line)
    {
        if (!_firedOnce.Add(key))
            return;
        EnqueuePriority(line);
    }

    private IEnumerator ProcessQueue()
    {
        while (_pending.Count > 0)
        {
            string msg = _pending[0];
            _pending.RemoveAt(0);
            yield return SpeakLine(msg);
            float pause = Mathf.Max(0f, queuePauseAfterLineSeconds);
            if (pause > 0f)
                yield return new WaitForSecondsRealtime(pause);
        }

        _queueRoutine = null;
    }

    private void ResolvePlayerIfNeeded()
    {
        if (_player != null)
            return;
        var go = PlayerSetupFlow.TryResolveRuntimePlayer();
        if (go != null)
            _player = go.transform;
    }

    private void UpdateZoneFromPosition(Vector3 p)
    {
        NarrativeZone z;
        if (p.y < lobbyMaxY)
            z = NarrativeZone.Lobby;
        else if (p.y < cafeteriaMaxY)
            z = NarrativeZone.Cafeteria;
        else if (p.y < hallwayMaxY)
            z = NarrativeZone.Hallway;
        else
            z = NarrativeZone.ScienceLab;

        if (z != _currentZone)
            _currentZone = z;
    }

    public NarrativeZone GetCurrentZone() => _currentZone;
}
