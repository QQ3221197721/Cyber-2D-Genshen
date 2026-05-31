using UnityEngine;

namespace CyberTerraria
{
    public enum BiomeType
    {
        Wasteland,      // 废土荒原 (森林)
        ToxicSwamp,     // 辐射沼泽 (丛林)
        RuinedCity,     // 废壟都市 (城市区)
        CrystalCave,    // 紫晶洞穴 (地下神殖)
        FleshBiome,     // 异变肉域 (猩红)
        DataCorruption, // 数据腐蚀 (腐化)
        NeonSanctuary,  // 霓虹净土 (神圣-困难模式后)
        IceCrystal,     // 冰晶冻原 (雪地)
        RadDesert,      // 辐射沙漠 (沙漠)
        ToxicOcean,     // 废液海岸 (海洋)
        DataCenter,     // 废弃数据中心 (地牢)
        MoltenCore      // 熔核深渊 (地狱)
    }

    /// <summary>
    /// 生物群落系统 - 决定世界不同区域的生态
    /// </summary>
    public class BiomeSystem : MonoBehaviour
    {
        [Range(0.003f, 0.02f)] public float biomeFrequency = 0.007f;

        private BiomeType[] _biomeMap;

        public BiomeType GetBiomeAt(int x, float seedOffset)
        {
            int worldWidth = GameManager.Instance != null ? GameManager.Instance.worldWidth : 6400;

            // 世界两侧是海洋
            float relX = (float)x / worldWidth;
            if (relX < 0.04f || relX > 0.96f) return BiomeType.ToxicOcean;

            // 世界中心区域是废壟都市(出生点)
            if (relX > 0.42f && relX < 0.58f) return BiomeType.RuinedCity;

            // 左侧有数据腐蚀
            if (relX > 0.15f && relX < 0.25f) return BiomeType.DataCorruption;

            // 右侧有异变肉域
            if (relX > 0.75f && relX < 0.85f) return BiomeType.FleshBiome;

            // 其他生物群落用噪声分布
            float noise = Mathf.PerlinNoise((x + seedOffset) * biomeFrequency, seedOffset + 500f);

            if (relX < 0.15f) // 左侧寒冷地带
                return noise < 0.5f ? BiomeType.IceCrystal : BiomeType.Wasteland;

            if (relX > 0.85f) // 右侧沙漠地带
                return noise < 0.5f ? BiomeType.RadDesert : BiomeType.Wasteland;

            // 中间地带混合
            if (noise < 0.25f) return BiomeType.ToxicSwamp;
            if (noise < 0.55f) return BiomeType.Wasteland;
            if (noise < 0.75f) return BiomeType.CrystalCave;
            return BiomeType.Wasteland;
        }

        /// <summary>
        /// 预计算整个世界的生物群落分布
        /// </summary>
        public void PrecomputeBiomes(int worldWidth, float seedOffset)
        {
            _biomeMap = new BiomeType[worldWidth];
            for (int x = 0; x < worldWidth; x++)
            {
                _biomeMap[x] = GetBiomeAt(x, seedOffset);
            }
        }

        public BiomeType GetCachedBiome(int x)
        {
            if (_biomeMap == null || x < 0 || x >= _biomeMap.Length)
                return BiomeType.Wasteland;
            return _biomeMap[x];
        }

        /// <summary>
        /// 获取生物群落的环境颜色
        /// </summary>
        public static Color GetBiomeAmbientColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.ToxicSwamp: return new Color(0.2f, 0.4f, 0.1f, 0.3f);
                case BiomeType.RuinedCity: return new Color(0.1f, 0.15f, 0.25f, 0.2f);
                case BiomeType.CrystalCave: return new Color(0.3f, 0.1f, 0.5f, 0.25f);
                case BiomeType.FleshBiome: return new Color(0.5f, 0.1f, 0.15f, 0.3f);
                default: return new Color(0.15f, 0.12f, 0.08f, 0.1f);
            }
        }

        /// <summary>
        /// 获取生物群落的天空颜色
        /// </summary>
        public static Color GetBiomeSkyColor(BiomeType biome, bool isDay)
        {
            if (isDay)
            {
                switch (biome)
                {
                    case BiomeType.ToxicSwamp: return new Color(0.35f, 0.45f, 0.2f);
                    case BiomeType.RuinedCity: return new Color(0.25f, 0.3f, 0.4f);
                    case BiomeType.CrystalCave: return new Color(0.3f, 0.2f, 0.45f);
                    case BiomeType.FleshBiome: return new Color(0.45f, 0.2f, 0.2f);
                    default: return new Color(0.4f, 0.35f, 0.25f);
                }
            }
            else
            {
                switch (biome)
                {
                    case BiomeType.ToxicSwamp: return new Color(0.05f, 0.1f, 0.02f);
                    case BiomeType.RuinedCity: return new Color(0.03f, 0.05f, 0.1f);
                    case BiomeType.CrystalCave: return new Color(0.08f, 0.03f, 0.12f);
                    case BiomeType.FleshBiome: return new Color(0.12f, 0.03f, 0.03f);
                    default: return new Color(0.04f, 0.03f, 0.06f);
                }
            }
        }
    }
}
