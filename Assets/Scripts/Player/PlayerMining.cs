using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家挖掘/放置系统 - 支持镐力等级检查、AOE挖掘、工具特殊能力、耐久度消耗
    /// </summary>
    public class PlayerMining : MonoBehaviour
    {
        [Header("设置")]
        public float miningRange = 5f;
        public float placingRange = 5f;
        public LayerMask tileLayer;

        private float _miningProgress;
        private Vector2Int _miningTarget = new Vector2Int(-1, -1);
        private Camera _cam;

        // 工具系统状态（预留：后续用于工具损坏提示与修复逻辑）
#pragma warning disable 0414
        private bool _toolBroken;
#pragma warning restore 0414
        private float _insufficientPowerTimer; // "工具等级不足"提示计时器
        private string _toolWarningMessage;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.isPaused) return;
            if (GameManager.Instance.isInventoryOpen) return;

            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int tilePos = new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(-mouseWorld.y));

            float dist = Vector2.Distance(transform.position, mouseWorld);

            // 清除过时的警告
            if (_insufficientPowerTimer > 0f)
                _insufficientPowerTimer -= Time.deltaTime;

            // 左键: 挖掘/攻击
            if (Input.GetMouseButton(0) && dist <= miningRange)
            {
                HandleMining(tilePos);
            }
            else
            {
                _miningProgress = 0f;
                _miningTarget = new Vector2Int(-1, -1);
            }

            // 右键: 放置
            if (Input.GetMouseButtonDown(1) && dist <= placingRange)
            {
                HandlePlacing(tilePos);
            }
        }

        private void HandleMining(Vector2Int tilePos)
        {
            var gm = GameManager.Instance;
            TileType tile = gm.GetTile(tilePos.x, tilePos.y);

            if (tile == TileType.Air) return;

            // 获取方块属性
            var tileProps = TileRegistry.Get(tile);
            if (tileProps == null) return;

            // 获取当前装备的工具
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            int toolPickPower = 0;
            float miningSpeed = 0.5f; // 赤手挖掘速度很慢

            if (selectedItem != null && selectedItem.category == ItemCategory.Tool)
            {
                toolPickPower = selectedItem.pickPower;
                miningSpeed = selectedItem.miningSpeed;
            }

            // --- 镐力检查：阻止低级工具挖高级方块 ---
            if (tileProps.requiredPickPower > 0 && toolPickPower < tileProps.requiredPickPower)
            {
                // 工具等级不足，无法挖掘，进度不前进
                _miningProgress = 0f;
                _insufficientPowerTimer = 2f;
                _toolWarningMessage = "需要更高等级工具!";
                return;
            }

            // 检查是否换了目标
            if (tilePos != _miningTarget)
            {
                _miningProgress = 0f;
                _miningTarget = tilePos;
            }

            // 计算挖掘速度（含工具特殊加速）
            float speedMult = PlayerStats.Instance != null ? PlayerStats.Instance.miningSpeedMultiplier : 1f;
            float toolSpeedMultiplier = GetToolSpeedMultiplier(selectedItem);
            float materialBonus = GetMaterialBonus(selectedItem, tile);
            float effectivePickPower = Mathf.Max(toolPickPower, 1f);

            _miningProgress += Time.deltaTime * miningSpeed * speedMult * toolSpeedMultiplier * materialBonus * effectivePickPower;

            // 达到硬度阈值 -> 破坏
            float requiredTime = tileProps.hardness * 0.15f / effectivePickPower;
            if (_miningProgress >= requiredTime)
            {
                // 检查是否AOE工具且按住Shift
                if (IsAOETool(selectedItem) && Input.GetKey(KeyCode.LeftShift))
                {
                    AOEMine(tilePos.x, tilePos.y, selectedItem);
                }
                else
                {
                    BreakTileAt(tilePos.x, tilePos.y, selectedItem);
                }
                _miningProgress = 0f;
            }
        }

        // ========== 工具特殊能力系统 ==========

        /// <summary>
        /// 是否为AOE范围挖掘工具（量子钻头 ID 102）
        /// </summary>
        private bool IsAOETool(ItemData tool)
        {
            return tool != null && tool.id == 102;
        }

        /// <summary>
        /// AOE范围挖掘 - 3x3范围同时破坏
        /// </summary>
        private void AOEMine(int centerX, int centerY, ItemData tool)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int tx = centerX + dx;
                int ty = centerY + dy;
                BreakTileAt(tx, ty, tool);
            }
        }

        /// <summary>
        /// 获取工具速度倍率 - 等离子切割器(103)双倍速
        /// </summary>
        private float GetToolSpeedMultiplier(ItemData tool)
        {
            if (tool == null) return 1f;
            if (tool.id == 103) return 2f; // 等离子切割器双倍速
            return 1f;
        }

        /// <summary>
        /// 获取工具耐久消耗量
        /// </summary>
        private int GetDurabilityConsumption(ItemData tool)
        {
            if (tool == null) return 0;
            if (tool.id == 103) return 2; // 等离子切割器双倍消耗
            if (tool.id == 102) return 1; // 量子钻头正常消耗
            return 1;
        }

        /// <summary>
        /// 获取工具对特定材质的加速倍率 - 数据提取器(104)对有机方块加速
        /// </summary>
        private float GetMaterialBonus(ItemData tool, TileType tile)
        {
            if (tool == null) return 1f;
            if (tool.id == 104)
            {
                // 对木质/有机类方块速度加倍
                if (tile == TileType.CyberTreeTrunk || tile == TileType.CyberTreeCanopy ||
                    tile == TileType.MutantCactus || tile == TileType.DataMoss)
                    return 2f;
            }
            return 1f;
        }

        // ========== 核心破坏逻辑 ==========

        /// <summary>
        /// 在指定坐标破坏方块 - 包含镐力检查和耐久扣除
        /// </summary>
        private void BreakTileAt(int x, int y, ItemData tool)
        {
            var gm = GameManager.Instance;
            TileType tile = gm.GetTile(x, y);

            // 跳过空气
            if (tile == TileType.Air) return;

            var props = TileRegistry.Get(tile);
            if (props == null) return;

            // AOE挖掘时也需要对每个方块单独检查镐力
            int toolPickPower = (tool != null && tool.category == ItemCategory.Tool) ? tool.pickPower : 0;
            if (props.requiredPickPower > 0 && toolPickPower < props.requiredPickPower)
                return; // 此方块镐力不足，跳过

            // 设置为空气
            gm.SetTile(x, y, TileType.Air);

            // 教程追踪：挖掘
            if (TutorialSystem.Instance != null)
                TutorialSystem.Instance.HasMined = true;

            // 通知AI伙伴
            if (AICompanion.Instance != null)
                AICompanion.Instance.OnPlayerMined();

            // 检查是否是幸运方块 - 触发特殊掉落而非普通物品
            if (tile == TileType.LuckyRuin)
            {
                if (LuckyBlockSystem.Instance != null)
                {
                    Vector2 worldPos = new Vector2(x + 0.5f, -y - 0.5f);
                    LuckyBlockSystem.Instance.OnLuckyBlockDestroyed(worldPos);
                }
            }
            else if (props.dropItemId > 0)
            {
                // 普通方块掉落物品
                SpawnItemDrop(new Vector2Int(x, y), props.dropItemId);
            }

            // 更新光照
            var lighting = FindObjectOfType<LightingSystem>();
            if (lighting != null)
                lighting.UpdateLocalLighting(x, y);

            // 更新Tilemap显示
            var chunks = FindObjectOfType<ChunkManager>();
            if (chunks != null)
                chunks.RefreshTile(x, y);

            // 粒子效果
            var particles = FindObjectOfType<ParticleManager>();
            if (particles != null)
                particles.SpawnBreakParticles(new Vector2(x + 0.5f, -y - 0.5f), props.baseColor);

            // --- 工具耐久度消耗 ---
            if (tool != null && tool.category == ItemCategory.Tool && tool.maxDurability > 0)
            {
                int duraCost = GetDurabilityConsumption(tool);
                bool toolSurvived = Inventory.Instance.ConsumeDurability(Inventory.Instance.SelectedSlot, duraCost);
                if (!toolSurvived)
                {
                    // 工具已损坏
                    _toolBroken = true;
                    _toolWarningMessage = "工具已损坏!";
                    _insufficientPowerTimer = 2f;
                    _miningProgress = 0f;
                    Debug.Log("[工具] 当前工具已损坏!");
                }
            }
        }

        // ========== 放置系统 ==========

        private void HandlePlacing(Vector2Int tilePos)
        {
            var gm = GameManager.Instance;

            // 检查目标位置是否为空
            if (gm.GetTile(tilePos.x, tilePos.y) != TileType.Air) return;

            // 检查手持物品是否可放置
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem == null || selectedItem.category != ItemCategory.Placeable) return;

            // 放置方块
            gm.SetTile(tilePos.x, tilePos.y, selectedItem.placesTile);

            // 教程追踪：放置
            if (TutorialSystem.Instance != null)
                TutorialSystem.Instance.HasPlacedBlock = true;

            // 消耗物品
            Inventory.Instance.ConsumeSelected();

            // 更新显示
            var chunks = FindObjectOfType<ChunkManager>();
            if (chunks != null)
                chunks.RefreshTile(tilePos.x, tilePos.y);

            // 更新光照
            var lighting = FindObjectOfType<LightingSystem>();
            if (lighting != null)
                lighting.UpdateLocalLighting(tilePos.x, tilePos.y);
        }

        private void SpawnItemDrop(Vector2Int tilePos, int itemId)
        {
            // 直接添加到背包（简化版，后续可改为物理掉落）
            Inventory.Instance?.AddItem(itemId, 1);
        }

        // ========== 公共查询接口 ==========

        /// <summary>
        /// 获取当前挖掘进度 0-1
        /// </summary>
        public float GetMiningProgress()
        {
            if (_miningTarget.x < 0) return 0f;
            var tile = GameManager.Instance.GetTile(_miningTarget.x, _miningTarget.y);
            var props = TileRegistry.Get(tile);
            if (props == null) return 0f;

            float pickPower = 1f;
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem != null && selectedItem.category == ItemCategory.Tool)
                pickPower = selectedItem.pickPower;

            float requiredTime = props.hardness * 0.15f / Mathf.Max(pickPower, 1f);
            return requiredTime > 0 ? Mathf.Clamp01(_miningProgress / requiredTime) : 0f;
        }

        public Vector2Int GetMiningTarget() => _miningTarget;

        /// <summary>
        /// 获取当前工具的耐久度百分比 (0-1)
        /// </summary>
        public float GetToolDurabilityPercent()
        {
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem == null || selectedItem.maxDurability <= 0) return 1f;

            int currentDura = Inventory.Instance.GetSlotDurability(Inventory.Instance.SelectedSlot);
            return (float)currentDura / selectedItem.maxDurability;
        }

        /// <summary>
        /// 获取当前警告信息（工具等级不足/工具损坏等）
        /// </summary>
        public string GetToolWarning()
        {
            if (_insufficientPowerTimer > 0f)
                return _toolWarningMessage;
            return null;
        }

        /// <summary>
        /// 当前选中的是否为AOE工具
        /// </summary>
        public bool IsCurrentToolAOE()
        {
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            return IsAOETool(selectedItem);
        }
    }
}
