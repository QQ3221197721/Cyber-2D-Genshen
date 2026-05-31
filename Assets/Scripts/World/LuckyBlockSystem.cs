using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 幸运方块核心系统 - 管理废墟方块的摧毁奖励和AI修复加成
    /// </summary>
    public class LuckyBlockSystem : MonoBehaviour
    {
        public static LuckyBlockSystem Instance { get; private set; }

        // 奖励池定义
        private struct LootEntry
        {
            public int itemId;
            public int minCount;
            public int maxCount;
            public float weight;
        }

        private List<LootEntry> _commonLoot = new List<LootEntry>();    // 60%
        private List<LootEntry> _rareLoot = new List<LootEntry>();      // 25%
        private List<LootEntry> _epicLoot = new List<LootEntry>();      // 12%
        private List<LootEntry> _legendaryLoot = new List<LootEntry>(); // 3%

        void Awake()
        {
            Instance = this;
            InitializeLootTables();
        }

        private void InitializeLootTables()
        {
            // 普通奖励(60%): 材料和弹药
            _commonLoot.Add(new LootEntry { itemId = 1, minCount = 5, maxCount = 15, weight = 1f });    // 锈铁
            _commonLoot.Add(new LootEntry { itemId = 2, minCount = 3, maxCount = 10, weight = 1f });    // 铜线缆
            _commonLoot.Add(new LootEntry { itemId = 3, minCount = 3, maxCount = 8, weight = 1f });     // 硅晶
            _commonLoot.Add(new LootEntry { itemId = 310, minCount = 20, maxCount = 50, weight = 1f }); // 标准弹药
            _commonLoot.Add(new LootEntry { itemId = 311, minCount = 10, maxCount = 25, weight = 1f }); // 霰弹壳
            _commonLoot.Add(new LootEntry { itemId = 312, minCount = 10, maxCount = 30, weight = 1f }); // 能量电池
            _commonLoot.Add(new LootEntry { itemId = 300, minCount = 2, maxCount = 5, weight = 1f });   // 纳米修复剂

            // 稀有奖励(25%): 高级材料和中端武器
            _rareLoot.Add(new LootEntry { itemId = 5, minCount = 2, maxCount = 5, weight = 1f });    // 钛矿
            _rareLoot.Add(new LootEntry { itemId = 6, minCount = 1, maxCount = 3, weight = 1f });    // 量子芯片
            _rareLoot.Add(new LootEntry { itemId = 201, minCount = 1, maxCount = 1, weight = 1f });  // 锈蚀砍刀
            _rareLoot.Add(new LootEntry { itemId = 202, minCount = 1, maxCount = 1, weight = 1f });  // 钛合金长剑
            _rareLoot.Add(new LootEntry { itemId = 252, minCount = 1, maxCount = 1, weight = 1f });  // 脉冲步枪
            _rareLoot.Add(new LootEntry { itemId = 313, minCount = 5, maxCount = 15, weight = 1f }); // 等离子芯

            // 史诗奖励(12%): 高端武器和工具
            _epicLoot.Add(new LootEntry { itemId = 203, minCount = 1, maxCount = 1, weight = 1f }); // 等离子刃
            _epicLoot.Add(new LootEntry { itemId = 207, minCount = 1, maxCount = 1, weight = 1f }); // 霓虹武士刀
            _epicLoot.Add(new LootEntry { itemId = 253, minCount = 1, maxCount = 1, weight = 1f }); // 等离子炮
            _epicLoot.Add(new LootEntry { itemId = 102, minCount = 1, maxCount = 1, weight = 1f }); // 量子钻头
            _epicLoot.Add(new LootEntry { itemId = 103, minCount = 1, maxCount = 1, weight = 1f }); // 等离子切割器

            // 传奇奖励(3%): 最强武器和Boss召唤
            _legendaryLoot.Add(new LootEntry { itemId = 206, minCount = 1, maxCount = 1, weight = 1f }); // 单分子线
            _legendaryLoot.Add(new LootEntry { itemId = 254, minCount = 1, maxCount = 1, weight = 1f }); // 磁轨枪
            _legendaryLoot.Add(new LootEntry { itemId = 204, minCount = 1, maxCount = 1, weight = 1f }); // 量子大剑
            _legendaryLoot.Add(new LootEntry { itemId = 401, minCount = 1, maxCount = 1, weight = 1f }); // Boss召唤物
        }

        /// <summary>
        /// 摧毁幸运方块时调用，随机掉落奖励物品
        /// </summary>
        public void OnLuckyBlockDestroyed(Vector2 worldPosition)
        {
            // 随机选择稀有度
            float roll = Random.value;
            List<LootEntry> pool;
            string tierName;

            if (roll < 0.03f) { pool = _legendaryLoot; tierName = "传奇"; }
            else if (roll < 0.15f) { pool = _epicLoot; tierName = "史诗"; }
            else if (roll < 0.40f) { pool = _rareLoot; tierName = "稀有"; }
            else { pool = _commonLoot; tierName = "普通"; }

            // 从池中随机选择一个
            if (pool.Count == 0) return;
            var entry = pool[Random.Range(0, pool.Count)];
            int count = Random.Range(entry.minCount, entry.maxCount + 1);

            // 添加到背包
            if (Inventory.Instance != null)
                Inventory.Instance.AddItem(entry.itemId, count);

            // 获取物品名称
            string itemName = "未知物品";
            var itemData = ItemDatabase.Get(entry.itemId);
            if (itemData != null) itemName = itemData.itemName;

            // 显示获得提示（通过AI伙伴说话）
            if (AICompanion.Instance != null)
            {
                AICompanion.Instance.ShowDialogue($"[{tierName}] 发现 {itemName} x{count}！", 3f);
            }

            // 粒子特效
            if (ParticleManager.Instance != null)
            {
                Color effectColor = tierName == "传奇" ? Color.yellow :
                                    tierName == "史诗" ? new Color(0.8f, 0.3f, 1f) :
                                    Color.cyan;
                ParticleManager.Instance.SpawnBreakParticles(worldPosition, effectColor);
            }
        }

        // ===== 修复系统 =====

        // 修复加成定义
        private static readonly string[] RepairBonusTypes = { "maxHealth", "defense", "damage", "miningSpeed", "moveSpeed" };
        private static readonly float[] RepairBonusValues = { 10f, 2f, 0.05f, 0.05f, 0.3f };
        private static readonly string[] RepairBonusNames = { "最大生命+10", "防御+2", "伤害+5%", "挖掘速度+5%", "移动速度+0.3" };

        private int _totalRepaired = 0;

        /// <summary>
        /// AI修复幸运废墟后调用
        /// </summary>
        public void OnRuinRepaired(int tileX, int tileY)
        {
            _totalRepaired++;

            // 随机选择一个加成类型
            int bonusIndex = Random.Range(0, RepairBonusTypes.Length);
            string bonusType = RepairBonusTypes[bonusIndex];
            float bonusValue = RepairBonusValues[bonusIndex];
            string bonusName = RepairBonusNames[bonusIndex];

            // 应用永久加成
            ApplyPermanentBonus(bonusType, bonusValue);

            // AI伙伴播报
            if (AICompanion.Instance != null)
            {
                AICompanion.Instance.ShowDialogue($"遗迹修复完成！获得永久加成：{bonusName}", 4f);
            }

            // 将方块变为已修复状态
            var chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager != null)
            {
                chunkManager.SetTile(tileX, tileY, TileType.RepairedRuin);
            }
        }

        private void ApplyPermanentBonus(string statType, float amount)
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return;

            switch (statType)
            {
                case "maxHealth":
                    stats.IncreaseMaxHealth((int)amount);
                    break;
                case "defense":
                    stats.defense += (int)amount;
                    break;
                case "damage":
                    stats.damageMultiplier += amount;
                    break;
                case "miningSpeed":
                    stats.miningSpeedMultiplier += amount;
                    break;
                case "moveSpeed":
                    stats.moveSpeedBonus += amount;
                    break;
            }
        }

        public int TotalRepaired => _totalRepaired;
    }
}
