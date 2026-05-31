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

        private int _viewRadius = 3;           // 默认视野半径
        private const int LAMP_RADIUS = 5;     // 能量灯视野半径
        private const int LIGHT_EMIT_RADIUS = 2; // 发光方块探索半径
        private const int NEON_TORCH_ID = 505; // 霓虹火把物品ID

        // 性能优化：缓存上一帧玩家 Tile 坐标
        private int _lastPlayerTileX = int.MinValue;
        private int _lastPlayerTileY = int.MinValue;
        private bool _lastHasLamp = false;
        private bool _dirty = true; // 是否需要通知 ChunkManager 刷新

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
            Debug.Log($"[FogOfWar] 初始化完成: {width}x{height}");
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

            // 只在玩家 Tile 坐标变化或灯状态变化时更新
            if (playerTileX == _lastPlayerTileX &&
                playerTileY == _lastPlayerTileY &&
                hasLamp == _lastHasLamp)
            {
                return;
            }

            _lastPlayerTileX = playerTileX;
            _lastPlayerTileY = playerTileY;
            _lastHasLamp = hasLamp;

            UpdateVisibility(playerTileX, playerTileY, hasLamp);
        }

        /// <summary>
        /// 更新可见区域
        /// </summary>
        public void UpdateVisibility(int playerTileX, int playerTileY, bool hasLamp)
        {
            // 清除上一帧可见状态
            System.Array.Clear(_visible, 0, _visible.Length);

            int radius = hasLamp ? LAMP_RADIUS : _viewRadius;

            // 标记玩家周围为可见+已探索
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

            _dirty = true;

            // 通知 ChunkManager 刷新当前可见区域的颜色
            var chunkMgr = FindObjectOfType<ChunkManager>();
            if (chunkMgr != null)
            {
                chunkMgr.RefreshFogColors();
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

            // 地表及以上始终可见
            if (tileY <= _surfaceHeights[tileX])
                return FogState.Visible;

            // 当前视野内
            if (_visible[tileX, tileY])
                return FogState.Visible;

            // 已探索
            if (_explored[tileX, tileY])
                return FogState.Explored;

            // 未探索
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
