namespace CyberTerraria
{
    /// <summary>
    /// 所有方块/瓦片类型枚举 - 赛博朋克+废土风格
    /// </summary>
    public enum TileType
    {
        Air = 0,

        // === 地表层 ===
        ScorchedEarth = 1,    // 焦土
        ToxicMoss = 2,        // 变异苔藓
        Asphalt = 3,          // 沥青路面
        Sand = 4,             // 沙石
        Clay = 5,             // 黏土
        DeadWood = 6,         // 枯木

        // === 建筑材料 ===
        Concrete = 10,        // 混凝土
        RustedMetal = 11,     // 锈铁
        ReinforcedConcrete = 12, // 强化混凝土
        BrokenBrick = 13,     // 碎砖
        CarbonFiber = 14,     // 碳纤维
        BulletproofGlass = 15,// 防弹玻璃

        // === 工业材料 ===
        Pipe = 20,            // 管道
        Wire = 21,            // 线缆
        CircuitBoard = 22,    // 电路板
        VentDuct = 23,        // 通风管道

        // === 矿石/科技材料 ===
        TitaniumOre = 30,     // 钛矿石
        NeonCrystal = 31,     // 霓虹晶体
        PlasmaCell = 32,      // 等离子电池
        QuantumChip = 33,     // 量子芯片
        CyberAlloy = 34,      // 赛博合金

        // === 发光方块 ===
        NeonPanel = 40,       // 霓虹面板
        HologramBlock = 41,   // 全息方块
        PlasmaLamp = 42,      // 等离子灯

        // === 自然/异变 ===
        ToxicSludge = 50,     // 辐射淤泥
        FleshBlock = 51,      // 异变肉块
        CrystalCluster = 52,  // 紫晶簇
        Obsidian = 53,        // 黑曜石
        IceCrystal = 54,      // 冰晶

        // === 液体 ===
        ToxicWater = 60,      // 辐射废液
        Lava = 61,            // 熔岩
        Coolant = 62,         // 冷却液

        // === 功能方块 ===
        Platform = 70,        // 平台（可穿透）
        Ladder = 71,          // 梯子
        Door = 72,            // 门
        Chest = 73,           // 箱子
        CraftingStation = 74, // 合成台
        CyberBench = 75,      // 义体工作台

        // === 地表可采集资源 ===
        CyberTreeTrunk = 80,  // 赛博树干 - 金属骨架树干，表面缠绕发光线缆
        CyberTreeCanopy = 81, // 赛博树冠 - 全息投影树叶，散发霓虹光
        ScrapPile = 82,       // 废金属堆 - 锈蚀机械零件堆积
        EnergyCrystal = 83,   // 能量水晶 - 地表裸露的高能晶体
        MutantCactus = 84,    // 变异仙人掌 - 辐射变异后的多刺植物
        DataMoss = 85,        // 数据苔藓 - 寄生在废墟表面的信息生命体

        // === 特殊方块 ===
        UnstableRift = 90,    // 不稳定裂缝
        LuckyRuin = 91,       // 幸运废墟方块（可摧毁/可修复）
        RepairedRuin = 92,    // 已修复遗迹（修复后的状态，不可再修复）

        // === 不可破坏 ===
        ReinforcedSteel = 99, // 强化钢（基岩）
    }
}
