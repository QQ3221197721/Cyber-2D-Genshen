using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 合成系统 - 管理所有合成配方和合成操作
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        private static List<CraftingRecipe> _recipes;

        public static List<CraftingRecipe> AllRecipes
        {
            get
            {
                if (_recipes == null) InitializeRecipes();
                return _recipes;
            }
        }

        private void Awake() { Instance = this; }

        private static void InitializeRecipes()
        {
            _recipes = new List<CraftingRecipe>
            {
                // === 工具 ===
                new CraftingRecipe(101, 1, "钛合金镐", new[] {(30, 12), (11, 5)}),
                new CraftingRecipe(102, 1, "量子钻头", new[] {(33, 3), (32, 2), (30, 8)}),
                new CraftingRecipe(103, 1, "等离子切割器", new[] {(32, 3), (30, 5), (22, 2)}),

                // === 近战武器 ===
                new CraftingRecipe(200, 1, "废铁刀", new[] {(11, 8)}),
                new CraftingRecipe(201, 1, "锈蚀砍刀", new[] {(11, 12), (14, 3)}),
                new CraftingRecipe(202, 1, "钛合金长剑", new[] {(30, 15), (11, 5)}),
                new CraftingRecipe(203, 1, "等离子刃", new[] {(32, 4), (30, 8)}),
                new CraftingRecipe(204, 1, "量子大剑", new[] {(33, 5), (34, 10)}),
                new CraftingRecipe(205, 1, "螳螂刀", new[] {(30, 12), (31, 4)}),
                new CraftingRecipe(206, 1, "单分子线", new[] {(33, 4), (31, 6)}),
                new CraftingRecipe(207, 1, "霓虹武士刀", new[] {(31, 8), (30, 6), (22, 3)}),

                // === 远程武器 ===
                new CraftingRecipe(250, 1, "手制左轮", new[] {(11, 10), (10, 5)}),
                new CraftingRecipe(251, 1, "废土猎枪", new[] {(11, 15), (20, 3)}),
                new CraftingRecipe(252, 1, "脉冲步枪", new[] {(30, 8), (22, 4), (31, 2)}),
                new CraftingRecipe(253, 1, "等离子炮", new[] {(32, 5), (30, 10), (22, 3)}),
                new CraftingRecipe(254, 1, "磁轨枪", new[] {(33, 6), (34, 12), (22, 8)}),

                // === 可放置方块 ===
                new CraftingRecipe(500, 10, "混凝土块x10", new[] {(10, 10)}),
                new CraftingRecipe(501, 5, "霓虹灯管x5", new[] {(31, 2), (15, 1)}),
                new CraftingRecipe(502, 10, "平台x10", new[] {(1, 5)}),
                new CraftingRecipe(503, 5, "全息方块x5", new[] {(22, 3), (31, 1)}),
                new CraftingRecipe(504, 3, "等离子灯x3", new[] {(32, 1), (15, 2)}),

                // === 消耗品 ===
                new CraftingRecipe(300, 3, "纳米修复剂x3", new[] {(31, 1), (2, 3)}),
                new CraftingRecipe(301, 1, "高级修复剂", new[] {(33, 1), (31, 2)}),
                new CraftingRecipe(302, 3, "能量饮料x3", new[] {(32, 1), (31, 1)}),

                // === Boss召唤物 ===
                new CraftingRecipe(400, 1, "损坏的信号器", new[] {(22, 10), (31, 5)}),
                new CraftingRecipe(401, 1, "腐蚀数据核心", new[] {(33, 3), (22, 8), (51, 10)}),
                new CraftingRecipe(402, 1, "过载电容器", new[] {(32, 5), (34, 8), (22, 5)}),

                // === 地表资源合成 ===
                new CraftingRecipe(505, 5, "霓虹火把x5", new[] {(80, 1), (83, 1)}),
                new CraftingRecipe(150, 1, "废铁护甲", new[] {(82, 15), (11, 5)}),
                new CraftingRecipe(104, 1, "数据提取器", new[] {(85, 10), (22, 3)}),
                new CraftingRecipe(305, 3, "变异药剂x3", new[] {(84, 5), (81, 3)}),
                new CraftingRecipe(86, 1, "能量电池", new[] {(83, 5), (21, 3)}),
            };
        }

        /// <summary>
        /// 检查是否能合成某配方
        /// </summary>
        public static bool CanCraft(CraftingRecipe recipe)
        {
            var inv = Inventory.Instance;
            if (inv == null) return false;

            foreach (var mat in recipe.materials)
            {
                if (!inv.HasItem(mat.itemId, mat.count))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 执行合成
        /// </summary>
        public static bool Craft(CraftingRecipe recipe)
        {
            if (!CanCraft(recipe)) return false;

            var inv = Inventory.Instance;

            // 消耗材料
            foreach (var mat in recipe.materials)
            {
                inv.RemoveItem(mat.itemId, mat.count);
            }

            // 给予结果
            inv.AddItem(recipe.resultItemId, recipe.resultCount);

            Debug.Log($"[合成] 成功合成: {recipe.name} x{recipe.resultCount}");
            return true;
        }

        /// <summary>
        /// 获取所有可合成的配方
        /// </summary>
        public static List<CraftingRecipe> GetAvailableRecipes()
        {
            List<CraftingRecipe> available = new List<CraftingRecipe>();
            foreach (var recipe in AllRecipes)
            {
                if (CanCraft(recipe))
                    available.Add(recipe);
            }
            return available;
        }
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public int resultItemId;
        public int resultCount;
        public string name;
        public CraftMaterial[] materials;

        public CraftingRecipe(int result, int count, string recipeName, (int id, int cnt)[] mats)
        {
            resultItemId = result;
            resultCount = count;
            name = recipeName;
            materials = new CraftMaterial[mats.Length];
            for (int i = 0; i < mats.Length; i++)
                materials[i] = new CraftMaterial { itemId = mats[i].id, count = mats[i].cnt };
        }
    }

    [System.Serializable]
    public struct CraftMaterial
    {
        public int itemId;
        public int count;
    }
}
