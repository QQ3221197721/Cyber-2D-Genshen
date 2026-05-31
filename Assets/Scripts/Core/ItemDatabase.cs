using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    public enum ItemCategory
    {
        Material,
        Tool,
        MeleeWeapon,
        RangedWeapon,
        Armor,
        Accessory,
        Consumable,
        Placeable,
        BossSummon,
        Cyberware
    }

    [System.Serializable]
    public class ItemData
    {
        public int id;
        public string itemName;
        public string description;
        public ItemCategory category;
        public Color displayColor;
        public int maxStack;
        public int damage;
        public float attackSpeed;
        public float range;
        public int defense;
        public int pickPower;       // 镐力
        public float miningSpeed;
        public int healAmount;
        public int manaRestore;
        public TileType placesTile; // 如果是可放置方块
        public string bossToSummon; // Boss召唤ID
    }

    /// <summary>
    /// 物品数据库 - 注册所有游戏物品
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<int, ItemData> _items;

        public static Dictionary<int, ItemData> All
        {
            get
            {
                if (_items == null) Initialize();
                return _items;
            }
        }

        public static ItemData Get(int id)
        {
            if (_items == null) Initialize();
            return _items.ContainsKey(id) ? _items[id] : null;
        }

        private static void Initialize()
        {
            _items = new Dictionary<int, ItemData>();

            // ===== 材料 (ID 1-99) =====
            AddMaterial(1, "焦土块", new Color(0.20f, 0.14f, 0.09f));
            AddMaterial(2, "变异苔", new Color(0.09f, 0.43f, 0.15f));
            AddMaterial(3, "沥青块", new Color(0.11f, 0.11f, 0.13f));
            AddMaterial(4, "沙石", new Color(0.55f, 0.51f, 0.35f));
            AddMaterial(5, "黏土", new Color(0.43f, 0.31f, 0.24f));
            AddMaterial(6, "枯木", new Color(0.27f, 0.20f, 0.12f));
            AddMaterial(10, "混凝土块", new Color(0.28f, 0.28f, 0.31f));
            AddMaterial(11, "锈铁", new Color(0.45f, 0.22f, 0.09f));
            AddMaterial(12, "强化混凝土", new Color(0.35f, 0.35f, 0.38f));
            AddMaterial(13, "碎砖", new Color(0.23f, 0.21f, 0.19f));
            AddMaterial(14, "碳纤维", new Color(0.05f, 0.05f, 0.07f));
            AddMaterial(15, "防弹玻璃", new Color(0.33f, 0.54f, 0.69f));
            AddMaterial(20, "管道", new Color(0.22f, 0.35f, 0.35f));
            AddMaterial(21, "线缆", new Color(0.16f, 0.24f, 0.27f));
            AddMaterial(22, "电路板", new Color(0.08f, 0.31f, 0.24f));
            AddMaterial(30, "钛合金锭", new Color(0.51f, 0.58f, 0.63f));
            AddMaterial(31, "霓虹晶体", new Color(0.0f, 0.94f, 0.71f));
            AddMaterial(32, "等离子电池", new Color(0.90f, 0.51f, 0.0f));
            AddMaterial(33, "量子芯片", new Color(0.67f, 0.20f, 0.90f));
            AddMaterial(34, "赛博合金板", new Color(0.15f, 0.15f, 0.24f));
            AddMaterial(51, "异变肉块", new Color(0.47f, 0.16f, 0.20f));
            AddMaterial(52, "紫晶", new Color(0.55f, 0.24f, 0.78f));
            AddMaterial(80, "赛博树木", new Color(0.15f, 0.45f, 0.25f));
            AddMaterial(81, "全息叶片", new Color(0.0f, 1.0f, 0.6f));
            AddMaterial(82, "废金属", new Color(0.45f, 0.35f, 0.2f));
            AddMaterial(83, "能量晶体", new Color(0.1f, 0.9f, 1.0f));
            AddMaterial(84, "变异纤维", new Color(0.4f, 0.75f, 0.1f));
            AddMaterial(85, "数据碎片", new Color(0.2f, 0.6f, 0.9f));
            AddMaterial(86, "能量电池", new Color(0.1f, 0.8f, 0.9f));

            // ===== 护甲 (ID 150-199) =====
            AddArmor(150, "废铁护甲", 6, new Color(0.45f, 0.35f, 0.2f));

            // ===== 工具 (ID 100-149) =====
            AddTool(100, "废铁镐", 1, 1.0f, new Color(0.59f, 0.39f, 0.27f));
            AddTool(101, "钛合金镐", 2, 1.5f, new Color(0.51f, 0.58f, 0.63f));
            AddTool(102, "量子钻头", 4, 3.0f, new Color(0.67f, 0.20f, 0.90f));
            AddTool(103, "等离子切割器", 3, 2.0f, new Color(0.90f, 0.51f, 0.0f));
            AddTool(104, "数据提取器", 2, 1.8f, new Color(0.2f, 0.6f, 0.9f));

            // ===== 近战武器 (ID 200-249) =====
            AddMelee(200, "废铁刀", 12, 1.0f, 2.5f, new Color(0.55f, 0.43f, 0.31f));
            AddMelee(201, "锈蚀砍刀", 18, 0.9f, 2.8f, new Color(0.45f, 0.22f, 0.09f));
            AddMelee(202, "钛合金长剑", 25, 1.1f, 3.0f, new Color(0.51f, 0.58f, 0.63f));
            AddMelee(203, "等离子刃", 35, 1.3f, 3.2f, new Color(0.90f, 0.51f, 0.0f));
            AddMelee(204, "量子大剑", 50, 0.8f, 3.8f, new Color(0.67f, 0.20f, 0.90f));
            AddMelee(205, "螳螂刀", 38, 2.0f, 2.2f, new Color(0.0f, 1.0f, 0.78f));
            AddMelee(206, "单分子线", 55, 1.5f, 4.5f, new Color(1.0f, 0.0f, 0.78f));
            AddMelee(207, "霓虹武士刀", 42, 1.8f, 3.0f, new Color(0.0f, 0.94f, 0.71f));

            // ===== 远程武器 (ID 250-299) =====
            AddRanged(250, "手制左轮", 15, 0.8f, new Color(0.47f, 0.39f, 0.31f));
            AddRanged(251, "废土猎枪", 22, 0.5f, new Color(0.35f, 0.30f, 0.25f));
            AddRanged(252, "脉冲步枪", 28, 2.0f, new Color(0.0f, 0.78f, 1.0f));
            AddRanged(253, "等离子炮", 45, 0.5f, new Color(0.90f, 0.51f, 0.0f));
            AddRanged(254, "磁轨枪", 70, 0.3f, new Color(0.39f, 0.0f, 1.0f));
            AddRanged(255, "霓虹弩", 32, 1.2f, new Color(0.0f, 0.94f, 0.71f));

            // ===== 消耗品 (ID 300-349) =====
            AddConsumable(300, "纳米修复剂", 50, 0, new Color(1.0f, 0.31f, 0.31f));
            AddConsumable(301, "高级修复剂", 120, 0, new Color(1.0f, 0.55f, 0.55f));
            AddConsumable(302, "能量饮料", 0, 40, new Color(0.31f, 0.31f, 1.0f));
            AddConsumable(303, "肾上腺素", 0, 0, new Color(1.0f, 0.78f, 0.0f));
            AddConsumable(304, "辐射解毒剂", 0, 0, new Color(0.0f, 0.80f, 0.40f));
            AddConsumable(305, "变异药剂", 80, 20, new Color(0.4f, 0.75f, 0.1f));

            // ===== Boss召唤物 (ID 400-419) =====
            AddBossSummon(400, "损坏的信号器", "MechEye", new Color(1.0f, 0.0f, 0.0f));
            AddBossSummon(401, "腐蚀数据核心", "DataWorm", new Color(0.0f, 1.0f, 0.39f));
            AddBossSummon(402, "过载电容器", "TitanMech", new Color(0.0f, 0.60f, 1.0f));
            AddBossSummon(403, "虚空碎片", "NexusCore", new Color(0.67f, 0.0f, 1.0f));

            // ===== 可放置方块 (ID 500-549) =====
            AddPlaceable(500, "混凝土块", TileType.Concrete, new Color(0.28f, 0.28f, 0.31f));
            AddPlaceable(501, "霓虹灯管", TileType.NeonPanel, new Color(0.90f, 0.0f, 0.50f));
            AddPlaceable(502, "平台", TileType.Platform, new Color(0.24f, 0.20f, 0.18f));
            AddPlaceable(503, "全息方块", TileType.HologramBlock, new Color(0.39f, 0.78f, 1.0f));
            AddPlaceable(504, "等离子灯", TileType.PlasmaLamp, new Color(1.0f, 0.60f, 0.0f));
            AddPlaceable(505, "霓虹火把", TileType.NeonPanel, new Color(0.0f, 1.0f, 0.6f));
        }

        private static void AddMaterial(int id, string name, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.Material,
                displayColor = color, maxStack = 999, description = "基础材料"
            };
        }

        private static void AddArmor(int id, string name, int def, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.Armor,
                displayColor = color, maxStack = 1, defense = def,
                description = $"防御: {def}"
            };
        }

        private static void AddTool(int id, string name, int power, float speed, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.Tool,
                displayColor = color, maxStack = 1, pickPower = power,
                miningSpeed = speed, damage = power * 4,
                description = $"镐力: {power} | 速度: {speed:F1}x"
            };
        }

        private static void AddMelee(int id, string name, int dmg, float speed, float range, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.MeleeWeapon,
                displayColor = color, maxStack = 1, damage = dmg,
                attackSpeed = speed, range = range,
                description = $"伤害: {dmg} | 速度: {speed:F1}x | 范围: {range:F1}"
            };
        }

        private static void AddRanged(int id, string name, int dmg, float speed, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.RangedWeapon,
                displayColor = color, maxStack = 1, damage = dmg,
                attackSpeed = speed, range = 20f,
                description = $"伤害: {dmg} | 射速: {speed:F1}/s"
            };
        }

        private static void AddConsumable(int id, string name, int heal, int mana, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.Consumable,
                displayColor = color, maxStack = 30, healAmount = heal,
                manaRestore = mana,
                description = heal > 0 ? $"恢复 {heal} HP" : mana > 0 ? $"恢复 {mana} MP" : "特殊效果"
            };
        }

        private static void AddBossSummon(int id, string name, string bossId, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.BossSummon,
                displayColor = color, maxStack = 5, bossToSummon = bossId,
                description = "在特定条件下使用可召唤Boss"
            };
        }

        private static void AddPlaceable(int id, string name, TileType tile, Color color)
        {
            _items[id] = new ItemData
            {
                id = id, itemName = name, category = ItemCategory.Placeable,
                displayColor = color, maxStack = 999, placesTile = tile,
                description = "可放置"
            };
        }
    }
}
