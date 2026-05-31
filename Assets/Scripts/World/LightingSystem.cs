using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 动态光照系统 - BFS光线传播（类似Terraria的光照）
    /// </summary>
    public class LightingSystem : MonoBehaviour
    {
        [Header("设置")]
        public int maxLightLevel = 15;
        public float sunlightDecay = 3f; // 每穿过一个实体方块衰减

        /// <summary>
        /// 全局光照计算
        /// </summary>
        public void CalculateFullLighting()
        {
            var gm = GameManager.Instance;
            int w = gm.worldWidth;
            int h = gm.worldHeight;

            // 清空光照
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    gm.LightMap[x, y] = 0;

            // 阳光从顶部向下传播
            for (int x = 0; x < w; x++)
            {
                int sun = maxLightLevel;
                for (int y = 0; y < h; y++)
                {
                    TileType tile = gm.WorldTiles[x, y];
                    var props = TileRegistry.Get(tile);

                    if (tile != TileType.Air && tile != TileType.Platform)
                    {
                        bool transparent = props != null && props.isTransparent;
                        sun = Mathf.Max(0, sun - (transparent ? 1 : 3));
                    }

                    gm.LightMap[x, y] = (byte)Mathf.Max(gm.LightMap[x, y], sun);
                }
            }

            // 发光方块BFS扩散
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var props = TileRegistry.Get(gm.WorldTiles[x, y]);
                    if (props != null && props.lightEmission > 0)
                    {
                        SpreadLight(x, y, props.lightEmission);
                    }
                }
            }
        }

        /// <summary>
        /// BFS光线扩散
        /// </summary>
        public void SpreadLight(int sx, int sy, int level)
        {
            var gm = GameManager.Instance;
            Queue<(int x, int y, int l)> queue = new Queue<(int, int, int)>();
            queue.Enqueue((sx, sy, level));

            while (queue.Count > 0)
            {
                var (x, y, l) = queue.Dequeue();
                if (x < 0 || x >= gm.worldWidth || y < 0 || y >= gm.worldHeight || l <= 0)
                    continue;

                if (gm.LightMap[x, y] >= l) continue;
                gm.LightMap[x, y] = (byte)l;

                TileType tile = gm.WorldTiles[x, y];
                int cost = (tile == TileType.Air || tile == TileType.Platform) ? 1 : 2;
                int nextL = l - cost;

                if (nextL > 0)
                {
                    queue.Enqueue((x - 1, y, nextL));
                    queue.Enqueue((x + 1, y, nextL));
                    queue.Enqueue((x, y - 1, nextL));
                    queue.Enqueue((x, y + 1, nextL));
                }
            }
        }

        /// <summary>
        /// 局部光照更新（方块被挖掘/放置时）
        /// </summary>
        public void UpdateLocalLighting(int cx, int cy, int radius = 12)
        {
            var gm = GameManager.Instance;
            int w = gm.worldWidth;
            int h = gm.worldHeight;

            // 清空局部区域
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                        gm.LightMap[x, y] = 0;
                }
            }

            // 重新计算阳光列
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= w) continue;
                int sun = maxLightLevel;
                for (int y = 0; y < h; y++)
                {
                    TileType tile = gm.WorldTiles[x, y];
                    if (tile != TileType.Air && tile != TileType.Platform)
                        sun = Mathf.Max(0, sun - 3);
                    gm.LightMap[x, y] = (byte)Mathf.Max(gm.LightMap[x, y], sun);
                }
            }

            // 重新扩散附近发光方块
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    var props = TileRegistry.Get(gm.WorldTiles[x, y]);
                    if (props != null && props.lightEmission > 0)
                        SpreadLight(x, y, props.lightEmission);
                }
            }
        }
    }
}
