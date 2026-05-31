using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 分块管理器 - 管理Tilemap渲染，只渲染摄像机可见区域
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("引用")]
        public Tilemap groundTilemap;
        public Tilemap backgroundTilemap;
        public Tilemap liquidTilemap;

        [Header("设置")]
        public int chunkSize = 32;
        public int renderDistance = 3; // 渲染周围几个chunk

        private const int VariantCount = 3;
        private Dictionary<TileType, TileBase[]> _tileCache = new Dictionary<TileType, TileBase[]>();
        private HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
        private Vector2Int _lastPlayerChunk;
        private Camera _cam;
        private FogOfWarSystem _fogSystem;

        // 副本数据源切换
        private TileType[,] _dungeonTiles;
        private int _dungeonWidth, _dungeonHeight;
        private bool _inDungeon = false;

        private void Awake()
        {
            // 必须在Awake中生成TileAssets，因为ForceRefresh可能在Start之前被调用
            GenerateTileAssets();
        }

        private void Start()
        {
            _cam = Camera.main;
            _fogSystem = FogOfWarSystem.Instance;
        }

        private void LateUpdate()
        {
            if (!_inDungeon && (GameManager.Instance == null || GameManager.Instance.WorldTiles == null)) return;

            // 用玩家位置决定加载哪些chunk，而不是自己的位置
            Vector3 trackPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : transform.position;

            Vector2Int playerChunk = GetChunkAt(trackPos);
            if (playerChunk != _lastPlayerChunk)
            {
                UpdateVisibleChunks(playerChunk);
                _lastPlayerChunk = playerChunk;
            }
        }

        /// <summary>
        /// 生成所有Tile类型对应的TileBase资产（每种类型生成多个变体）
        /// </summary>
        private void GenerateTileAssets()
        {
            foreach (var kvp in TileRegistry.All)
            {
                if (kvp.Key == TileType.Air) continue;
                TileBase[] variants = new TileBase[VariantCount];
                for (int v = 0; v < VariantCount; v++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = CreateColoredSprite(kvp.Value.baseColor, kvp.Key, v);
                    tile.color = Color.white;
                    variants[v] = tile;
                }
                _tileCache[kvp.Key] = variants;
            }
        }

        /// <summary>
        /// 根据位置hash选择变体
        /// </summary>
        private TileBase GetTileVariant(TileType type, int x, int y)
        {
            if (!_tileCache.ContainsKey(type)) return null;
            int hash = x * 7919 + y * 7927;
            int idx = ((hash % VariantCount) + VariantCount) % VariantCount;
            return _tileCache[type][idx];
        }

        #region Tile Category Classification

        private enum TileCategory { Default, Earth, Metal, Crystal, Plant, Concrete }

        private static TileCategory GetCategory(TileType type)
        {
            switch (type)
            {
                // 土类
                case TileType.ScorchedEarth:
                case TileType.ToxicMoss:
                case TileType.Sand:
                case TileType.Clay:
                case TileType.Asphalt:
                case TileType.DeadWood:
                    return TileCategory.Earth;

                // 金属/工业类
                case TileType.RustedMetal:
                case TileType.CarbonFiber:
                case TileType.Pipe:
                case TileType.Wire:
                case TileType.CyberAlloy:
                case TileType.VentDuct:
                case TileType.CircuitBoard:
                case TileType.ScrapPile:
                    return TileCategory.Metal;

                // 晶体/发光类
                case TileType.NeonCrystal:
                case TileType.PlasmaCell:
                case TileType.EnergyCrystal:
                case TileType.CrystalCluster:
                case TileType.IceCrystal:
                case TileType.NeonPanel:
                case TileType.HologramBlock:
                case TileType.PlasmaLamp:
                case TileType.QuantumChip:
                    return TileCategory.Crystal;

                // 植物类
                case TileType.CyberTreeTrunk:
                case TileType.CyberTreeCanopy:
                case TileType.MutantCactus:
                case TileType.DataMoss:
                    return TileCategory.Plant;

                // 混凝土/建筑类
                case TileType.Concrete:
                case TileType.ReinforcedConcrete:
                case TileType.BrokenBrick:
                case TileType.BulletproofGlass:
                    return TileCategory.Concrete;

                default:
                    return TileCategory.Default;
            }
        }

        #endregion

        #region Enhanced Texture Generation

        private Sprite CreateColoredSprite(Color baseColor, TileType tileType, int variantSeed)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // 基于变体种子的伪随机偏移
            int seed = variantSeed * 3571 + (int)tileType * 131;
            float noiseOffsetX = (seed % 100) * 0.37f;
            float noiseOffsetY = ((seed / 100) % 100) * 0.41f;

            TileCategory category = GetCategory(tileType);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    Color c = baseColor;
                    float brightness = 1f;

                    // === 基础Perlin噪声 ===
                    float noise = Mathf.PerlinNoise(
                        (x + noiseOffsetX) * 0.15f,
                        (y + noiseOffsetY) * 0.15f);
                    brightness *= 0.85f + noise * 0.3f;

                    // === 最外圈边框（1像素深色边缘）===
                    if (x == 0 || y == 0 || x == 15 || y == 15)
                    {
                        brightness *= 0.55f;
                    }
                    // === 次外圈（柔和过渡）===
                    else if (x == 1 || y == 1 || x == 14 || y == 14)
                    {
                        brightness *= 0.75f;
                    }
                    else
                    {
                        // === 根据类别添加材质细节（仅内部区域）===
                        switch (category)
                        {
                            case TileCategory.Earth:
                                brightness = ApplyEarthDetail(x, y, seed, brightness);
                                break;
                            case TileCategory.Metal:
                                c = ApplyMetalDetail(x, y, seed, c, ref brightness);
                                break;
                            case TileCategory.Crystal:
                                c = ApplyCrystalDetail(x, y, seed, c, ref brightness);
                                break;
                            case TileCategory.Plant:
                                brightness = ApplyPlantDetail(x, y, seed, brightness);
                                break;
                            case TileCategory.Concrete:
                                brightness = ApplyConcreteDetail(x, y, seed, brightness);
                                break;
                        }
                    }

                    c = new Color(
                        Mathf.Clamp01(c.r * brightness),
                        Mathf.Clamp01(c.g * brightness),
                        Mathf.Clamp01(c.b * brightness),
                        1f);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        /// <summary>
        /// 土类细节：深色小点（石子）+ 水平暗线
        /// </summary>
        private float ApplyEarthDetail(int x, int y, int seed, float brightness)
        {
            // 散布2-4个深色小点（石子/碎屑）
            int dotCount = 2 + (seed % 3); // 2-4个
            for (int i = 0; i < dotCount; i++)
            {
                int dx = ((seed * (i + 1) * 37) % 12) + 2;
                int dy = ((seed * (i + 1) * 53) % 12) + 2;
                if ((x == dx || x == dx + 1) && (y == dy))
                {
                    brightness *= 0.65f;
                }
            }

            // 微妙的水平纹理线（每隔4-5像素一条）
            int lineGap = 4 + (seed % 2);
            if (y > 2 && y < 14 && (y % lineGap == (seed % lineGap)))
            {
                brightness *= 0.9f;
            }

            return brightness;
        }

        /// <summary>
        /// 金属类细节：高光铆钉 + 锈斑 + 纵向条纹
        /// </summary>
        private Color ApplyMetalDetail(int x, int y, int seed, Color c, ref float brightness)
        {
            // 亮点（螺丝/铆钉效果）
            if ((x == 3 && y == 3) || (x == 12 && y == 12) ||
                (x == 3 && y == 12) || (x == 12 && y == 3))
            {
                brightness *= 1.5f;
                c = Color.Lerp(c, Color.white, 0.3f);
            }

            // 不规则锈斑：2-3个随机位置放2x2像素暗色区域
            int rustCount = 2 + (seed % 2);
            for (int i = 0; i < rustCount; i++)
            {
                int rx = ((seed * (i + 3) * 43) % 10) + 3;
                int ry = ((seed * (i + 3) * 67) % 10) + 3;
                if ((x == rx || x == rx + 1) && (y == ry || y == ry + 1))
                {
                    brightness *= 0.6f;
                    c = Color.Lerp(c, new Color(0.3f, 0.15f, 0.05f), 0.3f);
                }
            }

            // 轻微纵向条纹（金属拉丝感）
            if (x % 3 == (seed % 3))
            {
                brightness *= 0.92f;
            }

            return c;
        }

        /// <summary>
        /// 晶体/发光类细节：中心渐变 + 白色高光 + 高饱和度
        /// </summary>
        private Color ApplyCrystalDetail(int x, int y, int seed, Color c, ref float brightness)
        {
            // 中心到边缘的亮度渐变（中心比边缘亮30%）
            float cx = (x - 7.5f) / 7.5f;
            float cy = (y - 7.5f) / 7.5f;
            float distFromCenter = Mathf.Sqrt(cx * cx + cy * cy);
            float centerGlow = 1.3f - distFromCenter * 0.3f;
            brightness *= Mathf.Clamp(centerGlow, 0.9f, 1.3f);

            // 1-2个白色高光点（模拟光折射）
            int hlx1 = 5 + (seed % 4);
            int hly1 = 5 + ((seed / 4) % 4);
            int hlx2 = 9 + (seed % 3);
            int hly2 = 9 + ((seed / 3) % 3);
            if ((x == hlx1 && y == hly1) || (x == hlx2 && y == hly2))
            {
                c = Color.Lerp(c, Color.white, 0.7f);
                brightness *= 1.4f;
            }

            // 整体颜色饱和度更高
            float gray = (c.r + c.g + c.b) / 3f;
            c = new Color(
                Mathf.Lerp(gray, c.r, 1.3f),
                Mathf.Lerp(gray, c.g, 1.3f),
                Mathf.Lerp(gray, c.b, 1.3f),
                1f);

            return c;
        }

        /// <summary>
        /// 植物类细节：纵向纹理线 + 边缘生长色
        /// </summary>
        private float ApplyPlantDetail(int x, int y, int seed, float brightness)
        {
            // 纵向纹理线（树干纹理）或网状纹理
            int lineX = 4 + (seed % 3);
            int lineX2 = 9 + (seed % 4);
            if (x == lineX || x == lineX2)
            {
                brightness *= 0.8f;
            }

            // 网状交叉纹理（叶脉），水平线
            if (y % 5 == (seed % 5) && x > 3 && x < 12)
            {
                brightness *= 0.85f;
            }

            // 边缘微微不同色（生长边缘）- 内圈第2-3像素稍亮
            if (x == 2 || x == 13 || y == 2 || y == 13)
            {
                brightness *= 1.08f;
            }

            return brightness;
        }

        /// <summary>
        /// 混凝土/建筑类细节：裂纹 + 粗糙噪声
        /// </summary>
        private float ApplyConcreteDetail(int x, int y, int seed, float brightness)
        {
            // 细裂纹：1-2条从边缘延伸2-4像素的暗线
            int crackStartY = 3 + (seed % 8);
            int crackLen = 2 + (seed % 3);
            if (x >= 2 && x < 2 + crackLen && y == crackStartY)
            {
                brightness *= 0.6f;
            }

            int crackStartX = 5 + ((seed / 7) % 7);
            int crackLenV = 2 + ((seed / 3) % 3);
            if (y >= 2 && y < 2 + crackLenV && x == crackStartX)
            {
                brightness *= 0.6f;
            }

            // 表面粗糙感：更强的高频噪声
            float roughNoise = Mathf.PerlinNoise(
                x * 0.5f + seed * 0.1f,
                y * 0.5f + seed * 0.13f);
            brightness *= 0.88f + roughNoise * 0.24f;

            return brightness;
        }

        #endregion

        private Vector2Int GetChunkAt(Vector3 worldPos)
        {
            // Unity世界坐标 -> Tile坐标 -> Chunk坐标
            // Unity中 y是负的（向下），tile坐标y是正的（向下）
            int tileX = Mathf.FloorToInt(worldPos.x);
            int tileY = Mathf.FloorToInt(-worldPos.y); // 翻转Y
            return new Vector2Int(tileX / chunkSize, tileY / chunkSize);
        }

        private void UpdateVisibleChunks(Vector2Int centerChunk)
        {
            HashSet<Vector2Int> needed = new HashSet<Vector2Int>();
            for (int dx = -renderDistance; dx <= renderDistance; dx++)
            {
                for (int dy = -renderDistance; dy <= renderDistance; dy++)
                {
                    needed.Add(new Vector2Int(centerChunk.x + dx, centerChunk.y + dy));
                }
            }

            // 卸载不需要的
            List<Vector2Int> toUnload = new List<Vector2Int>();
            foreach (var chunk in _loadedChunks)
            {
                if (!needed.Contains(chunk))
                    toUnload.Add(chunk);
            }
            foreach (var chunk in toUnload)
            {
                UnloadChunk(chunk);
                _loadedChunks.Remove(chunk);
            }

            // 加载新的
            foreach (var chunk in needed)
            {
                if (!_loadedChunks.Contains(chunk))
                {
                    LoadChunk(chunk);
                    _loadedChunks.Add(chunk);
                }
            }
        }

        private void LoadChunk(Vector2Int chunk)
        {
            int startX = chunk.x * chunkSize;
            int startY = chunk.y * chunkSize;

            int worldW, worldH;
            if (_inDungeon)
            {
                worldW = _dungeonWidth;
                worldH = _dungeonHeight;
            }
            else
            {
                var gm = GameManager.Instance;
                worldW = gm.worldWidth;
                worldH = gm.worldHeight;
            }

            for (int x = startX; x < startX + chunkSize; x++)
            {
                for (int y = startY; y < startY + chunkSize; y++)
                {
                    if (x < 0 || x >= worldW || y < 0 || y >= worldH) continue;
                    TileType type = GetTileAt(x, y);
                    Vector3Int tilePos = new Vector3Int(x, -y, 0);
                    if (type != TileType.Air && _tileCache.ContainsKey(type))
                    {
                        groundTilemap.SetTile(tilePos, GetTileVariant(type, x, y));
                        // 应用迷雾颜色（副本内不应用迷雾）
                        if (!_inDungeon)
                            ApplyFogColor(tilePos, x, y);
                    }
                }
            }
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            int startX = chunk.x * chunkSize;
            int startY = chunk.y * chunkSize;

            for (int x = startX; x < startX + chunkSize; x++)
            {
                for (int y = startY; y < startY + chunkSize; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(x, -y, 0), null);
                }
            }
        }

        /// <summary>
        /// 更新单个方块的显示
        /// </summary>
        public void RefreshTile(int x, int y)
        {
            TileType type = GetTileAt(x, y);
            Vector3Int pos = new Vector3Int(x, -y, 0);

            if (type == TileType.Air)
            {
                groundTilemap.SetTile(pos, null);
            }
            else if (_tileCache.ContainsKey(type))
            {
                groundTilemap.SetTile(pos, GetTileVariant(type, x, y));
                if (!_inDungeon)
                    ApplyFogColor(pos, x, y);
            }
        }

        /// <summary>
        /// 设置方块类型并刷新显示（同时更新数据和渲染）
        /// </summary>
        public void SetTile(int tileX, int tileY, TileType type)
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetTile(tileX, tileY, type);
            }
            RefreshTile(tileX, tileY);

            // 更新光照
            var lighting = FindObjectOfType<LightingSystem>();
            if (lighting != null)
                lighting.UpdateLocalLighting(tileX, tileY);
        }

        /// <summary>
        /// 应用迷雾颜色到指定 Tile
        /// </summary>
        private void ApplyFogColor(Vector3Int tilePos, int tileX, int tileY)
        {
            if (_fogSystem == null) _fogSystem = FogOfWarSystem.Instance;
            if (_fogSystem == null || !_fogSystem.IsInitialized) return;

            Color fogColor = _fogSystem.GetFogColor(tileX, tileY);
            groundTilemap.SetTileFlags(tilePos, UnityEngine.Tilemaps.TileFlags.None);
            groundTilemap.SetColor(tilePos, fogColor);
        }

        /// <summary>
        /// 刷新所有已加载 chunk 的迷雾颜色（由 FogOfWarSystem 调用）
        /// </summary>
        public void RefreshFogColors()
        {
            // 副本内不处理迷雾
            if (_inDungeon) return;

            if (_fogSystem == null) _fogSystem = FogOfWarSystem.Instance;
            if (_fogSystem == null || !_fogSystem.IsInitialized) return;

            var gm = GameManager.Instance;
            foreach (var chunk in _loadedChunks)
            {
                int startX = chunk.x * chunkSize;
                int startY = chunk.y * chunkSize;

                for (int x = startX; x < startX + chunkSize; x++)
                {
                    for (int y = startY; y < startY + chunkSize; y++)
                    {
                        if (x < 0 || x >= gm.worldWidth || y < 0 || y >= gm.worldHeight) continue;
                        if (gm.WorldTiles[x, y] == TileType.Air) continue;

                        Vector3Int tilePos = new Vector3Int(x, -y, 0);
                        Color fogColor = _fogSystem.GetFogColor(x, y);
                        groundTilemap.SetTileFlags(tilePos, UnityEngine.Tilemaps.TileFlags.None);
                        groundTilemap.SetColor(tilePos, fogColor);
                    }
                }
            }
        }

        /// <summary>
        /// 强制重新加载所有可见chunk
        /// </summary>
        public void ForceRefresh()
        {
            _loadedChunks.Clear();
            groundTilemap.ClearAllTiles();
            Vector3 pos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : Vector3.zero;
            _lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
            UpdateVisibleChunks(GetChunkAt(pos));
        }

        #region Dungeon Data Source Switching

        /// <summary>
        /// 切换到副本数据源
        /// </summary>
        public void SwitchToDungeon(TileType[,] dungeonTiles, int width, int height)
        {
            _dungeonTiles = dungeonTiles;
            _dungeonWidth = width;
            _dungeonHeight = height;
            _inDungeon = true;

            // 卸载当前所有chunk
            UnloadAllChunks();

            // 强制刷新
            ForceRefresh();
        }

        /// <summary>
        /// 切换回主世界数据源
        /// </summary>
        public void SwitchToMainWorld()
        {
            _inDungeon = false;
            _dungeonTiles = null;

            // 卸载副本chunk
            UnloadAllChunks();

            // 强制刷新主世界
            ForceRefresh();
        }

        /// <summary>
        /// 获取指定坐标的方块类型（根据当前数据源）
        /// </summary>
        private TileType GetTileAt(int x, int y)
        {
            if (_inDungeon)
            {
                if (x >= 0 && x < _dungeonWidth && y >= 0 && y < _dungeonHeight)
                    return _dungeonTiles[x, y];
                return TileType.ReinforcedConcrete; // 副本边界外为墙
            }
            else
            {
                var gm = GameManager.Instance;
                if (gm != null && x >= 0 && x < gm.worldWidth && y >= 0 && y < gm.worldHeight)
                    return gm.WorldTiles[x, y];
                return TileType.Air;
            }
        }

        private void UnloadAllChunks()
        {
            foreach (var chunk in _loadedChunks)
            {
                UnloadChunk(chunk);
            }
            _loadedChunks.Clear();
        }

        public bool InDungeon => _inDungeon;

        #endregion
    }
}
