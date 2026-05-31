using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 迷雾战争状态枚举
    /// </summary>
    public enum FogState
    {
        Hidden,     // 未探索：完全黑色
        Explored,   // 已探索但不在视野：30%亮度
        Visible     // 当前可见：正常显示
    }

    /// <summary>
    /// 地下迷雾战争系统 - 管理探索可见性
    /// 地表以上始终可见；地下区域需要探索
    /// </summary>
    public class FogOfWarSystem : MonoBehaviour
    {
        public static FogOfWarSystem Instance { get; private set; }

        // 已探索区域记录（持久化）
        private bool[,] _explored;

        // 当前可见区域（每帧重新计算）
        private bool[,] _visible;

        // 地表高度引用（地表以上始终可见）
        private int[] _surfaceHeights;

        private int _worldWidth;
        private int _worldHeight;

        private const int UNDERGROUND_RADIUS = 1;     // 地下视野半径（3x3）
        private const int UNDERGROUND_LAMP_RADIUS = 2; // 地下持灯视野半径（5x5）
        private const int SURFACE_DEPTH = 3;           // 地表模式可见地下深度
        private const int LIGHT_EMIT_RADIUS = 2;       // 发光方块探索半径
        private const int NEON_TORCH_ID = 505;         // 霓虹火把物品ID

        // 性能优化：缓存上一帧玩家 Tile 坐标
        private int _lastPlayerTileX = int.MinValue;
        private int _lastPlayerTileY = int.MinValue;
        private bool _lastHasLamp = false;
        private bool _lastOnSurface = true;
        private int _lastCamTileX = int.MinValue; // 用于地表模式的摄像机位置缓存
#pragma warning disable 0414
        private bool _dirty = true; // 是否需要通知 ChunkManager 刷新（预留，后续用于增量刷新优化）
#pragma warning restore 0414

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// 初始化迷雾系统，传入世界尺寸和地表高度数组
        /// </summary>
        public void Initialize(int width, int height, int[] surfaceHeights)
        {
            _worldWidth = width;
            _worldHeight = height;
            _surfaceHeights = surfaceHeights;

            _explored = new bool[width, height];
            _visible = new bool[width, height];

            // 地表以上全部标记为已探索（y < surfaceHeight[x] 为天空）
            for (int x = 0; x < width; x++)
            {
                int surfY = _surfaceHeights[x];
                for (int y = 0; y < height; y++)
                {
                    if (y <= surfY)
                    {
                        _explored[x, y] = true;
                    }
                }
            }

            // 标记发光方块周围为已探索
            MarkLightEmittingTiles();

            _dirty = true;

            // 立即揭示玩家当前位置周围（避免首帧全黑）
            RevealAroundPlayer();

            Debug.Log($"[FogOfWar] 初始化完成: {width}x{height}");
        }

        /// <summary>
        /// 立即揭示玩家当前位置周围（在初始化时调用，确保首帧不会全黑）
        /// </summary>
        private void RevealAroundPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 pos = player.transform.position;
            int playerTileX = Mathf.FloorToInt(pos.x);
            int playerTileY = Mathf.FloorToInt(-pos.y);

            _lastPlayerTileX = playerTileX;
            _lastPlayerTileY = playerTileY;

            // 使用完整的可见性计算逻辑
            UpdateVisibility(playerTileX, playerTileY, false);
        }

        /// <summary>
        /// 扫描世界中的发光方块，将其周围标记为已探索
        /// </summary>
        private void MarkLightEmittingTiles()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.WorldTiles == null) return;

            for (int x = 0; x < _worldWidth; x++)
            {
                // 只扫描地下部分（地表以上已全部explored）
                int startY = _surfaceHeights[x] + 1;
                for (int y = startY; y < _worldHeight; y++)
                {
                    TileType type = gm.WorldTiles[x, y];
                    if (type == TileType.Air) continue;

                    var props = TileRegistry.Get(type);
                    if (props != null && props.lightEmission > 0)
                    {
                        MarkExploredRadius(x, y, LIGHT_EMIT_RADIUS);
                    }
                }
            }
        }

        /// <summary>
        /// 将指定坐标周围一定半径标记为已探索
        /// </summary>
        private void MarkExploredRadius(int cx, int cy, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int tx = cx + dx;
                    int ty = cy + dy;
                    if (tx >= 0 && tx < _worldWidth && ty >= 0 && ty < _worldHeight)
                    {
                        _explored[tx, ty] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 当放置发光方块时调用，标记其周围为已探索
        /// </summary>
        public void OnLightBlockPlaced(int tileX, int tileY)
        {
            MarkExploredRadius(tileX, tileY, LIGHT_EMIT_RADIUS);
            _dirty = true;
        }

        private void LateUpdate()
        {
            if (_explored == null) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            // 获取玩家 Tile 坐标（Unity Y 翻转）
            Vector3 pos = player.transform.position;
            int playerTileX = Mathf.FloorToInt(pos.x);
            int playerTileY = Mathf.FloorToInt(-pos.y);

            // 检查是否持有霓虹火把
            bool hasLamp = false;
            if (Inventory.Instance != null)
            {
                var slot = Inventory.Instance.Slots[Inventory.Instance.SelectedSlot];
                if (!slot.isEmpty && slot.itemId == NEON_TORCH_ID)
                {
                    hasLamp = true;
                }
            }

            // 地表模式下摄像机位置也影响可见范围
            int camTileX = int.MinValue;
            bool onSurface = IsPlayerOnSurface(playerTileX, playerTileY);
            if (onSurface)
            {
                Camera cam = Camera.main;
                if (cam != null) camTileX = Mathf.FloorToInt(cam.transform.position.x);
            }

            // 只在玩家 Tile 坐标变化、灯状态变化、或地表模式下摄像机位置变化时更新
            if (playerTileX == _lastPlayerTileX &&
                playerTileY == _lastPlayerTileY &&
                hasLamp == _lastHasLamp &&
                onSurface == _lastOnSurface &&
                (!onSurface || camTileX == _lastCamTileX))
            {
                return;
            }

            _lastPlayerTileX = playerTileX;
            _lastPlayerTileY = playerTileY;
            _lastHasLamp = hasLamp;
            _lastOnSurface = onSurface;
            _lastCamTileX = camTileX;

            UpdateVisibility(playerTileX, playerTileY, hasLamp);
        }

        /// <summary>
        /// 判断玩家是否在地表（玩家tileY <= 该位置地表高度）
        /// </summary>
        private bool IsPlayerOnSurface(int playerTileX, int playerTileY)
        {
            if (playerTileX < 0 || playerTileX >= _worldWidth) return true;
            return playerTileY <= _surfaceHeights[playerTileX];
        }

        /// <summary>
        /// 更新可见区域 - 根据玩家在地表/地下切换不同的可见规则
        /// </summary>
        public void UpdateVisibility(int playerTileX, int playerTileY, bool hasLamp)
        {
            // 清除上一帧可见状态
            System.Array.Clear(_visible, 0, _visible.Length);

            if (IsPlayerOnSurface(playerTileX, playerTileY))
            {
                // === 地表模式 ===
                // 可以看到屏幕范围内所有地表方块 + 地表以下3格
                UpdateSurfaceVisibility();
            }
            else
            {
                // === 地下模式 ===
                // 只能看到周围 3x3（无灯）或 5x5（有灯）
                int radius = hasLamp ? UNDERGROUND_LAMP_RADIUS : UNDERGROUND_RADIUS;
                UpdateUndergroundVisibility(playerTileX, playerTileY, radius);
            }

            _dirty = true;

            // 通知 ChunkManager 刷新当前可见区域的颜色
            var chunkMgr = FindObjectOfType<ChunkManager>();
            if (chunkMgr != null)
            {
                chunkMgr.RefreshFogColors();
            }
        }

        /// <summary>
        /// 地表模式可见性：屏幕范围内地表+地下3格全部可见
        /// </summary>
        private void UpdateSurfaceVisibility()
        {
            // 获取摄像机可见范围（tile坐标）
            Camera cam = Camera.main;
            if (cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 camPos = cam.transform.position;

            // 摄像机可见区域转换为tile坐标范围
            int minTileX = Mathf.FloorToInt(camPos.x - halfW) - 1;
            int maxTileX = Mathf.CeilToInt(camPos.x + halfW) + 1;
            int minTileY = Mathf.FloorToInt(-camPos.y - halfH) - 1; // Unity Y翻转
            int maxTileY = Mathf.CeilToInt(-camPos.y + halfH) + 1;

            // 限制在世界范围内
            minTileX = Mathf.Max(0, minTileX);
            maxTileX = Mathf.Min(_worldWidth - 1, maxTileX);
            minTileY = Mathf.Max(0, minTileY);
            maxTileY = Mathf.Min(_worldHeight - 1, maxTileY);

            for (int x = minTileX; x <= maxTileX; x++)
            {
                int surfY = (x >= 0 && x < _worldWidth) ? _surfaceHeights[x] : 0;
                int maxVisibleDepth = surfY + SURFACE_DEPTH; // 地表以下3格

                for (int y = minTileY; y <= maxTileY; y++)
                {
                    if (y <= maxVisibleDepth)
                    {
                        _visible[x, y] = true;
                        _explored[x, y] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 地下模式可见性：仅玩家周围小范围
        /// </summary>
        private void UpdateUndergroundVisibility(int playerTileX, int playerTileY, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int tx = playerTileX + dx;
                    int ty = playerTileY + dy;
                    if (tx >= 0 && tx < _worldWidth && ty >= 0 && ty < _worldHeight)
                    {
                        _visible[tx, ty] = true;
                        _explored[tx, ty] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 查询某格子的迷雾状态
        /// </summary>
        public FogState GetFogState(int tileX, int tileY)
        {
            // 边界检查
            if (tileX < 0 || tileX >= _worldWidth || tileY < 0 || tileY >= _worldHeight)
                return FogState.Visible;

            // 当前视野内（由 UpdateVisibility 计算）
            if (_visible[tileX, tileY])
                return FogState.Visible;

            // 已探索（暗淡显示）
            if (_explored[tileX, tileY])
                return FogState.Explored;

            // 未探索（完全黑色）
            return FogState.Hidden;
        }

        /// <summary>
        /// 手动标记某个位置为已探索（玩家挖掘时调用）
        /// </summary>
        public void MarkExplored(int tileX, int tileY)
        {
            if (tileX >= 0 && tileX < _worldWidth && tileY >= 0 && tileY < _worldHeight)
            {
                _explored[tileX, tileY] = true;
            }
        }

        /// <summary>
        /// 获取 Tile 颜色（供 ChunkManager 使用）
        /// </summary>
        public Color GetFogColor(int tileX, int tileY)
        {
            FogState state = GetFogState(tileX, tileY);
            switch (state)
            {
                case FogState.Hidden:
                    return Color.black;
                case FogState.Explored:
                    return new Color(0.3f, 0.3f, 0.3f, 1f);
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _explored != null;
    }
}
