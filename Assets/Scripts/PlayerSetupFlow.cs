using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class PlayerSetupFlow : MonoBehaviour
{
    /// <summary>Square scale for player XY; bump here to resize every spawn/update path.</summary>
    private const float PlayerWorldScale = 0.65f;
    private readonly List<CharacterSprites> characterSpritesList = new List<CharacterSprites>();
    private readonly List<Button> characterButtons = new List<Button>();
    private Font uiFont;

    private GameObject nameRoot;
    private GameObject charRoot;

    private InputField nameInput;
    private Text statusText;
    private Button playButton;
    private Button nameContinueButton;
    private int selectedCharacterIndex = -1;

    private readonly List<GameObject> suppressedGameplayHudRoots = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Debug.Log("PlayerSetupFlow: Bootstrap starting...");

        if (FindFirstObjectByType<PlayerSetupFlow>() != null)
        {
            Debug.Log("PlayerSetupFlow: Already exists, skipping.");
            return;
        }

        var go = new GameObject("PlayerSetupFlow");
        go.AddComponent<PlayerSetupFlow>();
        Debug.Log("PlayerSetupFlow: Created setup flow object.");
    }

    private void Start()
    {
        Debug.Log("PlayerSetupFlow: Start() called.");
        uiFont = CreateFunFont();
        LoadCharacterSpritesFromAssetsFolder();
        Debug.Log($"PlayerSetupFlow: Loaded {characterSpritesList.Count} characters.");
        EnsurePlayerExistsAtStartup();

        if (PlayerSessionData.IsConfigured && PlayerSessionData.SelectedCharacterSprites != null)
        {
            EnterGameplayFromSavedSession();
            return;
        }

        CreateUi();
        ApplySavedSelectionToUi();
        ShowNameScreenOnly();
        Debug.Log("PlayerSetupFlow: UI created successfully.");
    }

    /// <summary>If name and character are already set (e.g. scene reload), skip the title flow.</summary>
    private void EnterGameplayFromSavedSession()
    {
        PlayerSessionData.BeginRun();
        SpawnOrUpdatePlayer(PlayerSessionData.SelectedCharacterSprites);
        var player = FindPlayerObject();
        if (player != null)
            player.transform.position = PlayerSessionData.LobbyWorldPosition;
        EnsureGameplayHudExists();
        Debug.Log("PlayerSetupFlow: Session already configured — skipping name/character screens.");
        Destroy(gameObject);
    }

    private static Sprite LoadSetupSprite(string fileNameWithoutExtension)
    {
        var path = $"background/{fileNameWithoutExtension}";
        var slices = Resources.LoadAll<Sprite>(path);
        if (slices != null && slices.Length > 0)
        {
            return slices[0];
        }

        return Resources.Load<Sprite>(path);
    }

    private void LoadCharacterSpritesFromAssetsFolder()
    {
        var folderPath = Path.Combine(Application.dataPath, "Characters");
        Debug.Log($"PlayerSetupFlow: Looking for characters in: {folderPath}");

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"PlayerSetupFlow: Characters folder not found at {folderPath}");
            return;
        }

        var pngFiles = Directory.GetFiles(folderPath, "*.png");
        Debug.Log($"PlayerSetupFlow: Found {pngFiles.Length} PNG files.");

        foreach (var filePath in pngFiles)
        {
            Debug.Log($"PlayerSetupFlow: Loading character from: {filePath}");
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;

                if (!texture.LoadImage(bytes))
                {
                    Debug.LogWarning($"PlayerSetupFlow: Failed to load image data from {filePath}");
                    continue;
                }

                texture.name = Path.GetFileNameWithoutExtension(filePath);

                var charSprites = SliceSpriteSheet(texture);
                charSprites.Name = texture.name;
                characterSpritesList.Add(charSprites);

                Debug.Log($"PlayerSetupFlow: Successfully loaded character: {charSprites.Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlayerSetupFlow: Error loading {filePath}: {ex.Message}");
            }
        }
    }

    private CharacterSprites SliceSpriteSheet(Texture2D texture)
    {
        int halfWidth = texture.width / 2;
        int halfHeight = texture.height / 2;
        float pixelsPerUnit = 100f;

        var frontSprite = Sprite.Create(
            texture,
            new Rect(0, halfHeight, halfWidth, halfHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
        frontSprite.name = texture.name + "_Front";

        var rightSprite = Sprite.Create(
            texture,
            new Rect(halfWidth, halfHeight, halfWidth, halfHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
        rightSprite.name = texture.name + "_Right";

        var backSprite = Sprite.Create(
            texture,
            new Rect(0, 0, halfWidth, halfHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
        backSprite.name = texture.name + "_Back";

        var leftSprite = Sprite.Create(
            texture,
            new Rect(halfWidth, 0, halfWidth, halfHeight),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
        leftSprite.name = texture.name + "_Left";

        return new CharacterSprites
        {
            Front = frontSprite,
            Right = rightSprite,
            Back = backSprite,
            Left = leftSprite
        };
    }

    private void CreateUi()
    {
        EnsureEventSystem();

        var existingSetupCanvas = GameObject.Find("PlayerSetupCanvas");
        if (existingSetupCanvas != null)
        {
            Destroy(existingSetupCanvas);
        }

        var canvasGo = new GameObject("PlayerSetupCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGo);

        nameRoot = CreateNameScreen(canvasGo.transform);
        charRoot = CreateCharacterScreen(canvasGo.transform);

        nameRoot.SetActive(true);
        charRoot.SetActive(false);

        SuppressSceneGameplayHudForLobby();
    }

    private GameObject CreateNameScreen(Transform canvas)
    {
        var root = new GameObject("NameEntry", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        StretchFull(root.GetComponent<RectTransform>());

        var bgSprite = LoadSetupSprite("Background_EnterName");
        if (bgSprite != null)
        {
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            var img = bg.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning("PlayerSetupFlow: Background_EnterName not found; using solid panel.");
            var panel = new GameObject("FallbackPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            StretchFull(panel.GetComponent<RectTransform>());
            var fallbackImg = panel.GetComponent<Image>();
            fallbackImg.color = new Color(0.1f, 0.12f, 0.22f, 1f);
            fallbackImg.raycastTarget = false;
        }

        // Sit inside the painted name plate (no filled input background — text only over the art).
        nameInput = CreateInputField(
            root.transform,
            "NameInput",
            new Vector2(0f, -22f),
            new Vector2(720f, 96f),
            transparentBackground: true);
        nameInput.onValueChanged.AddListener(OnNameInputChanged);

        nameContinueButton = CreateArtUiButton(
            root.transform,
            "ContinueButton",
            "UI_ContinueButton",
            new Vector2(0f, -340f),
            170f,
            OnNameContinueClicked,
            "CONTINUE");
        // Continue is created after the input and can overlap it in screen space; keep the field on top for clicks/focus.
        nameInput.transform.SetAsLastSibling();
        UpdateNameContinueState();

        return root;
    }

    private GameObject CreateCharacterScreen(Transform canvas)
    {
        var root = new GameObject("CharacterSelect", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        StretchFull(root.GetComponent<RectTransform>());

        var bgSprite = LoadSetupSprite("Background_CharacterSelect");
        if (bgSprite != null)
        {
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            var img = bg.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
        }
        else
        {
            var panel = new GameObject("FallbackPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            StretchFull(panel.GetComponent<RectTransform>());
            var fallbackImg = panel.GetComponent<Image>();
            fallbackImg.color = new Color(0.1f, 0.12f, 0.22f, 1f);
            fallbackImg.raycastTarget = false;
        }

        statusText = CreateText(root.transform, "StatusText", "Choose your character:", 22, new Vector2(0f, 420f), new Vector2(900f, 48f));

        var buttonHolder = new GameObject("CharacterButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonHolder.transform.SetParent(root.transform, false);
        var holderRect = buttonHolder.GetComponent<RectTransform>();
        holderRect.anchorMin = holderRect.anchorMax = new Vector2(0.5f, 0.5f);
        holderRect.anchoredPosition = new Vector2(0f, -100f);
        holderRect.sizeDelta = new Vector2(1000f, 280f);

        var layout = buttonHolder.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (characterSpritesList.Count == 0)
        {
            statusText.text = "No character sprites found in Assets/Characters.";
        }

        for (var i = 0; i < characterSpritesList.Count; i++)
        {
            var charSprites = characterSpritesList[i];
            var index = i;
            var button = CreateCharacterButton(buttonHolder.transform, charSprites.Front, () => SelectCharacter(index));
            characterButtons.Add(button);
        }

        playButton = CreateStartGameArtButton(root.transform, new Vector2(0f, -380f), 220f, ConfirmSelection);
        UpdatePlayButtonState();

        return root;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void ShowNameScreenOnly()
    {
        nameRoot.SetActive(true);
        charRoot.SetActive(false);
        StartCoroutine(FocusNameInputNextFrame());
    }

    private IEnumerator FocusNameInputNextFrame()
    {
        yield return null;
        if (nameInput == null || nameRoot == null || !nameRoot.activeInHierarchy)
        {
            yield break;
        }

        nameInput.interactable = true;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(nameInput.gameObject, null);
        }

        nameInput.Select();
        nameInput.ActivateInputField();
    }

    private void ShowCharacterScreenOnly()
    {
        nameRoot.SetActive(false);
        charRoot.SetActive(true);
        UpdatePlayButtonState();
    }

    private void OnNameContinueClicked()
    {
        if (string.IsNullOrWhiteSpace(nameInput != null ? nameInput.text : string.Empty))
        {
            return;
        }

        ShowCharacterScreenOnly();
    }

    private void OnNameInputChanged(string _)
    {
        if (nameInput == null)
        {
            return;
        }

        var upper = nameInput.text.ToUpperInvariant();
        if (nameInput.text != upper)
        {
            var pos = nameInput.caretPosition;
            nameInput.SetTextWithoutNotify(upper);
            nameInput.caretPosition = Mathf.Min(pos, upper.Length);
        }

        UpdateNameContinueState();
    }

    private void EnsureEventSystem()
    {
        var eventSystem = FindFirstObjectByType<EventSystem>();
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

    private Text CreateText(Transform parent, string objectName, string content, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        var text = textObject.GetComponent<Text>();
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = new Color(0.96f, 0.93f, 0.85f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    private InputField CreateInputField(Transform parent, string objectName, Vector2 anchoredPos, Vector2 size, bool transparentBackground)
    {
        var inputObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        var rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = inputObject.GetComponent<Image>();
        image.sprite = null;
        image.raycastTarget = true;
        // Alpha 0 often drops pointer hits in UI; ~1% keeps the field invisible but clickable.
        if (transparentBackground)
        {
            image.color = new Color(1f, 1f, 1f, 0.01f);
        }
        else
        {
            image.color = new Color(0.95f, 0.92f, 0.82f, 0.98f);
        }

        // Bold, centered caps; dark blue over the tan name plate on the background art.
        var pad = new Vector2(40f, 14f);
        var nameBlue = new Color(0.07f, 0.16f, 0.48f, 1f);
        var text = CreateText(inputObject.transform, "Text", string.Empty, 52, Vector2.zero, size - pad);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = nameBlue;
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;

        var placeholder = CreateText(inputObject.transform, "Placeholder", "YOUR NAME", 48, Vector2.zero, size - pad);
        placeholder.alignment = TextAnchor.MiddleCenter;
        placeholder.color = new Color(nameBlue.r, nameBlue.g, nameBlue.b, 0.5f);
        placeholder.fontStyle = FontStyle.Bold;
        placeholder.raycastTarget = false;

        var field = inputObject.GetComponent<InputField>();
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = 16;
        return field;
    }

    private Button CreateCharacterButton(Transform parent, Sprite sprite, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(sprite.name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 220f);

        var image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = new Color(1f, 1f, 1f, 0.95f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    private Button CreateTextOnlyButton(Transform parent, string objectName, string label, Vector2 anchoredPos, Vector2 size)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.sprite = null;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        var buttonText = CreateText(buttonObject.transform, "Text", label, 28, Vector2.zero, size);
        buttonText.color = new Color(0.94f, 0.9f, 0.78f, 1f);
        buttonText.fontStyle = FontStyle.Bold;
        return button;
    }

    private Button CreateStartGameArtButton(Transform parent, Vector2 anchoredPos, float height, UnityEngine.Events.UnityAction onClick)
    {
        return CreateArtUiButton(parent, "PlayButton", "UI_StartGameButton", anchoredPos, height, onClick, "START GAME");
    }

    /// <summary>Sprite button from Resources/background (transparent PNG — no extra UI box). Falls back to text-only if missing.</summary>
    private Button CreateArtUiButton(
        Transform parent,
        string objectName,
        string resourceNameNoExtension,
        Vector2 anchoredPos,
        float height,
        UnityEngine.Events.UnityAction onClick,
        string fallbackLabel)
    {
        var sprite = LoadSetupSprite(resourceNameNoExtension);
        if (sprite == null)
        {
            Debug.LogWarning($"PlayerSetupFlow: {resourceNameNoExtension} missing; using text fallback.");
            var fb = CreateTextOnlyButton(parent, objectName, fallbackLabel, anchoredPos, new Vector2(420f, 72f));
            fb.onClick.AddListener(onClick);
            return fb;
        }

        var btnGo = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        var w = height * (sprite.rect.width / sprite.rect.height);
        rt.sizeDelta = new Vector2(w, height);

        var img = btnGo.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.type = Image.Type.Simple;
        img.color = Color.white;

        var btn = btnGo.GetComponent<Button>();
        // No color tint — avoids grey/dim overlays on imported button art (continue & start).
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        return btn;
    }

    private void SelectCharacter(int index)
    {
        selectedCharacterIndex = index;
        for (var i = 0; i < characterButtons.Count; i++)
        {
            bool isSelected = i == selectedCharacterIndex;
            var image = characterButtons[i].GetComponent<Image>();
            image.color = isSelected ? Color.white : new Color(0.62f, 0.62f, 0.62f, 0.75f);
            characterButtons[i].transform.localScale = isSelected ? Vector3.one * 1.22f : Vector3.one * 0.88f;
        }

        statusText.text = "Character selected — press Start when ready.";
        UpdatePlayButtonState();
    }

    private void UpdateNameContinueState()
    {
        var ok = !string.IsNullOrWhiteSpace(nameInput != null ? nameInput.text : string.Empty);
        if (nameContinueButton != null)
        {
            nameContinueButton.interactable = ok;
        }
    }

    private void UpdatePlayButtonState()
    {
        var hasName = !string.IsNullOrWhiteSpace(nameInput != null ? nameInput.text : string.Empty);
        var hasCharacter = selectedCharacterIndex >= 0 && selectedCharacterIndex < characterSpritesList.Count;
        if (playButton != null)
        {
            playButton.interactable = hasName && hasCharacter;
        }
    }

    private void ConfirmSelection()
    {
        var name = nameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            statusText.text = "Please enter your name.";
            return;
        }

        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characterSpritesList.Count)
        {
            statusText.text = "Please pick a character.";
            return;
        }

        var selectedCharacter = characterSpritesList[selectedCharacterIndex];
        PlayerSessionData.Set(name, selectedCharacter);
        PlayerSessionData.BeginRun();
        SpawnOrUpdatePlayer(selectedCharacter);

        RestoreSuppressedGameplayHud();
        EnsureGameplayHudExists();

        var canvas = GameObject.Find("PlayerSetupCanvas");
        if (canvas != null)
        {
            Destroy(canvas);
        }

        Destroy(gameObject);
    }

    private void SuppressSceneGameplayHudForLobby()
    {
        suppressedGameplayHudRoots.Clear();
        foreach (var hud in FindObjectsByType<GameHUD>(FindObjectsSortMode.None))
        {
            if (hud == null)
            {
                continue;
            }

            var root = hud.gameObject;
            if (!root.scene.IsValid())
            {
                continue;
            }

            suppressedGameplayHudRoots.Add(root);
            root.SetActive(false);
        }
    }

    private void RestoreSuppressedGameplayHud()
    {
        foreach (var go in suppressedGameplayHudRoots)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        suppressedGameplayHudRoots.Clear();
    }

    private static void EnsureGameplayHudExists()
    {
        if (FindFirstObjectByType<GameHUD>() != null)
        {
            return;
        }

        var hudGo = new GameObject("GameHUD");
        hudGo.AddComponent<Canvas>();
        hudGo.AddComponent<GameHUD>();
    }

    private static void SpawnOrUpdatePlayer(CharacterSprites characterSprites)
    {
        Debug.Log("SpawnOrUpdatePlayer: Starting...");

        var player = FindPlayerObject();
        if (player == null)
        {
            player = new GameObject("Player");
            var spawnPos = GetPlayerSpawnPosition();
            player.transform.position = spawnPos;
            Debug.Log($"SpawnOrUpdatePlayer: Created new Player at position {spawnPos}");
        }
        else
        {
            Debug.Log($"SpawnOrUpdatePlayer: Found existing Player at {player.transform.position}");
        }

        if (player.CompareTag("Untagged"))
        {
            try
            {
                player.tag = "Player";
            }
            catch (UnityException)
            {
            }
        }

        var renderer = player.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = player.AddComponent<SpriteRenderer>();
            Debug.Log("SpawnOrUpdatePlayer: Added SpriteRenderer");
        }

        if (characterSprites.Front == null)
        {
            Debug.LogError("SpawnOrUpdatePlayer: Front sprite is NULL!");
        }
        else
        {
            Debug.Log($"SpawnOrUpdatePlayer: Setting sprite to {characterSprites.Front.name}");
        }

        renderer.sprite = characterSprites.Front;
        renderer.sortingOrder = 100;

        player.transform.localScale = new Vector3(PlayerWorldScale, PlayerWorldScale, 1f);

        Debug.Log($"SpawnOrUpdatePlayer: Player sprite set, sortingOrder=100, scale={PlayerWorldScale}");

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody2D>();
            Debug.Log("SpawnOrUpdatePlayer: Added Rigidbody2D");
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var collider = player.GetComponent<Collider2D>();
        if (collider == null)
        {
            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.5f, 1f);
            Debug.Log("SpawnOrUpdatePlayer: Added CapsuleCollider2D");
        }

        EnsureMovementComponent(player);
        EnsureAnimatorComponent(player, characterSprites);
        EnsureHealthComponent(player);

        Debug.Log($"SpawnOrUpdatePlayer: Complete! Player is at {player.transform.position}");
    }

    private static void EnsureHealthComponent(GameObject player)
    {
        if (player.GetComponent<PlayerHealth>() == null)
            player.AddComponent<PlayerHealth>();
    }

    private static void EnsureMovementComponent(GameObject player)
    {
        if (player.GetComponent<PlayerMovement2D>() == null)
        {
            player.AddComponent<PlayerMovement2D>();
        }
    }

    private static void EnsureAnimatorComponent(GameObject player, CharacterSprites characterSprites)
    {
        var animator = player.GetComponent<PlayerAnimator>();
        if (animator == null)
        {
            animator = player.AddComponent<PlayerAnimator>();
        }

        animator.SetSprites(characterSprites);
    }

    private static GameObject FindPlayerObject()
    {
        GameObject taggedPlayer = null;
        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            // Player tag may not exist in custom project tag settings.
        }

        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        var namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            return namedPlayer;
        }

        var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var transformItem in allTransforms)
        {
            var go = transformItem.gameObject;
            var lowerName = go.name.ToLowerInvariant();
            if (!lowerName.Contains("player"))
            {
                continue;
            }

            // Ignore setup/UI objects that happened to include "player" in their name.
            if (lowerName.Contains("setup") || lowerName.Contains("canvas") || lowerName.Contains("ui"))
            {
                continue;
            }

            // Only treat it as the real player when behavior/components match.
            bool looksLikePlayer =
                go.GetComponent<PlayerMovement2D>() != null ||
                go.GetComponent<PlayerAnimator>() != null ||
                go.GetComponent<PlayerHealth>() != null ||
                (go.GetComponent<SpriteRenderer>() != null && go.GetComponent<Rigidbody2D>() != null);

            if (looksLikePlayer)
            {
                return go;
            }
        }

        return null;
    }

    private static Vector3 GetPlayerSpawnPosition()
    {
        var pos = PlayerSessionData.LobbyWorldPosition;
        Debug.Log($"GetPlayerSpawnPosition: Spawning player at lobby {pos}");
        return pos;
    }

    private static Font CreateFunFont()
    {
        var preferredFonts = new[]
        {
            "Comic Sans MS",
            "Chalkboard SE",
            "Marker Felt",
            "Arial"
        };

        try
        {
            return Font.CreateDynamicFontFromOSFont(preferredFonts, 18);
        }
        catch (System.Exception)
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    private void EnsurePlayerExistsAtStartup()
    {
        var existingPlayer = FindPlayerObject();
        if (existingPlayer != null)
        {
            Debug.Log("PlayerSetupFlow: Found existing Player object in scene.");
            if (PlayerSessionData.SelectedCharacterSprites != null && PlayerSessionData.SelectedCharacterSprites.Front != null)
            {
                SpawnOrUpdatePlayer(PlayerSessionData.SelectedCharacterSprites);
                return;
            }

            if (!string.IsNullOrWhiteSpace(PlayerSessionData.SelectedCharacterName))
            {
                for (var i = 0; i < characterSpritesList.Count; i++)
                {
                    if (string.Equals(characterSpritesList[i].Name, PlayerSessionData.SelectedCharacterName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        SpawnOrUpdatePlayer(characterSpritesList[i]);
                        return;
                    }
                }
            }

            return;
        }

        Debug.Log("PlayerSetupFlow: No Player found, creating one...");

        if (PlayerSessionData.SelectedCharacterSprites != null && PlayerSessionData.SelectedCharacterSprites.Front != null)
        {
            SpawnOrUpdatePlayer(PlayerSessionData.SelectedCharacterSprites);
            return;
        }

        if (!string.IsNullOrWhiteSpace(PlayerSessionData.SelectedCharacterName))
        {
            for (var i = 0; i < characterSpritesList.Count; i++)
            {
                if (string.Equals(characterSpritesList[i].Name, PlayerSessionData.SelectedCharacterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    SpawnOrUpdatePlayer(characterSpritesList[i]);
                    return;
                }
            }
        }

        if (characterSpritesList.Count > 0)
        {
            SpawnOrUpdatePlayer(characterSpritesList[0]);
            return;
        }

        var placeholderTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.95f, 0.85f, 0.2f, 1f);
        }

        placeholderTexture.SetPixels(pixels);
        placeholderTexture.Apply();
        placeholderTexture.name = "Player Placeholder";

        var placeholderSprite = Sprite.Create(
            placeholderTexture,
            new Rect(0, 0, placeholderTexture.width, placeholderTexture.height),
            new Vector2(0.5f, 0.5f),
            64f
        );
        placeholderSprite.name = "Player Placeholder";

        var placeholderCharacter = new CharacterSprites
        {
            Front = placeholderSprite,
            Back = placeholderSprite,
            Left = placeholderSprite,
            Right = placeholderSprite,
            Name = "Placeholder"
        };
        SpawnOrUpdatePlayer(placeholderCharacter);
    }

    private void ApplySavedSelectionToUi()
    {
        if (nameInput != null && !string.IsNullOrWhiteSpace(PlayerSessionData.PlayerName))
        {
            nameInput.text = PlayerSessionData.PlayerName.ToUpperInvariant();
            UpdateNameContinueState();
        }

        if (string.IsNullOrWhiteSpace(PlayerSessionData.SelectedCharacterName))
        {
            UpdatePlayButtonState();
            return;
        }

        for (var i = 0; i < characterSpritesList.Count; i++)
        {
            if (string.Equals(characterSpritesList[i].Name, PlayerSessionData.SelectedCharacterName, System.StringComparison.OrdinalIgnoreCase))
            {
                SelectCharacter(i);
                return;
            }
        }

        UpdatePlayButtonState();
    }
}
