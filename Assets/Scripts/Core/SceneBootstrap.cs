using UnityEngine;
using UnityEngine.Tilemaps;

namespace CyberTerraria
{
    /// <summary>
    /// 场景自动引导器 - 运行时自动创建完整游戏场景
    /// 使用 RuntimeInitializeOnLoadMethod 属性，无需手动挂载到任何GameObject
    /// 打开Unity后创建任意空场景按Play即可自动运行
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Unity启动后自动调用，不需要场景中存在任何物体
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            // 检查是否已经初始化过
            if (GameManager.Instance != null) return;

            GameObject bootstrapObj = new GameObject("[SceneBootstrap]");
            bootstrapObj.AddComponent<SceneBootstrap>();
        }
        private void Awake()
        {
            Debug.Log("[SceneBootstrap] 开始初始化赛博废土世界...");

            // 1. 创建GameManager
            CreateGameManager();

            // 2. 创建世界系统
            CreateWorldSystem();

            // 3. 创建玩家
            CreatePlayer();

            // 4. 创建相机系统
            SetupCamera();

            // 5. 创建UI
            CreateUI();

            // 6. 创建敌人刷怪系统
            CreateEnemySpawner();

            // 7. 创建粒子系统
            CreateEffectsSystem();

            // 8. 生成世界
            GenerateWorld();

            // 9. 创建AI伙伴
            CreateAICompanion();

            Debug.Log("[SceneBootstrap] 世界初始化完成！");
        }

        private void CreateGameManager()
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        private void CreateWorldSystem()
        {
            // 世界根节点
            var worldRoot = new GameObject("WorldSystem");

            // Tilemap Grid
            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(worldRoot.transform);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            // Ground Tilemap
            var groundObj = new GameObject("GroundTilemap");
            groundObj.transform.SetParent(gridObj.transform);
            var groundTilemap = groundObj.AddComponent<Tilemap>();
            var groundRenderer = groundObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingLayerName = "Tiles";
            groundObj.AddComponent<TilemapCollider2D>();
            groundObj.layer = LayerMask.NameToLayer("Ground");

            // 添加CompositeCollider2D用于更好的碰撞性能
            var groundRb = groundObj.AddComponent<Rigidbody2D>();
            groundRb.bodyType = RigidbodyType2D.Static;
            var composite = groundObj.AddComponent<CompositeCollider2D>();
            var tilemapCol = groundObj.GetComponent<TilemapCollider2D>();
            tilemapCol.usedByComposite = true;

            // 世界生成组件
            var worldGen = worldRoot.AddComponent<WorldGenerator>();
            var biome = worldRoot.AddComponent<BiomeSystem>();
            var ruins = worldRoot.AddComponent<RuinGenerator>();
            var lighting = worldRoot.AddComponent<LightingSystem>();
            var dayNight = worldRoot.AddComponent<DayNightCycle>();
            var fogOfWar = worldRoot.AddComponent<FogOfWarSystem>();

            // ChunkManager
            var chunkMgr = worldRoot.AddComponent<ChunkManager>();
            chunkMgr.groundTilemap = groundTilemap;

            // 裂缝副本系统
            worldRoot.AddComponent<DungeonRiftSystem>();

            // 副本管理器
            worldRoot.AddComponent<DungeonManager>();
        }

        private void CreatePlayer()
        {
            var playerObj = new GameObject("Player");
            playerObj.layer = LayerMask.NameToLayer("Player");

            // 组件
            playerObj.AddComponent<PlayerController>().groundLayer = LayerMask.GetMask("Ground");
            playerObj.AddComponent<PlayerStats>();
            playerObj.AddComponent<PlayerMining>();
            playerObj.AddComponent<PlayerCombat>();
            playerObj.AddComponent<Inventory>();
            playerObj.AddComponent<CyberwareSystem>();
            playerObj.AddComponent<BuffSystem>();
            playerObj.AddComponent<CraftingSystem>();

            // 物理
            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = playerObj.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = playerObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.5f);
            col.offset = new Vector2(0, 0.75f);

            // 精灵
            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlayerSprite();
            sr.sortingLayerName = "Player";

            // 初始位置（世界生成后会被重新设置）
            playerObj.transform.position = new Vector3(210, -55, 0);
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 40f;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);

            // 添加跟随脚本
            var follow = cam.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.maxX = GameManager.Instance.worldWidth;
            follow.minY = -GameManager.Instance.worldHeight;
        }

        private void CreateUI()
        {
            var uiObj = new GameObject("UIManager");
            uiObj.AddComponent<HUDManager>();
        }

        private void CreateEnemySpawner()
        {
            var spawnerObj = new GameObject("EnemySpawner");
            spawnerObj.AddComponent<EnemySpawner>();
            spawnerObj.AddComponent<WorldEventSystem>();
            spawnerObj.AddComponent<NPCSystem>();
            spawnerObj.AddComponent<ProgressionSystem>();
        }

        private void CreateEffectsSystem()
        {
            var effectsObj = new GameObject("EffectsSystem");
            effectsObj.AddComponent<ParticleManager>();

            // 创建天空背景
            GameObject skyObj = new GameObject("SkyBackground");
            SpriteRenderer skySr = skyObj.AddComponent<SpriteRenderer>();
            skySr.sortingOrder = -100; // 确保在所有物体后面渲染
            skyObj.AddComponent<SkyBackground>();
            skyObj.transform.position = new Vector3(0, 0, 10f);
        }

        private void GenerateWorld()
        {
            var gm = GameManager.Instance;
            var worldGen = FindObjectOfType<WorldGenerator>();

            if (worldGen != null && gm != null)
            {
                worldGen.Generate(gm.seed);

                // 初始化迷雾系统
                var fogOfWar = FindObjectOfType<FogOfWarSystem>();
                if (fogOfWar != null && worldGen.SurfaceHeights != null)
                {
                    fogOfWar.Initialize(gm.worldWidth, gm.worldHeight, worldGen.SurfaceHeights);
                }

                // 设置玩家出生点
                Vector2 spawn = worldGen.GetSpawnPoint();
                var player = PlayerController.Instance;
                if (player != null)
                {
                    // spawn.y 是tile坐标，Unity世界中y取负
                    player.transform.position = new Vector3(spawn.x, -spawn.y, 0);
                    Debug.Log($"[玩家] 出生点: tile({spawn.x:F0},{spawn.y:F0}) -> Unity({spawn.x:F1},{-spawn.y:F1})");
                }

                // 强制立即刷新Tilemap（在玩家位置周围）
                var chunkMgr = FindObjectOfType<ChunkManager>();
                if (chunkMgr != null)
                {
                    chunkMgr.ForceRefresh();
                }
            }
        }

        private void CreateAICompanion()
        {
            GameObject companionObj = new GameObject("AICompanion_QR7");

            SpriteRenderer sr = companionObj.AddComponent<SpriteRenderer>();
            sr.sprite = AICompanion.GenerateCompanionSprite();
            sr.sortingOrder = 8;

            companionObj.AddComponent<AICompanion>();

            // 初始位置在玩家旁边
            if (PlayerController.Instance != null)
            {
                companionObj.transform.position = PlayerController.Instance.transform.position + new Vector3(-2f, 0.5f, 0);
            }
        }

        /// <summary>
        /// 程序化生成玩家角色精灵 - 16x24帽子少年
        /// </summary>
        private Sprite CreatePlayerSprite()
        {
            Texture2D tex = new Texture2D(16, 24, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // 清除为透明
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, clear);

            Color shoe = new Color(0.45f, 0.55f, 0.75f);
            Color skin = new Color(0.92f, 0.78f, 0.67f);
            Color pants = new Color(0.45f, 0.5f, 0.3f);
            Color jacket = new Color(0.75f, 0.68f, 0.65f);
            Color shirt = new Color(0.85f, 0.3f, 0.25f);
            Color belt = new Color(0.2f, 0.2f, 0.2f);
            Color hat = new Color(0.55f, 0.35f, 0.2f);
            Color hatDark = new Color(0.4f, 0.25f, 0.15f);
            Color eye = new Color(0.15f, 0.15f, 0.15f);

            // 鞋子 y=0-2
            for (int y = 0; y <= 2; y++) {
                for (int x = 4; x <= 6; x++) tex.SetPixel(x, y, shoe);
                for (int x = 9; x <= 11; x++) tex.SetPixel(x, y, shoe);
            }

            // 小腿 y=3-4
            for (int y = 3; y <= 4; y++) {
                for (int x = 5; x <= 6; x++) tex.SetPixel(x, y, skin);
                for (int x = 9; x <= 10; x++) tex.SetPixel(x, y, skin);
            }

            // 短裤 y=5-8
            for (int y = 5; y <= 8; y++)
                for (int x = 4; x <= 11; x++) tex.SetPixel(x, y, pants);

            // 腰带 y=9
            for (int x = 4; x <= 11; x++) tex.SetPixel(x, 9, belt);

            // 上身 y=10-16
            for (int y = 10; y <= 16; y++) {
                for (int x = 3; x <= 12; x++) tex.SetPixel(x, y, jacket);
                for (int x = 6; x <= 9; x++) tex.SetPixel(x, y, shirt); // 红色内衬
            }
            // 手臂
            for (int y = 10; y <= 15; y++) {
                tex.SetPixel(2, y, jacket);
                tex.SetPixel(13, y, jacket);
            }

            // 脖子+脸 y=17-18
            for (int y = 17; y <= 18; y++)
                for (int x = 5; x <= 10; x++) tex.SetPixel(x, y, skin);
            // 眼睛
            tex.SetPixel(6, 18, eye);
            tex.SetPixel(9, 18, eye);

            // 头发/帽檐下 y=19
            for (int x = 4; x <= 11; x++) tex.SetPixel(x, 19, hatDark);

            // 帽子 y=20-23
            for (int y = 20; y <= 23; y++)
                for (int x = 5; x <= 10; x++) tex.SetPixel(x, y, hat);
            // 帽顶收窄
            for (int x = 5; x <= 10; x++) tex.SetPixel(x, 23, hat);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 24), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
