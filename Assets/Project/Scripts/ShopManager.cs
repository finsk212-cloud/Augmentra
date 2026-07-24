using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public TMP_FontAsset font;

    [Header("Panel Size")]
    public float panelWidth = 1040f;
    public float panelHeight = 760f;
    public float headerHeight = 46f;
    public float outerMargin = 12f;

    [Header("Columns")]
    public float leftColumnWidth = 700f;
    public float rightColumnWidth = 300f;

    [Header("Grid")]
    public Vector2 cellSize = new Vector2(78f, 78f);
    public Vector2 cellSpacing = new Vector2(8f, 8f);
    public int gridColumns = 6;
    public int contentPaddingLeft = 18;
    public int contentPaddingTop = 18;
    public float sectionSpacing = 16f;

    [Header("Font Sizes")]
    public float titleFontSize = 24f;
    public float sectionFontSize = 14f;
    public float costFontSize = 11f;
    public float detailNameFontSize = 17f;
    public float statFontSize = 13f;
    public float passiveDescFontSize = 12f;

    [Header("Colors")]
    public Color panelColor = Hex("#0A0B0E");
    public Color leftColor = Hex("#0A0B0E");
    public Color rightColor = Hex("#0D0E12");
    public Color cellColor = Hex("#13141A");
    public Color costColor = Hex("#9A7A2A");
    public Color purchaseColor = Hex("#1A3A1A");
    public Color purchaseBorderColor = Hex("#2A5A2A");

    private static readonly Color EpicColor = Hex("#9B7FD4");
    private static readonly Color ExcellentColor = Hex("#D4A537");
    private static readonly Color BootsColor = Hex("#888888");
    private static readonly Color ElixirColor = Hex("#4FA8A8");
    private static readonly Color OrbColor = Hex("#4FA84F");

    private const string CanvasName = "ShopCanvas";
    private const string FontResourcePath = "Fonts/Nexa-ExtraLight SDF";

    private static readonly Color NameColor = Hex("#F0F0F4");
    private static readonly Color SubtleText = Hex("#666666");
    private static readonly Color Divider = Hex("#2A2A35");
    private static readonly Color PassiveColor = Hex("#8A5020");
    private static readonly Color DescGrey = Hex("#888888");
    private static readonly Color OwnedBg = Hex("#0A0A0E");
    private static readonly Color PurchaseDim = Hex("#15171A");

    private static readonly string[] GroupKeys = { "EPIC", "EXCELLENT", "BOOTS", "ELIXIRS", "ORBS" };

    private class Card
    {
        public ItemSO item;
        public Image border;
        public Image bg;
        public TextMeshProUGUI costText;
        public Color borderBase;
        public bool purchased;
    }

    private readonly List<ItemSO> items = new List<ItemSO>();
    private readonly List<Card> cards = new List<Card>();

    private GameObject canvasRoot;
    private GameObject breakRoot;
    private GameObject shopPanel;
    private Transform content;
    private TextMeshProUGUI goldText;

    private GameObject placeholder;
    private GameObject detailTop;
    private Image rIcon;
    private TextMeshProUGUI rName;
    private TextMeshProUGUI rCost;
    private Transform statsContainer;
    private TextMeshProUGUI rPassiveLabel;
    private TextMeshProUGUI rPassiveDesc;
    private GameObject purchaseButton;
    private Image purchaseBg;
    private TextMeshProUGUI purchaseLabel;

    private bool breakActive;
    private bool shopOpen;
    private bool ready;
    private Card selectedCard;

    public bool IsShopOpen
    {
        get { return shopOpen; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        }

        LoadItems();
        EnsureEventSystem();
        BuildUI();

        breakRoot.SetActive(false);
        shopPanel.SetActive(false);
        ready = true;
    }

    private void Update()
    {
        if (!ready || !breakActive) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleShop();
        }

        if (shopOpen)
        {
            RefreshGold();
            UpdatePurchaseState();
        }
    }

    public void OpenBreak()
    {
        if (!ready) return;

        breakActive = true;
        shopOpen = false;
        Time.timeScale = 1f;

        breakRoot.SetActive(true);
        shopPanel.SetActive(false);
    }

    private void EndBreak()
    {
        breakActive = false;
        shopOpen = false;
        Time.timeScale = 1f;

        breakRoot.SetActive(false);
        shopPanel.SetActive(false);

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.StartCountdown();
        }
    }

    private void ToggleShop()
    {
        shopOpen = !shopOpen;
        shopPanel.SetActive(shopOpen);

        if (shopOpen)
        {
            RefreshGold();
            ClearSelection();
        }
    }

    private void LoadItems()
    {
        items.Clear();
        items.AddRange(Resources.LoadAll<ItemSO>("Items"));
    }

    private void BuildUI()
    {
        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            Destroy(existing);
        }

        canvasRoot = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBreakRoot();
        BuildShopPanel();
    }

    private void BuildBreakRoot()
    {
        breakRoot = NewUI("BreakRoot", canvasRoot.transform);
        Stretch(breakRoot.GetComponent<RectTransform>());

        TextMeshProUGUI hint = AddText("BreakHint", breakRoot.transform, "Press  B  to open shop", 26f, FontStyles.Normal, costColor);
        SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(700f, 40f));
    }

    private void BuildShopPanel()
    {
        shopPanel = NewUI("ShopPanel", canvasRoot.transform);
        Stretch(shopPanel.GetComponent<RectTransform>());

        GameObject dim = NewUI("Overlay", shopPanel.transform);
        Stretch(dim.GetComponent<RectTransform>());
        AddImage(dim, new Color(0f, 0f, 0f, 0f), null, false);

        GameObject box = NewUI("PanelBox", shopPanel.transform);
        SetRect(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(panelWidth, panelHeight));
        AddImage(box, panelColor, null, false);

        float topInset = outerMargin + headerHeight + 10f;
        float bottomInset = outerMargin;
        float colHeight = -(topInset + bottomInset);
        float colYOffset = (bottomInset - topInset) * 0.5f;

        BuildHeader(box.transform);
        BuildLeftColumn(box.transform, colYOffset, colHeight);
        BuildRightColumn(box.transform, colYOffset, colHeight);

        ClearSelection();
    }

    private void BuildHeader(Transform box)
    {
        GameObject header = NewUI("Header", box);
        SetRect(header.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -outerMargin), new Vector2(-outerMargin * 2f, headerHeight));

        TextMeshProUGUI title = AddText("Title", header.transform, "SHOP", titleFontSize, FontStyles.Bold, NameColor);
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(120f, 32f));
        title.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI hint = AddText("CloseHint", header.transform, "Press  B  to close", 12f, FontStyles.Normal, SubtleText);
        SetRect(hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, 0f), new Vector2(180f, 20f));
        hint.alignment = TextAlignmentOptions.Left;

        GameObject readyGo = NewUI("ReadyButton", header.transform);
        SetRect(readyGo.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(120f, 34f));
        Image readyBg = AddImage(readyGo, purchaseColor, null, false);
        Outline readyOutline = readyGo.AddComponent<Outline>();
        readyOutline.effectColor = purchaseBorderColor;
        readyOutline.effectDistance = new Vector2(1f, -1f);
        Button readyButton = readyGo.AddComponent<Button>();
        readyButton.targetGraphic = readyBg;
        readyButton.onClick.AddListener(EndBreak);
        TextMeshProUGUI readyLabel = AddText("ReadyLabel", readyGo.transform, "READY", 16f, FontStyles.Bold, NameColor);
        Stretch(readyLabel.rectTransform);

        GameObject goldHolder = NewUI("Gold", header.transform);
        SetRect(goldHolder.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-142f, 0f), new Vector2(120f, 28f));
        HorizontalLayoutGroup goldLayout = goldHolder.AddComponent<HorizontalLayoutGroup>();
        goldLayout.spacing = 6f;
        goldLayout.childAlignment = TextAnchor.MiddleRight;
        goldLayout.childControlWidth = false;
        goldLayout.childControlHeight = false;
        goldLayout.childForceExpandWidth = false;
        goldLayout.childForceExpandHeight = false;

        GameObject coin = NewUI("Coin", goldHolder.transform);
        coin.GetComponent<RectTransform>().sizeDelta = new Vector2(18f, 18f);
        AddImage(coin, costColor, Knob(), false);

        goldText = AddText("GoldText", goldHolder.transform, "0", 18f, FontStyles.Bold, costColor);
        goldText.rectTransform.sizeDelta = new Vector2(90f, 24f);
        goldText.alignment = TextAlignmentOptions.Right;
    }

    private void BuildLeftColumn(Transform box, float colYOffset, float colHeight)
    {
        GameObject left = NewUI("LeftColumn", box);
        SetRect(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(outerMargin, colYOffset), new Vector2(leftColumnWidth, colHeight));
        AddImage(left, leftColor, null, false);

        ScrollRect sr = left.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 28f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = NewUI("Viewport", left.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        GameObject contentGo = NewUI("Content", viewport.transform);
        RectTransform contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = sectionSpacing;
        contentLayout.padding = new RectOffset(contentPaddingLeft, 8, contentPaddingTop, 12);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = contentRect;
        content = contentGo.transform;

        BuildSections();
    }

    private void BuildSections()
    {
        for (int g = 0; g < GroupKeys.Length; g++)
        {
            string key = GroupKeys[g];

            GameObject section = NewUI("Section_" + key, content);
            VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 6f;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            TextMeshProUGUI header = AddText("Header", section.transform, key, sectionFontSize, FontStyles.Bold, GroupColor(key));
            header.alignment = TextAlignmentOptions.Left;
            header.characterSpacing = 4f;
            LayoutElement headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.preferredHeight = sectionFontSize + 6f;
            headerElement.minHeight = sectionFontSize + 6f;

            GameObject grid = NewUI("Grid", section.transform);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(cellSize.x, cellSize.y + costFontSize + 12f);
            gridLayout.spacing = cellSpacing;
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = Mathf.Max(1, gridColumns);

            foreach (ItemSO item in items)
            {
                if (GroupOf(item) == key)
                {
                    BuildCard(item, grid.transform, GroupColor(key));
                }
            }
        }
    }

    private void BuildCard(ItemSO item, Transform parent, Color borderColor)
    {
        GameObject cardGo = NewUI("Card_" + item.itemName, parent);
        Card card = new Card();
        card.item = item;
        card.borderBase = borderColor;

        GameObject squareGo = NewUI("Square", cardGo.transform);
        RectTransform squareRect = squareGo.GetComponent<RectTransform>();
        squareRect.anchorMin = new Vector2(0.5f, 1f);
        squareRect.anchorMax = new Vector2(0.5f, 1f);
        squareRect.pivot = new Vector2(0.5f, 1f);
        squareRect.anchoredPosition = Vector2.zero;
        squareRect.sizeDelta = new Vector2(cellSize.x, cellSize.y);
        card.border = AddImage(squareGo, borderColor, null, false);

        GameObject bgGo = NewUI("Bg", squareGo.transform);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        Stretch(bgRect);
        bgRect.offsetMin = new Vector2(1f, 1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        card.bg = AddImage(bgGo, cellColor, null, false);
        card.bg.raycastTarget = false;

        GameObject iconGo = NewUI("Icon", bgGo.transform);
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        Stretch(iconRect);
        iconRect.offsetMin = new Vector2(2f, 2f);
        iconRect.offsetMax = new Vector2(-2f, -2f);
        Image icon = AddImage(iconGo, NameColor, item.icon, false);
        icon.preserveAspect = true;
        icon.enabled = item.icon != null;
        icon.raycastTarget = false;

        card.costText = AddText("Cost", cardGo.transform, item.cost.ToString(), costFontSize, FontStyles.Bold, costColor);
        RectTransform costRect = card.costText.rectTransform;
        costRect.anchorMin = new Vector2(0f, 0f);
        costRect.anchorMax = new Vector2(1f, 0f);
        costRect.pivot = new Vector2(0.5f, 0f);
        costRect.anchoredPosition = Vector2.zero;
        costRect.sizeDelta = new Vector2(0f, costFontSize + 6f);
        card.costText.alignment = TextAlignmentOptions.Center;

        Card captured = card;
        AddTrigger(squareGo, EventTriggerType.PointerEnter, () => OnCardEnter(captured));
        AddTrigger(squareGo, EventTriggerType.PointerExit, () => OnCardExit(captured));
        AddTrigger(squareGo, EventTriggerType.PointerClick, () => SelectCard(captured));

        cards.Add(card);
    }

    private void BuildRightColumn(Transform box, float colYOffset, float colHeight)
    {
        GameObject right = NewUI("RightColumn", box);
        SetRect(right.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-outerMargin, colYOffset), new Vector2(rightColumnWidth, colHeight));
        AddImage(right, rightColor, null, false);

        placeholder = AddText("Placeholder", right.transform, "Select an item", 14f, FontStyles.Normal, SubtleText).gameObject;
        Stretch(placeholder.GetComponent<RectTransform>());

        detailTop = NewUI("DetailTop", right.transform);
        RectTransform detailRect = detailTop.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0f, 1f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.pivot = new Vector2(0.5f, 1f);
        detailRect.offsetMin = new Vector2(12f, 0f);
        detailRect.offsetMax = new Vector2(-12f, -12f);
        VerticalLayoutGroup detailLayout = detailTop.AddComponent<VerticalLayoutGroup>();
        detailLayout.spacing = 6f;
        detailLayout.childAlignment = TextAnchor.UpperLeft;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;
        ContentSizeFitter detailFitter = detailTop.AddComponent<ContentSizeFitter>();
        detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject iconRow = NewUI("IconRow", detailTop.transform);
        LayoutElement iconRowElement = iconRow.AddComponent<LayoutElement>();
        iconRowElement.preferredHeight = 86f;
        iconRowElement.minHeight = 86f;
        GameObject iconGo = NewUI("Icon", iconRow.transform);
        SetRect(iconGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
        rIcon = AddImage(iconGo, NameColor, null, false);
        rIcon.preserveAspect = true;

        rName = AddText("Name", detailTop.transform, "", detailNameFontSize, FontStyles.Bold, NameColor);
        rCost = AddText("Cost", detailTop.transform, "", 13f, FontStyles.Bold, costColor);

        GameObject divider = NewUI("Divider", detailTop.transform);
        LayoutElement dividerElement = divider.AddComponent<LayoutElement>();
        dividerElement.preferredHeight = 2f;
        dividerElement.minHeight = 2f;
        AddImage(divider, Divider, null, false);

        GameObject stats = NewUI("Stats", detailTop.transform);
        VerticalLayoutGroup statsLayout = stats.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 4f;
        statsLayout.childAlignment = TextAnchor.UpperLeft;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;
        statsContainer = stats.transform;

        rPassiveLabel = AddText("PassiveLabel", detailTop.transform, "PASSIVE", 12f, FontStyles.Bold, PassiveColor);
        rPassiveLabel.alignment = TextAlignmentOptions.Left;
        rPassiveLabel.characterSpacing = 3f;

        rPassiveDesc = AddText("PassiveDesc", detailTop.transform, "", passiveDescFontSize, FontStyles.Normal, DescGrey);
        rPassiveDesc.alignment = TextAlignmentOptions.TopLeft;

        purchaseButton = NewUI("PurchaseButton", right.transform);
        SetRect(purchaseButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-24f, 42f));
        purchaseBg = AddImage(purchaseButton, purchaseColor, null, false);
        Outline purchaseOutline = purchaseButton.AddComponent<Outline>();
        purchaseOutline.effectColor = purchaseBorderColor;
        purchaseOutline.effectDistance = new Vector2(1f, -1f);
        Button buy = purchaseButton.AddComponent<Button>();
        buy.targetGraphic = purchaseBg;
        buy.onClick.AddListener(PurchaseSelected);
        purchaseLabel = AddText("PurchaseLabel", purchaseButton.transform, "PURCHASE", 15f, FontStyles.Bold, NameColor);
        Stretch(purchaseLabel.rectTransform);
    }

    private void OnCardEnter(Card card)
    {
        if (!card.purchased && card != selectedCard)
        {
            card.bg.color = Brighten(cellColor, 0.06f);
        }
    }

    private void OnCardExit(Card card)
    {
        if (!card.purchased && card != selectedCard)
        {
            card.bg.color = cellColor;
        }
    }

    private void SelectCard(Card card)
    {
        if (selectedCard != null && !selectedCard.purchased)
        {
            selectedCard.bg.color = cellColor;
            selectedCard.border.color = selectedCard.borderBase;
        }

        selectedCard = card;
        card.border.color = Brighten(card.borderBase, 0.35f);

        if (!card.purchased)
        {
            card.bg.color = Brighten(cellColor, 0.06f);
        }

        ShowDetail(card);
    }

    private void ClearSelection()
    {
        selectedCard = null;

        if (placeholder != null) placeholder.SetActive(true);
        if (detailTop != null) detailTop.SetActive(false);
        if (purchaseButton != null) purchaseButton.SetActive(false);
    }

    private void ShowDetail(Card card)
    {
        ItemSO item = card.item;

        placeholder.SetActive(false);
        detailTop.SetActive(true);
        purchaseButton.SetActive(true);

        rIcon.sprite = item.icon;
        rIcon.enabled = item.icon != null;
        rName.text = item.itemName;
        rCost.text = item.cost + " g";

        for (int i = statsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(statsContainer.GetChild(i).gameObject);
        }

        if (item.stats != null)
        {
            foreach (ItemSO.ItemStat stat in item.stats)
            {
                BuildStatRow(stat);
            }
        }

        bool hasPassive = !string.IsNullOrEmpty(item.passiveName) || !string.IsNullOrEmpty(item.passiveDescription);
        rPassiveLabel.gameObject.SetActive(hasPassive);
        rPassiveDesc.gameObject.SetActive(hasPassive);
        rPassiveDesc.text = item.passiveDescription;

        UpdatePurchaseState();
    }

    private void BuildStatRow(ItemSO.ItemStat stat)
    {
        GameObject row = NewUI("StatRow", statsContainer);
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredHeight = statFontSize + 6f;
        element.minHeight = statFontSize + 6f;

        GameObject square = NewUI("Square", row.transform);
        SetRect(square.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(14f, 14f));
        AddImage(square, Divider, null, false);

        string label = string.IsNullOrEmpty(stat.statLabel) ? StatName(stat.statType) : stat.statLabel;
        string shown = FormatValue(stat.statType, stat.statValue);
        TextMeshProUGUI text = AddText("Text", row.transform, "<color=#FFFFFF>" + shown + "</color>  <color=#666666>" + label + "</color>", statFontSize, FontStyles.Normal, NameColor);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(20f, 0f);
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Left;
    }

    private void UpdatePurchaseState()
    {
        if (selectedCard == null || !purchaseButton.activeSelf) return;

        Button button = purchaseButton.GetComponent<Button>();

        if (selectedCard.purchased)
        {
            button.interactable = false;
            purchaseBg.color = PurchaseDim;
            purchaseLabel.text = "OWNED";
            return;
        }

        int gold = GameManager.Instance != null ? GameManager.Instance.gold : 0;
        bool afford = gold >= selectedCard.item.cost;
        button.interactable = afford;
        purchaseBg.color = afford ? purchaseColor : PurchaseDim;
        purchaseLabel.text = "PURCHASE";
    }

    private void PurchaseSelected()
    {
        if (selectedCard == null || selectedCard.purchased) return;
        if (!Buy(selectedCard.item)) return;

        selectedCard.purchased = true;
        selectedCard.bg.color = OwnedBg;
        selectedCard.costText.text = "OWNED";
        selectedCard.costText.color = SubtleText;
        UpdatePurchaseState();
    }

    private bool Buy(ItemSO item)
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.gold : 0;

        if (gold < item.cost)
        {
            return false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.gold -= item.cost;
        }

        ApplyItem(item);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.AddItemToInventory(item);
        }

        RefreshGold();
        return true;
    }

    private void ApplyItem(ItemSO item)
    {
        if (item.stats == null) return;

        foreach (ItemSO.ItemStat stat in item.stats)
        {
            ApplyStat(stat.statType, stat.statValue);
        }
    }

    private void ApplyStat(ItemSO.StatType type, float value)
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return;

        Health h = p.GetComponent<Health>();

        switch (type)
        {
            case ItemSO.StatType.AttackDamage:
                p.attackDamage += value;
                break;
            case ItemSO.StatType.Health:
                if (h != null) h.AddMaxHealth(value);
                break;
            case ItemSO.StatType.AttackSpeed:
                p.attackSpeed = Mathf.Max(0.05f, p.attackSpeed - value);
                break;
            case ItemSO.StatType.MovementSpeed:
                p.movementSpeed += value;
                break;
            case ItemSO.StatType.AbilityPower:
                p.abilityPower += value;
                break;
            case ItemSO.StatType.HealthRegen:
                p.healthRegen += value;
                break;
            case ItemSO.StatType.Mana:
                p.AddMaxMana(value);
                break;
            case ItemSO.StatType.ManaRegen:
                p.manaRegen += value;
                break;
            case ItemSO.StatType.Armor:
                p.armor += value;
                break;
            case ItemSO.StatType.MagicDefense:
                p.magicDefense += value;
                break;
        }
    }

    private void RefreshGold()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.gold : 0;
        goldText.text = gold.ToString();
    }

    private string GroupOf(ItemSO item)
    {
        if (item.category == ItemSO.Category.Boots) return "BOOTS";
        if (item.category == ItemSO.Category.Elixir) return "ELIXIRS";
        if (item.category == ItemSO.Category.Orb) return "ORBS";
        if (item.rarity == ItemSO.Rarity.Excellent) return "EXCELLENT";
        return "EPIC";
    }

    private Color GroupColor(string key)
    {
        switch (key)
        {
            case "EPIC": return EpicColor;
            case "EXCELLENT": return ExcellentColor;
            case "BOOTS": return BootsColor;
            case "ELIXIRS": return ElixirColor;
            case "ORBS": return OrbColor;
        }

        return BootsColor;
    }

    private string StatName(ItemSO.StatType type)
    {
        switch (type)
        {
            case ItemSO.StatType.AttackDamage: return "Attack Damage";
            case ItemSO.StatType.Health: return "Health";
            case ItemSO.StatType.AttackSpeed: return "Attack Speed";
            case ItemSO.StatType.MovementSpeed: return "Movement Speed";
            case ItemSO.StatType.AbilityPower: return "Ability Power";
            case ItemSO.StatType.HealthRegen: return "Health Regeneration";
            case ItemSO.StatType.Mana: return "Mana";
            case ItemSO.StatType.ManaRegen: return "Mana Regeneration";
            case ItemSO.StatType.Armor: return "Armor";
            case ItemSO.StatType.MagicDefense: return "Magic Defense";
        }

        return type.ToString();
    }

    private string FormatValue(ItemSO.StatType type, float value)
    {
        string sign = value >= 0f ? "+" : "-";
        float abs = Mathf.Abs(value);

        if (type == ItemSO.StatType.MovementSpeed || type == ItemSO.StatType.AttackSpeed)
        {
            return sign + abs.ToString("0.##") + "%";
        }

        return sign + abs.ToString("0.##");
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void AddTrigger(GameObject go, EventTriggerType type, Action callback)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = go.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(data => callback());
        trigger.triggers.Add(entry);
    }

    private GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private Image AddImage(GameObject go, Color color, Sprite sprite, bool sliced)
    {
        Image image = go.AddComponent<Image>();
        image.color = color;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        return image;
    }

    private TextMeshProUGUI AddText(string name, Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject go = NewUI(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

        if (font != null)
        {
            tmp.font = font;
        }

        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite Knob()
    {
        return null;
    }

    private static Color Brighten(Color c, float amount)
    {
        return new Color(Mathf.Clamp01(c.r + amount), Mathf.Clamp01(c.g + amount), Mathf.Clamp01(c.b + amount), c.a);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static Color HexA(string hex, float alpha)
    {
        Color color = Hex(hex);
        color.a = alpha;
        return color;
    }
}
