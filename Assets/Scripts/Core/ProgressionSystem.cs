using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 游戏进度系统 - 对标泰拉瑞亚的进度设计
    /// 
    /// 泰拉瑞亚结构 → 赛博废土对应:
    /// ─────────────────────────────────────────
    /// 前期(Pre-Hardmode):
    ///   克苏鲁之眼 → 机械之眼
    ///   世界吞噬者/克苏鲁之脑 → 数据蠕虫  
    ///   骷髅王 → 泰坦机甲
    ///   肉墙(开启困难模式) → 核心防火墙Boss
    /// 
    /// 困难模式(Hardmode):
    ///   机械Boss三连 → 超载三联体
    ///   世纪之花 → 量子花园守卫
    ///   石巨人 → 钢铁巨像
    /// 
    /// 终局(Endgame):
    ///   月亮领主 → 虚空核心Nexus
    /// 
    /// 世界规模对标:
    ///   小世界 4200x1200 (504万方块)
    ///   中世界 6400x1800 (1152万方块)  
    ///   大世界 8400x2400 (2016万方块)
    /// 
    /// 生物群落对标(赛博化):
    ///   森林 → 废土荒原
    ///   沙漠 → 辐射沙漠
    ///   雪地 → 冰晶冻原
    ///   丛林 → 变异丛林
    ///   腐化/猩红 → 数据腐蚀/异变肉域
    ///   神圣 → 霓虹净土
    ///   海洋 → 废液海岸
    ///   地牢 → 废弃数据中心
    ///   地狱 → 熔核深渊
    ///   太空 → 卫星轨道碎片
    /// </summary>
    public class ProgressionSystem : MonoBehaviour
    {
        public static ProgressionSystem Instance { get; private set; }

        // Boss击杀记录
        public HashSet<string> DefeatedBosses { get; private set; } = new HashSet<string>();

        // 游戏阶段
        public GamePhase CurrentPhase { get; private set; } = GamePhase.EarlyGame;

        // Boss列表（按进度顺序）
        public static readonly BossInfo[] AllBosses = new BossInfo[]
        {
            // === 前期 Boss ===
            new BossInfo("MechEye", "机械之眼", 2000, GamePhase.EarlyGame,
                "夜间使用「损坏的信号器」召唤", "一个觉醒的监控AI，拥有激光扫描和高速冲刺"),
            new BossInfo("DataWorm", "数据蠕虫", 4500, GamePhase.EarlyGame,
                "在腐蚀区域使用「腐蚀数据核心」", "巨型分节蠕虫，能腐蚀地形"),
            new BossInfo("TitanMech", "泰坦机甲", 6000, GamePhase.MidGame,
                "在废弃数据中心使用「过载电容器」", "远古战争遗留的巨型机甲"),
            new BossInfo("Firewall", "核心防火墙", 8000, GamePhase.MidGame,
                "在熔核深渊击杀守卫后自动触发", "世界底层的防御系统，击败后开启困难模式"),

            // === 困难模式 Boss ===
            new BossInfo("OverloadTwin", "超载双子", 12000, GamePhase.HardMode,
                "困难模式夜间使用「超载线圈」", "两个协同作战的AI核心"),
            new BossInfo("OverloadDestroyer", "超载歼灭者", 15000, GamePhase.HardMode,
                "困难模式使用「歼灭者信标」", "超长分节机械蛇"),
            new BossInfo("OverloadPrime", "超载Prime", 18000, GamePhase.HardMode,
                "困难模式使用「Prime启动器」", "四臂巨型战斗机器人"),
            new BossInfo("QuantumGuardian", "量子花园守卫", 22000, GamePhase.HardMode,
                "在地下紫晶区域破坏量子花蕾", "守护量子领域的植物机器混合体"),
            new BossInfo("SteelColossus", "钢铁巨像", 28000, GamePhase.LateHardMode,
                "在地下神殿激活巨像核心", "远古文明的终极武器"),

            // === 终局 Boss ===
            new BossInfo("NexusCore", "虚空核心", 50000, GamePhase.Endgame,
                "击败全部柱守后在世界中心出现", "控制整个废土世界的AI神"),
        };

        // 矿石进度（困难模式新增矿石）
        public static readonly string[] HardmodeOres = { "钴蓝合金", "秘银纤维", "精金芯片" };

        private void Awake() { Instance = this; }

        public void RegisterBossKill(string bossId)
        {
            DefeatedBosses.Add(bossId);
            GameManager.Instance.bossesDefeated = DefeatedBosses.Count;

            // 检查是否触发困难模式
            if (bossId == "Firewall" && !GameManager.Instance.hardmodeActivated)
            {
                ActivateHardMode();
            }

            UpdatePhase();
            Debug.Log($"[进度] Boss击败: {bossId}, 当前阶段: {CurrentPhase}");
        }

        private void ActivateHardMode()
        {
            GameManager.Instance.hardmodeActivated = true;
            Debug.Log("[进度] ★ 困难模式已激活！世界发生了巨大变化...");

            // 困难模式效果:
            // 1. 新矿石生成
            // 2. 新生物群落扩展（霓虹净土 + 加强版腐蚀）
            // 3. 新敌人类型解锁
            // 4. NPC解锁
        }

        private void UpdatePhase()
        {
            int defeated = DefeatedBosses.Count;
            if (defeated == 0) CurrentPhase = GamePhase.EarlyGame;
            else if (defeated <= 2) CurrentPhase = GamePhase.MidGame;
            else if (!GameManager.Instance.hardmodeActivated) CurrentPhase = GamePhase.MidGame;
            else if (defeated <= 7) CurrentPhase = GamePhase.HardMode;
            else if (defeated <= 9) CurrentPhase = GamePhase.LateHardMode;
            else CurrentPhase = GamePhase.Endgame;
        }

        public bool IsBossDefeated(string bossId) => DefeatedBosses.Contains(bossId);

        /// <summary>
        /// 获取当前阶段可用的最高矿石等级
        /// </summary>
        public int GetMaxOreLevel()
        {
            switch (CurrentPhase)
            {
                case GamePhase.EarlyGame: return 1; // 钛矿
                case GamePhase.MidGame: return 2;   // 霓虹晶+等离子
                case GamePhase.HardMode: return 3;  // 量子芯片
                case GamePhase.LateHardMode: return 4;
                case GamePhase.Endgame: return 5;
                default: return 1;
            }
        }
    }

    public enum GamePhase
    {
        EarlyGame,      // 前期
        MidGame,        // 中期(击败1-2个Boss)
        HardMode,       // 困难模式
        LateHardMode,   // 困难模式后期
        Endgame         // 终局
    }

    [System.Serializable]
    public class BossInfo
    {
        public string id;
        public string name;
        public float maxHealth;
        public GamePhase phase;
        public string summonMethod;
        public string description;

        public BossInfo(string id, string name, float hp, GamePhase phase, string summon, string desc)
        {
            this.id = id; this.name = name; this.maxHealth = hp;
            this.phase = phase; this.summonMethod = summon; this.description = desc;
        }
    }
}
