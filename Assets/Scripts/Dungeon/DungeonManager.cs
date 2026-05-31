using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        private DungeonGenerator _generator;
        private bool _isActive = false;
        private int _currentDifficulty = 1;

        // 副本地图数据
        private TileType[,] _dungeonTiles;
        private List<DungeonRoom> _dungeonRooms;

        // 副本内敌人管理
        private List<GameObject> _dungeonEnemies = new List<GameObject>();
        private DungeonRoom _currentBossRoom;

        // 副本完成状态
        private bool _bossDefeated = false;

        public bool IsActive => _isActive;

        /// <summary>
        /// 获取副本中指定坐标的方块类型
        /// </summary>
        public TileType GetDungeonTile(int x, int y)
        {
            if (_dungeonTiles == null) return TileType.Air;
            if (x >= 0 && x < DungeonGenerator.DUNGEON_WIDTH && y >= 0 && y < DungeonGenerator.DUNGEON_HEIGHT)
                return _dungeonTiles[x, y];
            return TileType.ReinforcedConcrete;
        }

        void Awake()
        {
            Instance = this;
            _generator = new DungeonGenerator();
        }

        void Update()
        {
            if (!_isActive) return;

            // 检查当前房间敌人是否清理完毕
            CheckRoomCleared();

            // 检查Boss击杀
            if (_bossDefeated)
            {
                // Boss击杀后生成退出传送门
                SpawnExitPortal();
                _bossDefeated = false;
            }
        }

        /// <summary>
        /// 生成副本并传送玩家进入
        /// </summary>
        public void GenerateAndEnter(Vector2Int riftPos)
        {
            // 生成地牢
            _generator.Generate(_currentDifficulty);
            _dungeonTiles = _generator.Tiles;
            _dungeonRooms = _generator.Rooms;
            _isActive = true;
            _bossDefeated = false;
            _currentBossRoom = null;

            // 切换ChunkManager数据源到副本
            var chunkMgr = FindObjectOfType<ChunkManager>();
            if (chunkMgr != null)
            {
                chunkMgr.SwitchToDungeon(_dungeonTiles,
                    DungeonGenerator.DUNGEON_WIDTH, DungeonGenerator.DUNGEON_HEIGHT);
            }

            // 传送玩家到入口房间
            if (_dungeonRooms.Count > 0)
            {
                var entrance = _dungeonRooms[0];
                float spawnX = entrance.X + entrance.Width / 2;
                float spawnY = -(entrance.Y + entrance.Height / 2); // Tile坐标转Unity坐标
                PlayerController.Instance.transform.position = new Vector3(spawnX, spawnY, 0);
            }

            // 生成副本内敌人
            SpawnDungeonEnemies();

            // 增加难度（每次进入副本+1）
            _currentDifficulty++;

            Debug.Log($"[Dungeon] 进入地牢！难度: {_currentDifficulty}, 房间数: {_dungeonRooms.Count}");
        }

        /// <summary>
        /// 退出副本，回到主世界
        /// </summary>
        public void ExitDungeon()
        {
            _isActive = false;

            // 清理副本敌人
            foreach (var enemy in _dungeonEnemies)
            {
                if (enemy != null) Destroy(enemy);
            }
            _dungeonEnemies.Clear();

            // 切换回主世界
            var chunkMgr = FindObjectOfType<ChunkManager>();
            if (chunkMgr != null)
            {
                chunkMgr.SwitchToMainWorld();
            }

            // 通知DungeonRiftSystem恢复玩家位置
            if (DungeonRiftSystem.Instance != null)
            {
                DungeonRiftSystem.Instance.ExitDungeon();
            }

            Debug.Log("[Dungeon] 离开地牢，返回主世界");
        }

        private void SpawnDungeonEnemies()
        {
            foreach (var room in _dungeonRooms)
            {
                if (room.Type == RoomType.Entrance) continue;

                for (int i = 0; i < room.EnemyCount; i++)
                {
                    float ex = room.X + Random.Range(3, room.Width - 3);
                    float ey = -(room.Y + room.Height - 4); // 在地面上生成
                    Vector2 spawnPos = new Vector2(ex, ey);

                    // 使用EnemyFactory生成敌人（地牢敌人更强）
                    string enemyType;
                    if (room.Type == RoomType.Boss)
                        enemyType = "CoreGuardian";
                    else
                    {
                        // 地牢特有敌人：更强版本
                        string[] dungeonEnemies = { "SecurityBot", "HoverDrone", "SpiderBot", "EMPDrone", "TechPriest" };
                        enemyType = dungeonEnemies[Random.Range(0, dungeonEnemies.Length)];
                    }

                    GameObject enemy = EnemyFactory.SpawnEnemy(enemyType, spawnPos);
                    if (enemy != null)
                    {
                        // 地牢加成：HP和伤害+50%
                        var eb = enemy.GetComponent<EnemyBase>();
                        if (eb != null)
                        {
                            eb.maxHealth = Mathf.RoundToInt(eb.maxHealth * 1.5f);
                            eb.currentHealth = eb.maxHealth;
                            eb.damage = Mathf.RoundToInt(eb.damage * 1.5f);
                        }
                        _dungeonEnemies.Add(enemy);
                    }
                }
            }
        }

        private void CheckRoomCleared()
        {
            // 清理已销毁的敌人引用
            _dungeonEnemies.RemoveAll(e => e == null);

            // 检查Boss房间
            if (_currentBossRoom != null && !_currentBossRoom.IsCleared)
            {
                bool bossAlive = false;
                foreach (var enemy in _dungeonEnemies)
                {
                    if (enemy != null && enemy.name.Contains("CoreGuardian"))
                    {
                        bossAlive = true;
                        break;
                    }
                }

                if (!bossAlive)
                {
                    _currentBossRoom.IsCleared = true;
                    _bossDefeated = true;
                }
            }
            else if (_currentBossRoom == null)
            {
                // 找到Boss房间
                foreach (var room in _dungeonRooms)
                {
                    if (room.Type == RoomType.Boss)
                    {
                        _currentBossRoom = room;
                        break;
                    }
                }
            }
        }

        private void SpawnExitPortal()
        {
            if (_currentBossRoom == null) return;

            // 在Boss房间中心放置一个传送门（用UnstableRift方块表示）
            int portalX = _currentBossRoom.X + _currentBossRoom.Width / 2;
            int portalY = _currentBossRoom.Y + _currentBossRoom.Height / 2;

            if (_dungeonTiles != null)
            {
                _dungeonTiles[portalX, portalY] = TileType.UnstableRift;

                // 刷新显示
                var chunkMgr = FindObjectOfType<ChunkManager>();
                if (chunkMgr != null)
                {
                    chunkMgr.ForceRefresh();
                }
            }

            Debug.Log("[Dungeon] Boss已击败！传送门已生成。");
        }

        /// <summary>
        /// 玩家死亡时调用
        /// </summary>
        public void OnPlayerDeath()
        {
            if (_isActive)
            {
                ExitDungeon();
            }
        }

        /// <summary>
        /// 获取副本奖励（Boss击败后）
        /// </summary>
        public void GiveRewards()
        {
            var inv = Inventory.Instance;
            if (inv == null) return;

            // Boss掉落奖励
            inv.AddItem(33, Random.Range(3, 8));  // 量子芯片
            inv.AddItem(34, Random.Range(2, 5));  // 赛博合金
            inv.AddItem(83, Random.Range(5, 10)); // 能量晶体

            // 特殊掉落
            if (Random.value < 0.3f)
            {
                inv.AddItem(32, Random.Range(1, 3)); // 等离子电池
            }

            Debug.Log("[Dungeon] 奖励已发放！");
        }
    }
}
