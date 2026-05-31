using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 废墟建筑生成器 - 在地表生成赛博朋克风格的废墟建筑
    /// </summary>
    public class RuinGenerator : MonoBehaviour
    {
        [Header("参数")]
        public int maxBuildings = 18;
        public int minWidth = 4;
        public int maxWidth = 10;
        public int minHeight = 5;
        public int maxHeight = 25;

        public void GenerateRuins(int[] surfaceHeights)
        {
            var gm = GameManager.Instance;
            int worldWidth = gm.worldWidth;

            for (int i = 0; i < maxBuildings; i++)
            {
                int bx = Random.Range(20, worldWidth - 20);
                int bw = Random.Range(minWidth, maxWidth + 1);
                int bh = Random.Range(minHeight, maxHeight + 1);
                int by = surfaceHeights[bx];

                // 只在废墟都市生物群落生成建筑
                var biome = GetComponent<BiomeSystem>();
                if (biome != null)
                {
                    float seedOffset = Random.Range(0f, 10000f);
                    if (biome.GetBiomeAt(bx, seedOffset) != BiomeType.RuinedCity)
                    {
                        // 降低非城市区域的建筑高度
                        bh = Mathf.Min(bh, 8);
                        if (Random.value > 0.3f) continue;
                    }
                }

                GenerateBuilding(bx, by, bw, bh);
            }

            // 生成管道系统
            GeneratePipeNetworks(surfaceHeights);

            // 生成霓虹招牌
            GenerateNeonSigns(surfaceHeights);
        }

        private void GenerateBuilding(int bx, int by, int bw, int bh)
        {
            var gm = GameManager.Instance;
            int decay = Random.Range(0, bh / 3); // 破损程度

            for (int dx = 0; dx < bw; dx++)
            {
                int columnHeight = bh - Random.Range(0, decay);
                for (int dy = 0; dy < columnHeight; dy++)
                {
                    int wx = bx + dx;
                    int wy = by - 1 - dy;
                    if (wx < 0 || wx >= gm.worldWidth || wy < 0) continue;

                    if (dx == 0 || dx == bw - 1)
                    {
                        // 外墙
                        if (Random.value > 0.12f)
                        {
                            // 8%概率替换为幸运废墟方块
                            if (Random.value < 0.08f)
                                gm.WorldTiles[wx, wy] = TileType.LuckyRuin;
                            else
                                gm.WorldTiles[wx, wy] = TileType.Concrete;
                        }
                        else
                            gm.WorldTiles[wx, wy] = TileType.Air; // 破洞
                    }
                    else if (dy == 0 || dy == columnHeight - 1)
                    {
                        // 地板/天花板
                        gm.WorldTiles[wx, wy] = TileType.ReinforcedConcrete;
                    }
                    else if (dy % 4 == 0)
                    {
                        // 楼层分隔
                        gm.WorldTiles[wx, wy] = Random.value > 0.3f ? TileType.RustedMetal : TileType.Air;
                    }
                    else
                    {
                        // 内部空间
                        gm.WorldTiles[wx, wy] = TileType.Air;
                    }
                }
            }

            // 在建筑顶部放置天线/装饰
            if (bh > 10 && Random.value > 0.5f)
            {
                int antennaX = bx + bw / 2;
                int antennaY = by - bh - 1;
                if (antennaY > 0 && antennaX < gm.worldWidth)
                {
                    for (int ay = 0; ay < 3; ay++)
                    {
                        if (antennaY - ay > 0)
                            gm.WorldTiles[antennaX, antennaY - ay] = TileType.Wire;
                    }
                }
            }
        }

        private void GeneratePipeNetworks(int[] surfaceHeights)
        {
            var gm = GameManager.Instance;
            int worldWidth = gm.worldWidth;

            for (int i = 0; i < 12; i++)
            {
                int py = surfaceHeights[worldWidth / 2] + 5 + Random.Range(0, 45);
                int sx = Random.Range(0, (int)(worldWidth * 0.4f));
                int length = 25 + Random.Range(0, 80);
                int ex = Mathf.Min(sx + length, worldWidth - 1);

                for (int x = sx; x < ex; x++)
                {
                    if (py >= gm.worldHeight) continue;
                    if (gm.WorldTiles[x, py] != TileType.Air)
                        gm.WorldTiles[x, py] = TileType.Pipe;
                    // 偶尔有分支
                    if (Random.value < 0.03f && py + 1 < gm.worldHeight)
                    {
                        for (int vy = 0; vy < Random.Range(3, 8); vy++)
                        {
                            if (py + vy < gm.worldHeight && gm.WorldTiles[x, py + vy] != TileType.Air)
                                gm.WorldTiles[x, py + vy] = TileType.Pipe;
                        }
                    }
                }
            }
        }

        private void GenerateNeonSigns(int[] surfaceHeights)
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < 20; i++)
            {
                int x = Random.Range(10, gm.worldWidth - 10);
                int y = surfaceHeights[x] - Random.Range(3, 15);
                if (y > 0 && y < gm.worldHeight)
                {
                    // 随机放置霓虹块
                    TileType neonType = Random.value > 0.5f ? TileType.NeonPanel : TileType.HologramBlock;
                    int signWidth = Random.Range(2, 5);
                    for (int dx = 0; dx < signWidth; dx++)
                    {
                        int nx = x + dx;
                        if (nx < gm.worldWidth)
                            gm.WorldTiles[nx, y] = neonType;
                    }
                }
            }
        }
    }
}
