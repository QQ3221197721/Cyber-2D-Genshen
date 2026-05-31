using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// NPC系统 - 赛博废土版的友方NPC
    /// 类似泰拉瑞亚：满足条件后入住，提供商店/服务
    /// </summary>
    public class NPCSystem : MonoBehaviour
    {
        public static NPCSystem Instance { get; private set; }
        public List<NPCData> AllNPCs { get; private set; }
        public List<NPCData> SpawnedNPCs { get; private set; } = new List<NPCData>();

        private float _spawnCheckTimer = 0f;
        private List<GameObject> _spawnedNPCObjects = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
            InitializeNPCs();
        }

        private void Update()
        {
            // 定期检查NPC生成条件
            _spawnCheckTimer += Time.deltaTime;
            if (_spawnCheckTimer >= 60f)
            {
                _spawnCheckTimer = 0f;
                TrySpawnAllNPCs();
            }

            // NPC交互检测
            if (Input.GetKeyDown(KeyCode.E))
            {
                CheckNPCInteraction();
            }
        }

        private void InitializeNPCs()
        {
            AllNPCs = new List<NPCData>
            {
                new NPCData { id = "merchant", name = "废品商人", description = "贩卖基础材料和工具",
                    spawnCondition = NPCCondition.Always,
                    shopItems = new[]{100, 300, 500, 502, 504} },

                new NPCData { id = "arms_dealer", name = "军火贩子", description = "出售各类远程武器和弹药",
                    spawnCondition = NPCCondition.HasGun,
                    shopItems = new[]{250, 251, 255} },

                new NPCData { id = "ripperdoc", name = "义体医生", description = "安装和维护赛博义体",
                    spawnCondition = NPCCondition.HasCyberware,
                    shopItems = new[]{300, 301, 304} },

                new NPCData { id = "netrunner", name = "网络骸客", description = "出售电子元件和量子材料",
                    spawnCondition = NPCCondition.HasCircuitBoard,
                    shopItems = new[]{22, 31, 32, 33} },

                new NPCData { id = "mechanic", name = "机械师", description = "出售管道/线缆，提供装备升级",
                    spawnCondition = NPCCondition.BossDefeated1,
                    shopItems = new[]{20, 21, 15, 103} },

                new NPCData { id = "nurse", name = "医疗AI", description = "治疗伤病，清除Debuff",
                    spawnCondition = NPCCondition.HasHealthItem,
                    shopItems = new[]{300, 301, 302, 304} },

                new NPCData { id = "demolitionist", name = "爆破专家", description = "出售炸弹和重型武器",
                    spawnCondition = NPCCondition.HasExplosive,
                    shopItems = new[]{253} },

                new NPCData { id = "fixer", name = "掎客", description = "发布悬赏任务",
                    spawnCondition = NPCCondition.BossDefeated1,
                    shopItems = new[]{400, 401} },

                new NPCData { id = "scientist", name = "疯狂科学家", description = "研究异变材料",
                    spawnCondition = NPCCondition.HardMode,
                    shopItems = new[]{33, 402, 403} },

                new NPCData { id = "informant", name = "信息贩子", description = "提供世界情报",
                    spawnCondition = NPCCondition.BossDefeated2,
                    shopItems = new[]{303} },
            };
        }

        /// <summary>
        /// 尝试生成所有满足条件的NPC
        /// </summary>
        private void TrySpawnAllNPCs()
        {
            foreach (var npc in AllNPCs)
            {
                if (npc.isSpawned) continue;
                if (CheckSpawnCondition(npc))
                {
                    Vector2 spawnPos = FindNPCSurfacePosition();
                    GameObject npcObj = SpawnNPC(npc, spawnPos);
                    _spawnedNPCObjects.Add(npcObj);
                    SpawnedNPCs.Add(npc);
                    npc.isSpawned = true;
                    Debug.Log($"[NPC] {npc.name} 已生成于 {spawnPos}");
                }
            }
        }

        /// <summary>
        /// 创建NPC的GameObject
        /// </summary>
        private GameObject SpawnNPC(NPCData npc, Vector2 worldPosition)
        {
            GameObject npcObj = new GameObject($"NPC_{npc.name}");
            npcObj.transform.position = worldPosition;

            // 添加精灵渲染器
            SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateNPCSprite(npc);
            sr.sortingLayerName = "Entities";
            sr.sortingOrder = 5;

            // 添加碰撞体（用于交互检测）
            BoxCollider2D col = npcObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.6f);
            col.isTrigger = true;

            // 添加刚体防止穿地
            Rigidbody2D rb = npcObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.freezeRotation = true;

            return npcObj;
        }

        /// <summary>
        /// 程序化生成NPC精灵 (12x18像素人形)
        /// </summary>
        private Sprite GenerateNPCSprite(NPCData npc)
        {
            int w = 12, h = 18;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // 清透明
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // 根据NPC ID生成不同配色
            int hash = npc.id.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            Color skinColor = new Color(0.85f, 0.7f, 0.55f);
            Color clothColor = Color.HSVToRGB(hue, 0.6f, 0.7f);
            Color hairColor = Color.HSVToRGB((hue + 0.5f) % 1f, 0.4f, 0.3f);

            // 画头部 (y=14-17, x=3-8)
            for (int x = 3; x <= 8; x++)
                for (int y = 14; y <= 17; y++)
                    pixels[y * w + x] = skinColor;

            // 画头发 (y=16-17, x=3-8)
            for (int x = 3; x <= 8; x++)
                for (int y = 16; y <= 17; y++)
                    pixels[y * w + x] = hairColor;

            // 画身体/衣服 (y=6-13, x=3-8)
            for (int x = 3; x <= 8; x++)
                for (int y = 6; y <= 13; y++)
                    pixels[y * w + x] = clothColor;

            // 画腿部 (y=0-5, x=4-5 和 x=6-7)
            for (int y = 0; y <= 5; y++)
            {
                pixels[y * w + 4] = clothColor * 0.7f;
                pixels[y * w + 5] = clothColor * 0.7f;
                pixels[y * w + 6] = clothColor * 0.7f;
                pixels[y * w + 7] = clothColor * 0.7f;
            }

            // 画眼睛 (y=15, x=4 和 x=7)
            pixels[15 * w + 4] = Color.black;
            pixels[15 * w + 7] = Color.black;

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 12f);
        }

        /// <summary>
        /// 在玩家附近找到地表位置用于NPC生成
        /// </summary>
        private Vector2 FindNPCSurfacePosition()
        {
            var player = PlayerController.Instance;
            if (player == null) return Vector2.zero;

            // 在玩家附近随机偏移15-30格
            float offsetX = Random.Range(15f, 30f) * (Random.value > 0.5f ? 1f : -1f);
            Vector2 basePos = (Vector2)player.transform.position + new Vector2(offsetX, 10f);

            // 向下射线找地表
            RaycastHit2D hit = Physics2D.Raycast(basePos, Vector2.down, 50f, LayerMask.GetMask("Ground"));
            if (hit.collider != null)
            {
                return hit.point + Vector2.up * 1.5f;
            }

            // 找不到地面就用玩家附近位置
            return (Vector2)player.transform.position + new Vector2(offsetX, 0f);
        }

        /// <summary>
        /// 检查玩家附近是否有可交互的NPC
        /// </summary>
        private void CheckNPCInteraction()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            for (int i = _spawnedNPCObjects.Count - 1; i >= 0; i--)
            {
                var npcObj = _spawnedNPCObjects[i];
                if (npcObj == null)
                {
                    _spawnedNPCObjects.RemoveAt(i);
                    continue;
                }
                float dist = Vector2.Distance(player.transform.position, npcObj.transform.position);
                if (dist < 2f)
                {
                    // 获取对应NPC数据
                    string npcName = npcObj.name.Replace("NPC_", "");
                    NPCData npcData = AllNPCs.Find(n => n.name == npcName);
                    if (npcData != null)
                    {
                        Debug.Log($"[交互] 与 {npcData.name} 对话 - {npcData.description}");
                        Debug.Log($"[商店] 可购买物品ID: {string.Join(", ", npcData.shopItems)}");
                    }
                    else
                    {
                        Debug.Log($"[交互] 与NPC交互: {npcObj.name}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 检查NPC是否满足出生条件
        /// </summary>
        public bool CheckSpawnCondition(NPCData npc)
        {
            var gm = GameManager.Instance;
            var inv = Inventory.Instance;
            if (gm == null || inv == null) return false;

            switch (npc.spawnCondition)
            {
                case NPCCondition.Always: return true;
                case NPCCondition.HasGun: return inv.HasItem(250) || inv.HasItem(251) || inv.HasItem(252);
                case NPCCondition.HasCyberware: return CyberwareSystem.Instance != null && !CyberwareSystem.Instance.Slots[0].IsEmpty;
                case NPCCondition.HasCircuitBoard: return inv.CountItem(22) >= 5;
                case NPCCondition.HasHealthItem: return inv.HasItem(300);
                case NPCCondition.HasExplosive: return false; // TODO
                case NPCCondition.BossDefeated1: return gm.bossesDefeated >= 1;
                case NPCCondition.BossDefeated2: return gm.bossesDefeated >= 2;
                case NPCCondition.HardMode: return gm.hardmodeActivated;
                default: return false;
            }
        }
    }

    [System.Serializable]
    public class NPCData
    {
        public string id;
        public string name;
        public string description;
        public NPCCondition spawnCondition;
        public int[] shopItems;
        public bool isSpawned;
    }

    public enum NPCCondition
    {
        Always, HasGun, HasCyberware, HasCircuitBoard,
        HasHealthItem, HasExplosive,
        BossDefeated1, BossDefeated2, HardMode
    }
}
