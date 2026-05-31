using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// HUD管理器 - 运行时动态创建UI
    /// 包含: 血条/蓝条/快捷栏/背包面板/合成面板/拾取提示
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        private Canvas _canvas;
        private Image _healthFill;
        private Image _manaFill;
        private Text _healthText;
        private Text _coordText;

        // ===== 快捷栏 =====
        private GameObject _hotbarContainer;
        private Image[] _hotbarSlotBgs;
        private Image[] _hotbarItemIcons;
        private Text[] _hotbarCountTexts;
        private Image[] _hotbarBorders;

        // ===== 背包面板 =====
        private GameObject _inventoryPanel;
        private Image[] _invSlotBgs;
        private Image[] _invItemIcons;
        private Text[] _invCountTexts;
        private int _selectedInvSlot = -1;

        // ===== 合成面板 =====
        private GameObject _craftingPanel;
        private GameObject _recipeListContent;
        private GameObject _recipeDetailPanel;
        private Text _detailNameText;
        private Image _detailColorBlock;
        private GameObject _materialsListContent;
        private Button _craftButton;
        private Text _craftButtonText;
        private int _selectedRecipeIndex = -1;
        private List<GameObject> _recipeEntries = new List<GameObject>();

        // ===== 拾取提示 =====
        private List<PickupToast> _pickupToasts = new List<PickupToast>();

        // ===== 颜色主题 =====
        private static readonly Color CyberCyan = new Color(0f, 0.9f, 0.85f, 1f);
        private static readonly Color CyberDarkBlue = new Color(0.05f, 0.08f, 0.15f, 0.95f);
        private static readonly Color CyberBorder = new Color(0f, 0.6f, 0.7f, 0.8f);
        private static readonly Color CyberHighlight = new Color(0f, 1f, 0.9f, 1f);
        private static readonly Color CyberDimBorder = new Color(0.1f, 0.2f, 0.3f, 0.7f);
        private static readonly Color PanelBG = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        private Font _font;
        private bool _inventoryOpen;
        private bool _craftingOpen;

        private void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateCanvas();
            CreateHealthBar();
            CreateManaBar();
            CreateInfoText();
            CreateHotbar();
            CreateInventoryPanel();
            CreateCraftingPanel();
        }

        private void Start()
        {
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnHealthChanged += UpdateHealthBar;
                PlayerStats.Instance.OnManaChanged += UpdateManaBar;
            }
            if (Inventory.Instance != null)
            {
                Inventory.Instance.OnInventoryChanged += RefreshHotbar;
                Inventory.Instance.OnInventoryChanged += RefreshInventoryPanel;
                Inventory.Instance.OnHotbarSelected += OnHotbarSelectionChanged;
                Inventory.Instance.OnItemPickedUp += ShowPickupToast;
            }
            RefreshHotbar();
        }

        private void Update()
        {
            // 坐标信息
            if (PlayerController.Instance != null && _coordText != null)
            {
                var pos = PlayerController.Instance.GetTilePosition();
                var gm = GameManager.Instance;
                string timeStr = "";
                var dayNight = FindObjectOfType<DayNightCycle>();
                if (dayNight != null) timeStr = dayNight.GetTimeString();
                _coordText.text = $"[{pos.x},{pos.y}] {timeStr} Day:{gm?.dayCount ?? 0}";
            }

            // 按键处理
            if (Input.GetKeyDown(KeyCode.E))
                ToggleInventory();
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleCrafting();
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseAllPanels();

            // 更新拾取提示
            UpdatePickupToasts();
            // 刷新快捷栏高亮
            UpdateHotbarHighlight();
        }

        // ========== 面板控制 ==========

        private void ToggleInventory()
        {
            _inventoryOpen = !_inventoryOpen;
            _inventoryPanel.SetActive(_inventoryOpen);
            if (_inventoryOpen)
            {
                _selectedInvSlot = -1;
                RefreshInventoryPanel();
            }
            UpdateInputBlock();
        }

        private void ToggleCrafting()
        {
            _craftingOpen = !_craftingOpen;
            _craftingPanel.SetActive(_craftingOpen);
            if (_craftingOpen)
            {
                _selectedRecipeIndex = -1;
                RefreshCraftingPanel();
            }
            UpdateInputBlock();
        }

        private void CloseAllPanels()
        {
            _inventoryOpen = false;
            _craftingOpen = false;
            _inventoryPanel.SetActive(false);
            _craftingPanel.SetActive(false);
            _selectedInvSlot = -1;
            _selectedRecipeIndex = -1;
            UpdateInputBlock();
        }

        private void UpdateInputBlock()
        {
            bool anyOpen = _inventoryOpen || _craftingOpen;
            if (PlayerController.Instance != null)
                PlayerController.Instance.InputEnabled = !anyOpen;
        }

        // ========== Canvas & 基础HUD ==========

        private void CreateCanvas()
        {
            var canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem for UI clicks
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
        }

        private void CreateHealthBar()
        {
            var bgObj = CreateUIImage("HealthBarBG", new Vector2(110, 18),
                new Vector2(10, -10), new Color(0.1f, 0.02f, 0.02f, 0.9f));
            bgObj.transform.SetParent(_canvas.transform, false);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 1);
            bgRT.anchorMax = new Vector2(0, 1);
            bgRT.pivot = new Vector2(0, 1);

            var fillObj = CreateUIImage("HealthFill", new Vector2(106, 14),
                new Vector2(2, -2), new Color(0.8f, 0.1f, 0.1f, 1f));
            fillObj.transform.SetParent(bgObj.transform, false);
            _healthFill = fillObj.GetComponent<Image>();
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = new Vector2(2, 2);
            fillRT.offsetMax = new Vector2(-2, -2);

            var textObj = new GameObject("HPText");
            textObj.transform.SetParent(bgObj.transform, false);
            _healthText = textObj.AddComponent<Text>();
            _healthText.text = "100/100";
            _healthText.font = _font;
            _healthText.fontSize = 10;
            _healthText.color = new Color(1f, 0.7f, 0.7f);
            _healthText.alignment = TextAnchor.MiddleCenter;
            var textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void CreateManaBar()
        {
            var bgObj = CreateUIImage("ManaBarBG", new Vector2(90, 14),
                new Vector2(10, -32), new Color(0.02f, 0.02f, 0.1f, 0.9f));
            bgObj.transform.SetParent(_canvas.transform, false);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 1);
            bgRT.anchorMax = new Vector2(0, 1);
            bgRT.pivot = new Vector2(0, 1);

            var fillObj = CreateUIImage("ManaFill", new Vector2(86, 10),
                new Vector2(2, -2), new Color(0.2f, 0.2f, 0.9f, 1f));
            fillObj.transform.SetParent(bgObj.transform, false);
            _manaFill = fillObj.GetComponent<Image>();
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = new Vector2(2, 2);
            fillRT.offsetMax = new Vector2(-2, -2);
        }

        private void CreateInfoText()
        {
            var textObj = new GameObject("InfoText");
            textObj.transform.SetParent(_canvas.transform, false);
            _coordText = textObj.AddComponent<Text>();
            _coordText.font = _font;
            _coordText.fontSize = 11;
            _coordText.color = new Color(0f, 0.9f, 0.7f, 0.8f);
            _coordText.alignment = TextAnchor.UpperCenter;
            var rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -8);
            rt.sizeDelta = new Vector2(300, 20);
        }

        // ========== A. 快捷栏 ==========

        private void CreateHotbar()
        {
            int slotCount = 9;
            float slotSize = 40f;
            float spacing = 4f;
            float totalWidth = slotCount * slotSize + (slotCount - 1) * spacing;

            _hotbarContainer = new GameObject("HotbarContainer");
            _hotbarContainer.transform.SetParent(_canvas.transform, false);
            var containerRT = _hotbarContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0);
            containerRT.anchorMax = new Vector2(0.5f, 0);
            containerRT.pivot = new Vector2(0.5f, 0);
            containerRT.anchoredPosition = new Vector2(0, 10);
            containerRT.sizeDelta = new Vector2(totalWidth + 10, slotSize + 10);

            // 背景
            var bgImg = _hotbarContainer.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.03f, 0.06f, 0.8f);

            _hotbarSlotBgs = new Image[slotCount];
            _hotbarItemIcons = new Image[slotCount];
            _hotbarCountTexts = new Text[slotCount];
            _hotbarBorders = new Image[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                float xPos = -totalWidth / 2f + i * (slotSize + spacing) + slotSize / 2f;
                CreateHotbarSlot(i, xPos, slotSize);
            }
        }

        private void CreateHotbarSlot(int index, float xPos, float size)
        {
            // 槽位背景
            var slotObj = CreateUIImage($"HotbarSlot_{index}", new Vector2(size, size),
                Vector2.zero, new Color(0.08f, 0.08f, 0.12f, 0.9f));
            slotObj.transform.SetParent(_hotbarContainer.transform, false);
            var slotRT = slotObj.GetComponent<RectTransform>();
            slotRT.anchoredPosition = new Vector2(xPos, 0);
            _hotbarSlotBgs[index] = slotObj.GetComponent<Image>();

            // 边框（用另一个稍大的Image模拟）
            var borderObj = CreateUIImage($"HotbarBorder_{index}", new Vector2(size + 2, size + 2),
                Vector2.zero, Color.clear);
            borderObj.transform.SetParent(slotObj.transform, false);
            var borderImg = borderObj.GetComponent<Image>();
            borderImg.color = CyberDimBorder;
            var borderRT = borderObj.GetComponent<RectTransform>();
            borderRT.anchorMin = new Vector2(0.5f, 0.5f);
            borderRT.anchorMax = new Vector2(0.5f, 0.5f);
            borderRT.anchoredPosition = Vector2.zero;
            // 设为底层
            borderObj.transform.SetAsFirstSibling();
            _hotbarBorders[index] = borderImg;

            // 物品颜色块
            var iconObj = CreateUIImage($"HotbarIcon_{index}", new Vector2(20, 20),
                Vector2.zero, Color.clear);
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;
            _hotbarItemIcons[index] = iconObj.GetComponent<Image>();

            // 数量文字 (右下角)
            var countObj = new GameObject($"HotbarCount_{index}");
            countObj.transform.SetParent(slotObj.transform, false);
            var countText = countObj.AddComponent<Text>();
            countText.font = _font;
            countText.fontSize = 10;
            countText.color = Color.white;
            countText.alignment = TextAnchor.LowerRight;
            countText.text = "";
            var countRT = countObj.GetComponent<RectTransform>();
            countRT.anchorMin = Vector2.zero;
            countRT.anchorMax = Vector2.one;
            countRT.offsetMin = new Vector2(2, 1);
            countRT.offsetMax = new Vector2(-2, -2);
            _hotbarCountTexts[index] = countText;
        }

        private void RefreshHotbar()
        {
            if (Inventory.Instance == null) return;
            for (int i = 0; i < 9; i++)
            {
                var slot = Inventory.Instance.Slots[i];
                if (slot.isEmpty)
                {
                    _hotbarItemIcons[i].color = Color.clear;
                    _hotbarCountTexts[i].text = "";
                }
                else
                {
                    var itemData = ItemDatabase.Get(slot.itemId);
                    _hotbarItemIcons[i].color = itemData != null ? itemData.displayColor : Color.magenta;
                    _hotbarCountTexts[i].text = slot.count > 1 ? slot.count.ToString() : "";
                }
            }
        }

        private void UpdateHotbarHighlight()
        {
            if (Inventory.Instance == null) return;
            int selected = Inventory.Instance.SelectedSlot;
            for (int i = 0; i < 9; i++)
            {
                _hotbarBorders[i].color = (i == selected) ? CyberHighlight : CyberDimBorder;
            }
        }

        private void OnHotbarSelectionChanged(int index)
        {
            UpdateHotbarHighlight();
        }

        // ========== B. 背包面板 ==========

        private void CreateInventoryPanel()
        {
            int cols = 8, rows = 5;
            float slotSize = 40f;
            float spacing = 4f;
            float panelWidth = cols * (slotSize + spacing) + 30;
            float panelHeight = rows * (slotSize + spacing) + 60;

            _inventoryPanel = new GameObject("InventoryPanel");
            _inventoryPanel.transform.SetParent(_canvas.transform, false);
            var panelRT = _inventoryPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);
            var panelImg = _inventoryPanel.AddComponent<Image>();
            panelImg.color = PanelBG;

            // 标题
            var titleObj = new GameObject("InvTitle");
            titleObj.transform.SetParent(_inventoryPanel.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "数据背包";
            titleText.font = _font;
            titleText.fontSize = 16;
            titleText.color = CyberCyan;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -5);
            titleRT.sizeDelta = new Vector2(0, 25);

            // Grid容器
            var gridObj = new GameObject("InvGrid");
            gridObj.transform.SetParent(_inventoryPanel.transform, false);
            var gridRT = gridObj.AddComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot = new Vector2(0.5f, 0.5f);
            gridRT.anchoredPosition = new Vector2(0, -10);
            float gridW = cols * (slotSize + spacing);
            float gridH = rows * (slotSize + spacing);
            gridRT.sizeDelta = new Vector2(gridW, gridH);

            _invSlotBgs = new Image[40];
            _invItemIcons = new Image[40];
            _invCountTexts = new Text[40];

            for (int i = 0; i < 40; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = -gridW / 2f + col * (slotSize + spacing) + slotSize / 2f;
                float y = gridH / 2f - row * (slotSize + spacing) - slotSize / 2f;
                CreateInventorySlot(i, gridObj.transform, new Vector2(x, y), slotSize);
            }

            _inventoryPanel.SetActive(false);
        }

        private void CreateInventorySlot(int index, Transform parent, Vector2 pos, float size)
        {
            var slotObj = CreateUIImage($"InvSlot_{index}", new Vector2(size, size),
                Vector2.zero, new Color(0.08f, 0.08f, 0.12f, 0.9f));
            slotObj.transform.SetParent(parent, false);
            var slotRT = slotObj.GetComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0.5f, 0.5f);
            slotRT.anchorMax = new Vector2(0.5f, 0.5f);
            slotRT.anchoredPosition = pos;
            _invSlotBgs[index] = slotObj.GetComponent<Image>();

            // 添加按钮交互
            var btn = slotObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int idx = index;
            btn.onClick.AddListener(() => OnInventorySlotClicked(idx));

            // 物品颜色块
            var iconObj = CreateUIImage($"InvIcon_{index}", new Vector2(20, 20),
                Vector2.zero, Color.clear);
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = Vector2.zero;
            _invItemIcons[index] = iconObj.GetComponent<Image>();

            // 数量文字
            var countObj = new GameObject($"InvCount_{index}");
            countObj.transform.SetParent(slotObj.transform, false);
            var countText = countObj.AddComponent<Text>();
            countText.font = _font;
            countText.fontSize = 10;
            countText.color = Color.white;
            countText.alignment = TextAnchor.LowerRight;
            countText.text = "";
            var countRT = countObj.GetComponent<RectTransform>();
            countRT.anchorMin = Vector2.zero;
            countRT.anchorMax = Vector2.one;
            countRT.offsetMin = new Vector2(2, 1);
            countRT.offsetMax = new Vector2(-2, -2);
            _invCountTexts[index] = countText;
        }

        private void OnInventorySlotClicked(int index)
        {
            if (_selectedInvSlot == -1)
            {
                // 选中第一个
                if (Inventory.Instance.Slots[index].isEmpty) return;
                _selectedInvSlot = index;
            }
            else
            {
                // 交换
                if (_selectedInvSlot != index)
                    Inventory.Instance.SwapSlots(_selectedInvSlot, index);
                _selectedInvSlot = -1;
            }
            RefreshInventoryPanel();
        }

        private void RefreshInventoryPanel()
        {
            if (Inventory.Instance == null || _invSlotBgs == null) return;
            for (int i = 0; i < 40; i++)
            {
                var slot = Inventory.Instance.Slots[i];
                if (slot.isEmpty)
                {
                    _invItemIcons[i].color = Color.clear;
                    _invCountTexts[i].text = "";
                }
                else
                {
                    var itemData = ItemDatabase.Get(slot.itemId);
                    _invItemIcons[i].color = itemData != null ? itemData.displayColor : Color.magenta;
                    _invCountTexts[i].text = slot.count > 1 ? slot.count.ToString() : "";
                }

                // 高亮选中
                if (i == _selectedInvSlot)
                    _invSlotBgs[i].color = new Color(0f, 0.3f, 0.35f, 0.95f);
                else
                    _invSlotBgs[i].color = new Color(0.08f, 0.08f, 0.12f, 0.9f);
            }
        }

        // ========== C. 合成面板 ==========

        private void CreateCraftingPanel()
        {
            float panelWidth = 500f;
            float panelHeight = 400f;

            _craftingPanel = new GameObject("CraftingPanel");
            _craftingPanel.transform.SetParent(_canvas.transform, false);
            var panelRT = _craftingPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);
            var panelImg = _craftingPanel.AddComponent<Image>();
            panelImg.color = PanelBG;

            // 标题
            var titleObj = new GameObject("CraftTitle");
            titleObj.transform.SetParent(_craftingPanel.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "合成终端";
            titleText.font = _font;
            titleText.fontSize = 16;
            titleText.color = CyberCyan;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -5);
            titleRT.sizeDelta = new Vector2(0, 28);

            // 左栏 - 配方列表区域
            float leftWidth = 200f;
            var leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(_craftingPanel.transform, false);
            var leftRT = leftPanel.AddComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0, 0);
            leftRT.anchorMax = new Vector2(0, 1);
            leftRT.pivot = new Vector2(0, 0.5f);
            leftRT.anchoredPosition = new Vector2(10, -20);
            leftRT.sizeDelta = new Vector2(leftWidth, -70);

            // ScrollRect for recipe list
            var scrollObj = new GameObject("RecipeScroll");
            scrollObj.transform.SetParent(leftPanel.transform, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollObj.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.6f);
            scrollObj.AddComponent<Mask>().showMaskGraphic = true;

            _recipeListContent = new GameObject("RecipeContent");
            _recipeListContent.transform.SetParent(scrollObj.transform, false);
            var contentRT = _recipeListContent.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);
            scrollRect.content = contentRT;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // 右栏 - 详情
            _recipeDetailPanel = new GameObject("RightPanel");
            _recipeDetailPanel.transform.SetParent(_craftingPanel.transform, false);
            var rightRT = _recipeDetailPanel.AddComponent<RectTransform>();
            rightRT.anchorMin = new Vector2(0, 0);
            rightRT.anchorMax = new Vector2(1, 1);
            rightRT.pivot = new Vector2(0.5f, 0.5f);
            rightRT.offsetMin = new Vector2(leftWidth + 20, 45);
            rightRT.offsetMax = new Vector2(-10, -40);
            _recipeDetailPanel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.6f);

            // 产物名称
            var nameObj = new GameObject("DetailName");
            nameObj.transform.SetParent(_recipeDetailPanel.transform, false);
            _detailNameText = nameObj.AddComponent<Text>();
            _detailNameText.font = _font;
            _detailNameText.fontSize = 14;
            _detailNameText.color = Color.white;
            _detailNameText.alignment = TextAnchor.MiddleLeft;
            _detailNameText.text = "选择一个配方";
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.pivot = new Vector2(0, 1);
            nameRT.anchoredPosition = new Vector2(10, -8);
            nameRT.sizeDelta = new Vector2(-20, 22);

            // 产物颜色块
            var colorObj = CreateUIImage("DetailColor", new Vector2(24, 24),
                Vector2.zero, Color.clear);
            colorObj.transform.SetParent(_recipeDetailPanel.transform, false);
            var colorRT = colorObj.GetComponent<RectTransform>();
            colorRT.anchorMin = new Vector2(1, 1);
            colorRT.anchorMax = new Vector2(1, 1);
            colorRT.pivot = new Vector2(1, 1);
            colorRT.anchoredPosition = new Vector2(-10, -6);
            _detailColorBlock = colorObj.GetComponent<Image>();

            // 材料列表容器
            _materialsListContent = new GameObject("MaterialsList");
            _materialsListContent.transform.SetParent(_recipeDetailPanel.transform, false);
            var matRT = _materialsListContent.AddComponent<RectTransform>();
            matRT.anchorMin = new Vector2(0, 0);
            matRT.anchorMax = new Vector2(1, 1);
            matRT.pivot = new Vector2(0.5f, 1);
            matRT.offsetMin = new Vector2(10, 10);
            matRT.offsetMax = new Vector2(-10, -36);

            // 底部合成按钮
            var btnObj = new GameObject("CraftBtn");
            btnObj.transform.SetParent(_craftingPanel.transform, false);
            var btnRT = btnObj.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(60, 8);
            btnRT.sizeDelta = new Vector2(120, 30);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0f, 0.4f, 0.35f, 0.9f);
            _craftButton = btnObj.AddComponent<Button>();
            _craftButton.targetGraphic = btnImg;
            _craftButton.onClick.AddListener(OnCraftClicked);

            var btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            _craftButtonText = btnTextObj.AddComponent<Text>();
            _craftButtonText.text = "[合成]";
            _craftButtonText.font = _font;
            _craftButtonText.fontSize = 14;
            _craftButtonText.color = CyberCyan;
            _craftButtonText.alignment = TextAnchor.MiddleCenter;
            var btnTxtRT = btnTextObj.GetComponent<RectTransform>();
            btnTxtRT.anchorMin = Vector2.zero;
            btnTxtRT.anchorMax = Vector2.one;
            btnTxtRT.offsetMin = Vector2.zero;
            btnTxtRT.offsetMax = Vector2.zero;

            _craftingPanel.SetActive(false);
        }

        private void RefreshCraftingPanel()
        {
            // 清除旧的配方列表
            foreach (var entry in _recipeEntries)
                Destroy(entry);
            _recipeEntries.Clear();

            var recipes = CraftingSystem.AllRecipes;
            float entryHeight = 26f;
            float yOffset = 0;

            var contentRT = _recipeListContent.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, recipes.Count * entryHeight);

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                bool canCraft = CraftingSystem.CanCraft(recipe);
                var entryObj = new GameObject($"Recipe_{i}");
                entryObj.transform.SetParent(_recipeListContent.transform, false);
                var entryRT = entryObj.AddComponent<RectTransform>();
                entryRT.anchorMin = new Vector2(0, 1);
                entryRT.anchorMax = new Vector2(1, 1);
                entryRT.pivot = new Vector2(0.5f, 1);
                entryRT.anchoredPosition = new Vector2(0, -yOffset);
                entryRT.sizeDelta = new Vector2(0, entryHeight);

                var entryImg = entryObj.AddComponent<Image>();
                entryImg.color = (_selectedRecipeIndex == i)
                    ? new Color(0f, 0.3f, 0.4f, 0.8f)
                    : new Color(0.06f, 0.06f, 0.1f, 0.5f);

                var btn = entryObj.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() => OnRecipeSelected(idx));

                // 配方名
                var textObj = new GameObject("RecipeText");
                textObj.transform.SetParent(entryObj.transform, false);
                var txt = textObj.AddComponent<Text>();
                txt.font = _font;
                txt.fontSize = 11;
                txt.text = $"{recipe.name} x{recipe.resultCount}";
                txt.color = canCraft ? new Color(0.85f, 0.95f, 0.9f) : new Color(0.4f, 0.4f, 0.4f);
                txt.alignment = TextAnchor.MiddleLeft;
                var txtRT = textObj.GetComponent<RectTransform>();
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = new Vector2(6, 0);
                txtRT.offsetMax = new Vector2(-4, 0);

                _recipeEntries.Add(entryObj);
                yOffset += entryHeight;
            }

            RefreshRecipeDetail();
        }

        private void OnRecipeSelected(int index)
        {
            _selectedRecipeIndex = index;
            RefreshCraftingPanel();
        }

        private void RefreshRecipeDetail()
        {
            // 清除旧材料列表
            foreach (Transform child in _materialsListContent.transform)
                Destroy(child.gameObject);

            if (_selectedRecipeIndex < 0 || _selectedRecipeIndex >= CraftingSystem.AllRecipes.Count)
            {
                _detailNameText.text = "选择一个配方";
                _detailColorBlock.color = Color.clear;
                _craftButton.interactable = false;
                _craftButtonText.color = new Color(0.3f, 0.3f, 0.3f);
                return;
            }

            var recipe = CraftingSystem.AllRecipes[_selectedRecipeIndex];
            var resultData = ItemDatabase.Get(recipe.resultItemId);
            bool canCraft = CraftingSystem.CanCraft(recipe);

            _detailNameText.text = recipe.name + " x" + recipe.resultCount;
            _detailColorBlock.color = resultData != null ? resultData.displayColor : Color.magenta;
            _craftButton.interactable = canCraft;
            _craftButtonText.color = canCraft ? CyberCyan : new Color(0.3f, 0.3f, 0.3f);

            // 需要材料标签
            CreateMaterialHeader();

            // 材料列表
            float yOffset = -22f;
            foreach (var mat in recipe.materials)
            {
                CreateMaterialEntry(mat, yOffset);
                yOffset -= 22f;
            }
        }

        private void CreateMaterialHeader()
        {
            var headerObj = new GameObject("MatHeader");
            headerObj.transform.SetParent(_materialsListContent.transform, false);
            var txt = headerObj.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = 11;
            txt.text = "— 需要材料 —";
            txt.color = new Color(0.6f, 0.6f, 0.6f);
            txt.alignment = TextAnchor.MiddleLeft;
            var rt = headerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 20);
        }

        private void CreateMaterialEntry(CraftMaterial mat, float yPos)
        {
            var matData = ItemDatabase.Get(mat.itemId);
            int have = Inventory.Instance != null ? Inventory.Instance.CountItem(mat.itemId) : 0;
            bool enough = have >= mat.count;

            var entryObj = new GameObject("MatEntry");
            entryObj.transform.SetParent(_materialsListContent.transform, false);
            var entryRT = entryObj.AddComponent<RectTransform>();
            entryRT.anchorMin = new Vector2(0, 1);
            entryRT.anchorMax = new Vector2(1, 1);
            entryRT.pivot = new Vector2(0, 1);
            entryRT.anchoredPosition = new Vector2(0, yPos);
            entryRT.sizeDelta = new Vector2(0, 20);

            // 颜色块
            var colorObj = CreateUIImage("MatColor", new Vector2(14, 14), Vector2.zero,
                matData != null ? matData.displayColor : Color.gray);
            colorObj.transform.SetParent(entryObj.transform, false);
            var colorRT = colorObj.GetComponent<RectTransform>();
            colorRT.anchorMin = new Vector2(0, 0.5f);
            colorRT.anchorMax = new Vector2(0, 0.5f);
            colorRT.pivot = new Vector2(0, 0.5f);
            colorRT.anchoredPosition = new Vector2(4, 0);

            // 文字
            var textObj = new GameObject("MatText");
            textObj.transform.SetParent(entryObj.transform, false);
            var txt = textObj.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = 11;
            string matName = matData != null ? matData.itemName : $"ID:{mat.itemId}";
            txt.text = $"{matName}  {have}/{mat.count}";
            txt.color = enough ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.9f, 0.2f, 0.2f);
            txt.alignment = TextAnchor.MiddleLeft;
            var txtRT = textObj.GetComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0, 0);
            txtRT.anchorMax = new Vector2(1, 1);
            txtRT.offsetMin = new Vector2(22, 0);
            txtRT.offsetMax = Vector2.zero;
        }

        private void OnCraftClicked()
        {
            if (_selectedRecipeIndex < 0 || _selectedRecipeIndex >= CraftingSystem.AllRecipes.Count) return;
            var recipe = CraftingSystem.AllRecipes[_selectedRecipeIndex];
            if (CraftingSystem.Craft(recipe))
            {
                RefreshCraftingPanel();
                RefreshHotbar();
            }
        }

        // ========== D. 拾取物品飘字提示 ==========

        private void ShowPickupToast(int itemId, int count)
        {
            var itemData = ItemDatabase.Get(itemId);
            string name = itemData != null ? itemData.itemName : $"ID:{itemId}";

            var toastObj = new GameObject("PickupToast");
            toastObj.transform.SetParent(_canvas.transform, false);
            var txt = toastObj.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = 13;
            txt.text = $"+{count} {name}";
            txt.color = new Color(0.2f, 1f, 0.4f, 1f);
            txt.alignment = TextAnchor.LowerLeft;
            var rt = toastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(250, 20);

            // 计算位置(向上叠加)
            float baseY = 60f;
            float yOffset = baseY + _pickupToasts.Count * 22f;
            rt.anchoredPosition = new Vector2(15, yOffset);

            _pickupToasts.Add(new PickupToast
            {
                gameObject = toastObj,
                text = txt,
                timeRemaining = 2f
            });
        }

        private void UpdatePickupToasts()
        {
            for (int i = _pickupToasts.Count - 1; i >= 0; i--)
            {
                var toast = _pickupToasts[i];
                toast.timeRemaining -= Time.unscaledDeltaTime;

                if (toast.timeRemaining <= 0)
                {
                    Destroy(toast.gameObject);
                    _pickupToasts.RemoveAt(i);
                    // 重新排布剩余toast位置
                    RepositionToasts();
                }
                else if (toast.timeRemaining < 0.5f)
                {
                    // 淡出
                    float alpha = toast.timeRemaining / 0.5f;
                    var c = toast.text.color;
                    toast.text.color = new Color(c.r, c.g, c.b, alpha);
                }
            }
        }

        private void RepositionToasts()
        {
            float baseY = 60f;
            for (int i = 0; i < _pickupToasts.Count; i++)
            {
                var rt = _pickupToasts[i].gameObject.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(15, baseY + i * 22f);
            }
        }

        // ========== 工具方法 ==========

        private GameObject CreateUIImage(string name, Vector2 size, Vector2 position, Color color)
        {
            var obj = new GameObject(name);
            var img = obj.AddComponent<Image>();
            img.color = color;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return obj;
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (_healthFill != null)
                _healthFill.fillAmount = (float)current / max;
            if (_healthText != null)
                _healthText.text = $"{current}/{max}";
        }

        private void UpdateManaBar(int current, int max)
        {
            if (_manaFill != null)
                _manaFill.fillAmount = (float)current / max;
        }

        // ========== 辅助类 ==========

        private class PickupToast
        {
            public GameObject gameObject;
            public Text text;
            public float timeRemaining;
        }
    }
}
