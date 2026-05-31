using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 不稳定裂缝交互系统 - 管理副本入口交互、进入/退出副本逻辑
    /// </summary>
    public class DungeonRiftSystem : MonoBehaviour
    {
        public static DungeonRiftSystem Instance { get; private set; }

        private float _interactRange = 2.5f;
        private bool _showingPrompt = false;
        private Vector2Int _nearestRiftTile;

        // 保存主世界玩家位置（进入副本时记录）
        private Vector3 _savedPlayerPosition;
        private bool _isInDungeon = false;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (_isInDungeon)
            {
                // 副本内检测退出传送门
                CheckDungeonExitRift();
                return;
            }

            CheckNearbyRift();

            if (_showingPrompt && Input.GetKeyDown(KeyCode.E))
            {
                EnterDungeon(_nearestRiftTile);
            }
        }

        /// <summary>
        /// 副本内检测退出传送门（Boss击败后生成的UnstableRift）
        /// </summary>
        private void CheckDungeonExitRift()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 playerPos = player.transform.position;
            int playerTileX = Mathf.FloorToInt(playerPos.x);
            int playerTileY = Mathf.FloorToInt(-playerPos.y);

            // 在副本中扫描附近的裂缝方块
            var chunkMgr = FindObjectOfType<ChunkManager>();
            if (chunkMgr == null || !chunkMgr.InDungeon) return;

            _showingPrompt = false;
            int scanRange = 3;
            float closestDist = float.MaxValue;

            // 通过DungeonManager获取副本方块数据
            if (DungeonManager.Instance == null || !DungeonManager.Instance.IsActive) return;

            for (int dx = -scanRange; dx <= scanRange; dx++)
            {
                for (int dy = -scanRange; dy <= scanRange; dy++)
                {
                    int tx = playerTileX + dx;
                    int ty = playerTileY + dy;

                    // 通过DungeonManager查询副本方块类型
                    if (DungeonManager.Instance.GetDungeonTile(tx, ty) == TileType.UnstableRift)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < _interactRange && dist < closestDist)
                        {
                            _nearestRiftTile = new Vector2Int(tx, ty);
                            _showingPrompt = true;
                            closestDist = dist;
                        }
                    }
                }
            }

            if (_showingPrompt && Input.GetKeyDown(KeyCode.E))
            {
                if (DungeonManager.Instance != null)
                {
                    DungeonManager.Instance.GiveRewards();
                    DungeonManager.Instance.ExitDungeon();
                }
            }
        }

        private void CheckNearbyRift()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 playerPos = player.transform.position;
            int playerTileX = Mathf.FloorToInt(playerPos.x);
            int playerTileY = Mathf.FloorToInt(-playerPos.y);

            var gm = GameManager.Instance;
            _showingPrompt = false;

            // 扫描玩家周围区域寻找裂缝方块
            int scanRange = 3;
            float closestDist = float.MaxValue;

            for (int dx = -scanRange; dx <= scanRange; dx++)
            {
                for (int dy = -scanRange; dy <= scanRange; dy++)
                {
                    int tx = playerTileX + dx;
                    int ty = playerTileY + dy;

                    if (tx >= 0 && tx < gm.worldWidth && ty >= 0 && ty < gm.worldHeight)
                    {
                        if (gm.WorldTiles[tx, ty] == TileType.UnstableRift)
                        {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist < _interactRange && dist < closestDist)
                            {
                                closestDist = dist;
                                _nearestRiftTile = new Vector2Int(tx, ty);
                                _showingPrompt = true;
                            }
                        }
                    }
                }
            }
        }

        public void EnterDungeon(Vector2Int riftPos)
        {
            // 保存玩家位置
            _savedPlayerPosition = PlayerController.Instance.transform.position;
            _isInDungeon = true;
            _showingPrompt = false;

            // 调用DungeonManager生成并进入副本
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.GenerateAndEnter(riftPos);
            }

            Debug.Log($"[DungeonRift] 进入副本！裂缝位置: {riftPos}");
        }

        public void ExitDungeon()
        {
            _isInDungeon = false;

            // 恢复玩家位置
            PlayerController.Instance.transform.position = _savedPlayerPosition;

            // ChunkManager will refresh automatically
            Debug.Log("[DungeonRift] 离开副本，返回主世界");
        }

        public bool IsInDungeon => _isInDungeon;
        public bool IsShowingPrompt => _showingPrompt;

        /// <summary>
        /// 供AI伙伴量子雷达调用：获取最近裂缝的方向
        /// </summary>
        public Vector2Int? FindNearestRift(int fromTileX, int fromTileY, int searchRadius)
        {
            var gm = GameManager.Instance;
            float closestDist = float.MaxValue;
            Vector2Int? result = null;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int tx = fromTileX + dx;
                    int ty = fromTileY + dy;

                    if (tx >= 0 && tx < gm.worldWidth && ty >= 0 && ty < gm.worldHeight)
                    {
                        if (gm.WorldTiles[tx, ty] == TileType.UnstableRift)
                        {
                            float dist = dx * dx + dy * dy;
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                result = new Vector2Int(tx, ty);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// OnGUI显示交互提示
        /// </summary>
        void OnGUI()
        {
            if (_showingPrompt)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 22;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = new Color(0.6f, 0.2f, 1f);
                style.fontStyle = FontStyle.Bold;

                float w = 300f;
                float h = 40f;
                Rect rect = new Rect((Screen.width - w) / 2f, Screen.height * 0.7f, w, h);

                string promptText = _isInDungeon ? "按 E 离开副本" : "按 E 进入不稳定裂缝";
                GUI.Label(rect, promptText, style);
            }
        }
    }
}
