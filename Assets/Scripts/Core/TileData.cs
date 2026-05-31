using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 方块属性数据 - 定义每种方块的硬度、颜色、发光等
    /// </summary>
    [System.Serializable]
    public class TileProperties
    {
        public TileType type;
        public string displayName;
        public Color baseColor;
        public int hardness;          // 挖掘所需时间刻
        public int lightEmission;     // 发光强度 0-15
        public int dropItemId;        // 掉落物品ID
        public bool isSolid;          // 是否实体碰撞
        public bool isTransparent;    // 光线是否可穿透
        public int requiredPickPower; // 需要的最小镐力才能挖掘（0=任何工具可挖）
    }

    /// <summary>
    /// 方块数据注册表 - 静态初始化所有方块属性
    /// </summary>
    public static class TileRegistry
    {
        private static Dictionary<TileType, TileProperties> _registry;

        public static Dictionary<TileType, TileProperties> All
        {
            get
            {
                if (_registry == null) Initialize();
                return _registry;
            }
        }

        public static TileProperties Get(TileType type)
        {
            if (_registry == null) Initialize();
            return _registry.ContainsKey(type) ? _registry[type] : null;
        }

        private static void Initialize()
        {
            _registry = new Dictionary<TileType, TileProperties>();

            // 地表层 (requiredPickPower=0, 任何工具可挖)
            Register(TileType.ScorchedEarth, "焦土", new Color(0.20f, 0.14f, 0.09f), 3, 0, 1, true, false, 0);
            Register(TileType.ToxicMoss, "变异苔", new Color(0.09f, 0.43f, 0.15f), 3, 0, 2, true, false, 0);
            Register(TileType.Asphalt, "沥青", new Color(0.11f, 0.11f, 0.13f), 4, 0, 3, true, false, 0);
            Register(TileType.Sand, "沙石", new Color(0.55f, 0.51f, 0.35f), 2, 0, 4, true, false, 0);
            Register(TileType.Clay, "黏土", new Color(0.43f, 0.31f, 0.24f), 3, 0, 5, true, false, 0);
            Register(TileType.DeadWood, "枯木", new Color(0.27f, 0.20f, 0.12f), 2, 0, 6, true, false, 0);

            // 建筑材料
            Register(TileType.Concrete, "混凝土", new Color(0.28f, 0.28f, 0.31f), 5, 0, 10, true, false, 0);
            Register(TileType.RustedMetal, "锈铁", new Color(0.45f, 0.22f, 0.09f), 6, 0, 11, true, false, 1);
            Register(TileType.ReinforcedConcrete, "强化混凝土", new Color(0.35f, 0.35f, 0.38f), 8, 0, 12, true, false, 3);
            Register(TileType.BrokenBrick, "碎砖", new Color(0.23f, 0.21f, 0.19f), 2, 0, 13, true, false, 0);
            Register(TileType.CarbonFiber, "碳纤维", new Color(0.05f, 0.05f, 0.07f), 8, 0, 14, true, false, 1);
            Register(TileType.BulletproofGlass, "防弹玻璃", new Color(0.33f, 0.54f, 0.69f), 3, 0, 15, true, true, 0);

            // 工业材料
            Register(TileType.Pipe, "管道", new Color(0.22f, 0.35f, 0.35f), 6, 0, 20, true, false, 1);
            Register(TileType.Wire, "线缆", new Color(0.16f, 0.24f, 0.27f), 3, 0, 21, true, false, 1);
            Register(TileType.CircuitBoard, "电路板", new Color(0.08f, 0.31f, 0.24f), 7, 3, 22, true, false, 1);
            Register(TileType.VentDuct, "通风管道", new Color(0.30f, 0.30f, 0.33f), 4, 0, 23, true, false, 0);

            // 矿石
            Register(TileType.TitaniumOre, "钛矿石", new Color(0.51f, 0.58f, 0.63f), 10, 0, 30, true, false, 2);
            Register(TileType.NeonCrystal, "霓虹晶体", new Color(0.0f, 0.94f, 0.71f), 8, 10, 31, true, false, 2);
            Register(TileType.PlasmaCell, "等离子电池", new Color(0.90f, 0.51f, 0.0f), 12, 9, 32, true, false, 3);
            Register(TileType.QuantumChip, "量子芯片", new Color(0.67f, 0.20f, 0.90f), 16, 7, 33, true, false, 4);
            Register(TileType.CyberAlloy, "赛博合金", new Color(0.15f, 0.15f, 0.24f), 14, 0, 34, true, false, 2);

            // 发光方块
            Register(TileType.NeonPanel, "霓虹面板", new Color(0.90f, 0.0f, 0.50f), 5, 8, 40, true, false, 0);
            Register(TileType.HologramBlock, "全息方块", new Color(0.39f, 0.78f, 1.0f), 4, 12, 41, true, true, 0);
            Register(TileType.PlasmaLamp, "等离子灯", new Color(1.0f, 0.60f, 0.0f), 3, 11, 42, true, false, 0);

            // 自然/异变
            Register(TileType.ToxicSludge, "辐射淤泥", new Color(0.35f, 0.39f, 0.16f), 2, 2, 50, true, false, 0);
            Register(TileType.FleshBlock, "异变肉块", new Color(0.47f, 0.16f, 0.20f), 4, 0, 51, true, false, 0);
            Register(TileType.CrystalCluster, "紫晶簇", new Color(0.55f, 0.24f, 0.78f), 10, 5, 52, true, false, 2);
            Register(TileType.Obsidian, "黑曜石", new Color(0.07f, 0.04f, 0.11f), 18, 0, 53, true, false, 3);
            Register(TileType.IceCrystal, "冰晶", new Color(0.63f, 0.82f, 0.94f), 3, 4, 54, true, false, 0);

            // 液体（特殊处理）
            Register(TileType.ToxicWater, "辐射废液", new Color(0.30f, 0.50f, 0.10f), 0, 3, -1, false, true, 0);
            Register(TileType.Lava, "熔岩", new Color(0.90f, 0.30f, 0.0f), 0, 12, -1, false, true, 0);
            Register(TileType.Coolant, "冷却液", new Color(0.20f, 0.60f, 0.90f), 0, 2, -1, false, true, 0);

            // 功能方块
            Register(TileType.Platform, "平台", new Color(0.24f, 0.20f, 0.18f), 1, 0, 70, false, false, 0);
            Register(TileType.Ladder, "梯子", new Color(0.35f, 0.30f, 0.25f), 2, 0, 71, false, false, 0);

            // 地表可采集资源
            Register(TileType.CyberTreeTrunk, "赛博树干", new Color(0.15f, 0.45f, 0.25f), 4, 1, 80, true, false, 0);
            Register(TileType.CyberTreeCanopy, "赛博树冠", new Color(0.0f, 1.0f, 0.6f), 2, 4, 81, false, true, 0);
            Register(TileType.ScrapPile, "废金属堆", new Color(0.45f, 0.35f, 0.2f), 5, 0, 82, true, false, 0);
            Register(TileType.EnergyCrystal, "能量水晶", new Color(0.1f, 0.9f, 1.0f), 8, 7, 83, true, false, 3);
            Register(TileType.MutantCactus, "变异仙人掌", new Color(0.4f, 0.75f, 0.1f), 3, 2, 84, true, false, 0);
            Register(TileType.DataMoss, "数据苔藓", new Color(0.2f, 0.6f, 0.9f), 1, 3, 85, false, true, 0);

            // 特殊方块 (不可挖掘)
            Register(TileType.UnstableRift, "不稳定裂缝", new Color(0.4f, 0.1f, 0.6f),
                999, 12, -1, false, true, 999);

            // 不可破坏
            Register(TileType.ReinforcedSteel, "强化钢", new Color(0.06f, 0.06f, 0.09f), 9999, 0, -1, true, false, 999);
        }

        private static void Register(TileType type, string name, Color color, int hardness,
            int light, int dropId, bool solid = true, bool transparent = false, int pickPowerReq = 0)
        {
            _registry[type] = new TileProperties
            {
                type = type,
                displayName = name,
                baseColor = color,
                hardness = hardness,
                lightEmission = light,
                dropItemId = dropId,
                isSolid = solid,
                isTransparent = transparent,
                requiredPickPower = pickPowerReq
            };
        }
    }
}
