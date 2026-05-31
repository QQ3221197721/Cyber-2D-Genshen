using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 世界生成器 - 使用多层Perlin噪声生成赛博废土世界
    /// 参考Terraria: 地表→浅层→石层→深层→地狱
    /// 赛博版: 地表→废墟层→混凝土层→合金层→深渊层
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("地形参数")]
        [Range(0.005f, 0.05f)] public float terrainFrequency = 0.008f;
        [Range(5, 80)] public int terrainAmplitude = 40;
        public int surfaceLevel = 350; // 地表基准(从顶部算) - 大约世界高度的30%

        [Header("洞穴参数")]
        [Range(0.03f, 0.15f)] public float caveFrequency = 0.065f;
        [Range(0.25f, 0.5f)] public float caveThreshold = 0.38f;

        [Header("矿石")]
        [Range(0.001f, 0.02f)] public float titaniumChance = 0.008f;
        [Range(0.001f, 0.01f)] public float neonChance = 0.005f;
        [Range(0.001f, 0.008f)] public float plasmaChance = 0.004f;
        [Range(0.0005f, 0.005f)] public float quantumChance = 0.002f;

        private int _width;
        private int _height;
        private int _seed;
        private float _seedOffset;
        private int[] _surfaceHeights;

        public int[] SurfaceHeights => _surfaceHeights;

        /// <summary>
        /// 生成完整世界
        /// </summary>
        public void Generate(int seed)
        {
            var gm = GameManager.Instance;
            _width = gm.worldWidth;
            _height = gm.worldHeight;
            _seed = seed;

            Random.InitState(seed);
            float seedOffset = Random.Range(0f, 10000f);
            _seedOffset = seedOffset;

            // Step 1: 地表高度
            GenerateSurfaceHeights(seedOffset);

            // Step 2: 填充基础地形
            FillBaseTerrain(seedOffset);

            // Step 3: 雕刻洞穴
            CarveCaves(seedOffset);

            // Step 4: 放置矿石
            PlaceOres(seedOffset);

            // Step 5: 基岩层
            PlaceBedrock();

            // Step 6: 生成废墟建筑
            var ruinGen = GetComponent<RuinGenerator>();
            if (ruinGen != null)
                ruinGen.GenerateRuins(_surfaceHeights);

            // Step 7: 放置地表可采集资源
            PlaceSurfaceResources();

            // Step 8: 放置不稳定裂缝（副本入口）
            PlaceUnstableRifts();

            // Step 9: 计算光照
            var lighting = GetComponent<LightingSystem>();
            if (lighting != null)
                lighting.CalculateFullLighting();

            GameManager.Instance.OnWorldGenerated?.Invoke();
        }

        private void GenerateSurfaceHeights(float seedOffset)
        {
            _surfaceHeights = new int[_width];
            for (int x = 0; x < _width; x++)
            {
                float n1 = Mathf.PerlinNoise((x + seedOffset) * terrainFrequency, seedOffset);
                float n2 = Mathf.PerlinNoise((x + seedOffset) * terrainFrequency * 2.5f, seedOffset + 100f) * 0.4f;
                float n3 = Mathf.PerlinNoise((x + seedOffset) * terrainFrequency * 5f, seedOffset + 200f) * 0.2f;

                float combined = (n1 + n2 + n3) / 1.6f;
                _surfaceHeights[x] = surfaceLevel + Mathf.RoundToInt(combined * terrainAmplitude);
            }
        }

        private void FillBaseTerrain(float seedOffset)
        {
            var gm = GameManager.Instance;
            var biome = GetComponent<BiomeSystem>();

            for (int x = 0; x < _width; x++)
            {
                int surfY = _surfaceHeights[x];
                BiomeType bio = biome != null ? biome.GetBiomeAt(x, seedOffset) : BiomeType.Wasteland;

                for (int y = 0; y < _height; y++)
                {
                    if (y < surfY)
                    {
                        // 天空
                        gm.WorldTiles[x, y] = TileType.Air;
                    }
                    else if (y == surfY)
                    {
                        // 地表层
                        gm.WorldTiles[x, y] = GetSurfaceTile(bio);
                    }
                    else if (y < surfY + 8)
                    {
                        // 浅层
                        gm.WorldTiles[x, y] = GetShallowTile(bio);
                    }
                    else if (y < surfY + 80)
                    {
                        // 混凝土/废壟层 (泰拉瑞亚的土层)
                        gm.WorldTiles[x, y] = TileType.Concrete;
                    }
                    else if (y < surfY + 300)
                    {
                        // 合金层 (泰拉瑞亚的石层)
                        gm.WorldTiles[x, y] = TileType.CyberAlloy;
                    }
                    else if (y < _height - 150)
                    {
                        // 深渊层 (泰拉瑞亚的深层)
                        gm.WorldTiles[x, y] = TileType.Obsidian;
                    }
                    else
                    {
                        // 熔核层 (泰拉瑞亚的地狱)
                        gm.WorldTiles[x, y] = Random.value > 0.1f ? TileType.Obsidian : TileType.Air;
                    }

                    // 背景墙
                    if (y > surfY)
                        gm.WallMap[x, y] = 1;
                }
            }
        }

        private TileType GetSurfaceTile(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.ToxicSwamp: return TileType.ToxicMoss;
                case BiomeType.RuinedCity: return TileType.Asphalt;
                case BiomeType.CrystalCave: return TileType.CrystalCluster;
                case BiomeType.FleshBiome: return TileType.FleshBlock;
                default: return TileType.ScorchedEarth;
            }
        }

        private TileType GetShallowTile(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.RuinedCity: return TileType.Concrete;
                case BiomeType.FleshBiome: return TileType.FleshBlock;
                default: return TileType.ScorchedEarth;
            }
        }

        private void CarveCaves(float seedOffset)
        {
            var gm = GameManager.Instance;
            for (int x = 1; x < _width - 1; x++)
            {
                int startY = _surfaceHeights[x] + 4;
                for (int y = startY; y < _height - 5; y++)
                {
                    float c1 = Mathf.PerlinNoise((x + seedOffset) * caveFrequency, (y + seedOffset) * caveFrequency);
                    float c2 = Mathf.PerlinNoise((x + seedOffset + 300f) * caveFrequency * 1.5f,
                        (y + seedOffset) * caveFrequency * 1.5f);

                    float cave = (c1 + c2) / 2f;
                    float depthFactor = 1f - (float)(y - startY) / (_height - startY);
                    float threshold = caveThreshold + depthFactor * 0.04f;

                    if (cave < threshold - 0.08f)
                    {
                        gm.WorldTiles[x, y] = TileType.Air;
                    }
                }
            }
        }

        private void PlaceOres(float seedOffset)
        {
            var gm = GameManager.Instance;
            for (int x = 2; x < _width - 2; x++)
            {
                int surfY = _surfaceHeights[x];
                for (int y = surfY + 6; y < _height - 5; y++)
                {
                    if (gm.WorldTiles[x, y] == TileType.Air) continue;

                    float depth = (float)(y - surfY) / (_height - surfY);
                    float oreNoise = Mathf.PerlinNoise((x + seedOffset + 1000f) * 0.15f, (y + seedOffset) * 0.15f);

                    // 钛矿 - 浅层到中层
                    if (depth > 0.05f && depth < 0.45f && oreNoise > 0.7f && Random.value < titaniumChance)
                        PlaceOreVein(x, y, TileType.TitaniumOre, 3);
                    // 霓虹晶 - 中层
                    else if (depth > 0.2f && depth < 0.6f && oreNoise > 0.75f && Random.value < neonChance)
                        PlaceOreVein(x, y, TileType.NeonCrystal, 2);
                    // 等离子电池 - 中深层
                    else if (depth > 0.35f && depth < 0.75f && oreNoise > 0.78f && Random.value < plasmaChance)
                        PlaceOreVein(x, y, TileType.PlasmaCell, 2);
                    // 量子芯片 - 深层
                    else if (depth > 0.6f && oreNoise > 0.82f && Random.value < quantumChance)
                        PlaceOreVein(x, y, TileType.QuantumChip, 2);
                    // 电路板 - 中层
                    else if (depth > 0.15f && depth < 0.5f && Random.value < 0.006f)
                        PlaceOreVein(x, y, TileType.CircuitBoard, 2);
                    // 锈铁 - 浅层
                    else if (depth < 0.3f && Random.value < 0.01f)
                        gm.WorldTiles[x, y] = TileType.RustedMetal;
                }
            }
        }

        private void PlaceOreVein(int startX, int startY, TileType ore, int radius)
        {
            var gm = GameManager.Instance;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = startX + dx;
                    int ny = startY + dy;
                    if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                    if (Random.value > 0.55f) continue;
                    if (gm.WorldTiles[nx, ny] != TileType.Air && gm.WorldTiles[nx, ny] != TileType.ReinforcedSteel)
                        gm.WorldTiles[nx, ny] = ore;
                }
            }
        }

        private void PlaceBedrock()
        {
            var gm = GameManager.Instance;
            for (int x = 0; x < _width; x++)
            {
                for (int y = _height - 4; y < _height; y++)
                {
                    gm.WorldTiles[x, y] = TileType.ReinforcedSteel;
                }
            }
        }

        /// <summary>
        /// 获取玩家出生点
        /// </summary>
        public Vector2 GetSpawnPoint()
        {
            int spawnX = _width / 2;
            int spawnY = _surfaceHeights[spawnX] - 3;
            return new Vector2(spawnX + 0.5f, spawnY);
        }

        /// <summary>
        /// 获取指定x坐标的生物群落类型
        /// </summary>
        private BiomeType GetBiomeAt(int x)
        {
            var biome = GetComponent<BiomeSystem>();
            if (biome != null)
                return biome.GetBiomeAt(x, _seedOffset);
            return BiomeType.Wasteland;
        }

        /// <summary>
        /// 放置地表可采集资源（赛博树、废金属堆、能量水晶、变异仙人掌、数据苔藓）
        /// </summary>
        private void PlaceSurfaceResources()
        {
            var gm = GameManager.Instance;
            int w = gm.worldWidth;

            // 1. 放置赛博树（类似泰拉瑞亚树，多格高的结构）
            for (int attempt = 0; attempt < w / 25; attempt++)
            {
                int x = Random.Range(20, w - 20);
                int surfY = _surfaceHeights[x];

                // 检查地面是否平坦（左右1格高度差不超过1）
                if (x > 0 && x < w - 1 &&
                    Mathf.Abs(_surfaceHeights[x - 1] - surfY) <= 1 &&
                    Mathf.Abs(_surfaceHeights[x + 1] - surfY) <= 1)
                {
                    // 检查上方是否有空间
                    bool hasSpace = true;
                    int treeHeight = Random.Range(4, 8);
                    for (int dy = 1; dy <= treeHeight + 2; dy++)
                    {
                        if (surfY - dy < 0 || gm.WorldTiles[x, surfY - dy] != TileType.Air)
                        {
                            hasSpace = false;
                            break;
                        }
                    }

                    if (hasSpace)
                    {
                        // 放置树干
                        for (int dy = 1; dy <= treeHeight; dy++)
                        {
                            gm.WorldTiles[x, surfY - dy] = TileType.CyberTreeTrunk;
                        }
                        // 放置树冠（3x2区域）
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = 0; dy < 2; dy++)
                            {
                                int cx = x + dx;
                                int cy = surfY - treeHeight - 1 - dy;
                                if (cx >= 0 && cx < w && cy >= 0 && gm.WorldTiles[cx, cy] == TileType.Air)
                                {
                                    gm.WorldTiles[cx, cy] = TileType.CyberTreeCanopy;
                                }
                            }
                        }
                    }
                }
            }

            // 2. 放置废金属堆（1-3格宽，1-2格高，主要在RuinedCity生物群落）
            for (int attempt = 0; attempt < w / 30; attempt++)
            {
                int x = Random.Range(10, w - 10);
                int surfY = _surfaceHeights[x];
                BiomeType biome = GetBiomeAt(x);

                float spawnChance = (biome == BiomeType.RuinedCity) ? 0.7f : 0.2f;
                if (Random.value < spawnChance && surfY - 1 >= 0 && gm.WorldTiles[x, surfY - 1] == TileType.Air)
                {
                    int pileWidth = Random.Range(1, 4);
                    int pileHeight = Random.Range(1, 3);
                    for (int dx = 0; dx < pileWidth; dx++)
                    {
                        for (int dy = 0; dy < pileHeight; dy++)
                        {
                            int px = x + dx;
                            int py = surfY - 1 - dy;
                            if (px < w && py >= 0 && gm.WorldTiles[px, py] == TileType.Air)
                            {
                                if (dy == 0 || Random.value < 0.6f)
                                {
                                    gm.WorldTiles[px, py] = TileType.ScrapPile;
                                }
                            }
                        }
                    }
                }
            }

            // 3. 放置能量水晶（稀有，地表或浅层，发光）
            for (int attempt = 0; attempt < w / 80; attempt++)
            {
                int x = Random.Range(30, w - 30);
                int surfY = _surfaceHeights[x];
                int depth = Random.Range(0, 10);
                int y = surfY + depth;

                if (y < gm.worldHeight && gm.WorldTiles[x, y] != TileType.Air &&
                    gm.WorldTiles[x, y] != TileType.ReinforcedSteel)
                {
                    int clusterSize = Random.Range(1, 4);
                    gm.WorldTiles[x, y] = TileType.EnergyCrystal;
                    for (int i = 1; i < clusterSize; i++)
                    {
                        int ox = x + Random.Range(-1, 2);
                        int oy = y + Random.Range(-1, 2);
                        if (ox >= 0 && ox < w && oy >= 0 && oy < gm.worldHeight &&
                            gm.WorldTiles[ox, oy] != TileType.Air &&
                            gm.WorldTiles[ox, oy] != TileType.ReinforcedSteel)
                        {
                            gm.WorldTiles[ox, oy] = TileType.EnergyCrystal;
                        }
                    }
                }
            }

            // 4. 放置变异仙人掌（主要在 RadDesert 生物群落）
            for (int attempt = 0; attempt < w / 40; attempt++)
            {
                int x = Random.Range(10, w - 10);
                int surfY = _surfaceHeights[x];
                BiomeType biome = GetBiomeAt(x);

                if (biome == BiomeType.RadDesert || (biome == BiomeType.ToxicSwamp && Random.value < 0.3f))
                {
                    if (surfY - 1 >= 0 && gm.WorldTiles[x, surfY - 1] == TileType.Air)
                    {
                        int cactusHeight = Random.Range(2, 5);
                        for (int dy = 1; dy <= cactusHeight; dy++)
                        {
                            if (surfY - dy >= 0 && gm.WorldTiles[x, surfY - dy] == TileType.Air)
                            {
                                gm.WorldTiles[x, surfY - dy] = TileType.MutantCactus;
                            }
                        }
                        // 30%概率有分支
                        if (Random.value < 0.3f && cactusHeight >= 3)
                        {
                            int branchY = surfY - Random.Range(2, cactusHeight);
                            int branchDir = Random.value > 0.5f ? 1 : -1;
                            int bx = x + branchDir;
                            if (bx >= 0 && bx < w && gm.WorldTiles[bx, branchY] == TileType.Air)
                            {
                                gm.WorldTiles[bx, branchY] = TileType.MutantCactus;
                            }
                        }
                    }
                }
            }

            // 5. 放置数据苔藓（附着在废墟建筑表面，DataCorruption生物群落多见）
            for (int attempt = 0; attempt < w / 15; attempt++)
            {
                int x = Random.Range(5, w - 5);
                int surfY = _surfaceHeights[x];
                BiomeType biome = GetBiomeAt(x);

                float mossChance = (biome == BiomeType.DataCorruption) ? 0.5f : 0.1f;
                if (Random.value < mossChance)
                {
                    for (int dy = 1; dy <= 3; dy++)
                    {
                        int py = surfY - dy;
                        if (py >= 0 && gm.WorldTiles[x, py] == TileType.Air)
                        {
                            bool hasAdjacent = false;
                            if (x > 0 && gm.WorldTiles[x - 1, py] != TileType.Air) hasAdjacent = true;
                            if (x < w - 1 && gm.WorldTiles[x + 1, py] != TileType.Air) hasAdjacent = true;
                            if (py + 1 < gm.worldHeight && gm.WorldTiles[x, py + 1] != TileType.Air) hasAdjacent = true;

                            if (hasAdjacent && Random.value < 0.4f)
                            {
                                gm.WorldTiles[x, py] = TileType.DataMoss;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在地下放置不稳定裂缝（副本入口）
        /// </summary>
        private void PlaceUnstableRifts()
        {
            var gm = GameManager.Instance;
            int riftCount = Random.Range(5, 9); // 5-8个裂缝

            for (int i = 0; i < riftCount; i++)
            {
                int attempts = 0;
                while (attempts < 50)
                {
                    int x = Random.Range(50, gm.worldWidth - 50);
                    int surfY = _surfaceHeights[x];
                    // 深度范围：地表下30-200格
                    int y = surfY + Random.Range(30, 200);

                    if (y < gm.worldHeight - 10 &&
                        gm.WorldTiles[x, y] != TileType.Air &&
                        gm.WorldTiles[x, y] != TileType.ReinforcedSteel)
                    {
                        // 先挖出一个5x5的空间
                        for (int dx = -2; dx <= 2; dx++)
                            for (int dy = -2; dy <= 2; dy++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx > 0 && nx < gm.worldWidth && ny > 0 && ny < gm.worldHeight)
                                    gm.WorldTiles[nx, ny] = TileType.Air;
                            }

                        // 中心放置裂缝方块（2x2大小更显眼）
                        gm.WorldTiles[x, y] = TileType.UnstableRift;
                        gm.WorldTiles[x + 1, y] = TileType.UnstableRift;
                        gm.WorldTiles[x, y + 1] = TileType.UnstableRift;
                        gm.WorldTiles[x + 1, y + 1] = TileType.UnstableRift;

                        break;
                    }
                    attempts++;
                }
            }
        }
    }
}
