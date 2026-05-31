using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 游戏管理器 - 全局单例，管理游戏状态
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("世界设置")]
        public WorldSize worldSize = WorldSize.Medium;
        public int worldWidth = 6400;
        public int worldHeight = 1800;
        public int seed = -1; // -1 = 随机

        [Header("进度")]
        public bool hardmodeActivated = false; // 击败肉墙等价Boss后开启
        public int bossesDefeated = 0;

        [Header("游戏状态")]
        public bool isPaused;
        public bool isInventoryOpen;
        public float gameTime; // 0-24000 (一天)
        public int dayCount;

        [Header("难度")]
        public GameDifficulty difficulty = GameDifficulty.Normal;

        // 世界数据
        public TileType[,] WorldTiles { get; private set; }
        public byte[,] LightMap { get; private set; }
        public byte[,] WallMap { get; private set; }

        // 事件
        public System.Action OnWorldGenerated;
        public System.Action OnDayNightChanged;
        public System.Action<bool> OnPauseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeWorld();
        }

        private void InitializeWorld()
        {
            // 根据WorldSize设置尺寸（对标泰拉瑞亚）
            switch (worldSize)
            {
                case WorldSize.Small:
                    worldWidth = 4200; worldHeight = 1200; break;
                case WorldSize.Medium:
                    worldWidth = 6400; worldHeight = 1800; break;
                case WorldSize.Large:
                    worldWidth = 8400; worldHeight = 2400; break;
            }

            WorldTiles = new TileType[worldWidth, worldHeight];
            LightMap = new byte[worldWidth, worldHeight];
            WallMap = new byte[worldWidth, worldHeight];

            if (seed == -1)
                seed = Random.Range(0, 999999);

            Debug.Log($"[世界] 初始化 {worldSize} 世界: {worldWidth}x{worldHeight} = {(long)worldWidth * worldHeight} 方块, Seed: {seed}");
        }

        private void Update()
        {
            if (!isPaused)
            {
                // 时间流逝 (24000 ticks = 1天 = 10分钟现实时间)
                gameTime += Time.deltaTime * 40f;
                if (gameTime >= 24000f)
                {
                    gameTime -= 24000f;
                    dayCount++;
                    OnDayNightChanged?.Invoke();
                }
            }

            // 暂停切换
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            OnPauseChanged?.Invoke(isPaused);
        }

        public bool IsDay => gameTime >= 4500f && gameTime < 19500f;
        public bool IsNight => !IsDay;
        public float DayProgress => gameTime / 24000f;

        /// <summary>
        /// 设置方块（带边界检查）
        /// </summary>
        public void SetTile(int x, int y, TileType type)
        {
            if (x >= 0 && x < worldWidth && y >= 0 && y < worldHeight)
                WorldTiles[x, y] = type;
        }

        /// <summary>
        /// 获取方块（带边界检查）
        /// </summary>
        public TileType GetTile(int x, int y)
        {
            if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight)
                return TileType.ReinforcedSteel;
            return WorldTiles[x, y];
        }

        /// <summary>
        /// 获取光照等级
        /// </summary>
        public int GetLight(int x, int y)
        {
            if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight)
                return 15;
            return LightMap[x, y];
        }
    }

    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    public enum WorldSize
    {
        Small,  // 4200 x 1200
        Medium, // 6400 x 1800
        Large   // 8400 x 2400
    }
}
