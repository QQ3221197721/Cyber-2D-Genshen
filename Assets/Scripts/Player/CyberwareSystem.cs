using UnityEngine;
using System;
using System.Collections.Generic;

namespace CyberTerraria
{
    public enum CyberwareSlotType { Neural, Eyes, LeftArm, RightArm, Torso, Legs, SubdermalArmor, Circulatory }
    public enum PassiveEffect { None, DoubleJump, ChargedJump, SilentMove, FallImmunity, AutoHeal, RegenBoost, AttackSpeedBoost, WallBreaker, DamageReflect, EnemyHighlight, WallHack, AutoDodge, KillFrenzy }
    public enum ActiveAbility { None, BulletTime, ArmMissile, MonowireWhip, EMPBurst, Overclock }

    [System.Serializable]
    public class CyberwareData
    {
        public string id;
        public string name;
        public string description;
        public CyberwareSlotType slotType;
        public int tier;
        public float bonusDamage, bonusDefense, bonusSpeed, bonusJump, bonusHealth;
        public PassiveEffect passiveEffect;
        public ActiveAbility activeAbility;
        public int[] craftItemIds;
        public int[] craftAmounts;
    }

    [System.Serializable]
    public class CyberwareSlot
    {
        public string slotName;
        public CyberwareSlotType slotType;
        public CyberwareData installed;
        public bool IsEmpty => installed == null;
    }

    /// <summary>
    /// 赛博义体系统 - 8个槽位，安装义体获得被动/主动能力
    /// 深度参考赛博朋克2077的义体设计
    /// </summary>
    public class CyberwareSystem : MonoBehaviour
    {
        public static CyberwareSystem Instance { get; private set; }

        public CyberwareSlot[] Slots { get; private set; }
        public List<CyberwareData> AllCyberware { get; private set; }

        public float BonusDamage { get; private set; }
        public float BonusDefense { get; private set; }
        public float BonusSpeed { get; private set; }
        public float BonusJump { get; private set; }
        public float BonusHealth { get; private set; }

        public event Action OnCyberwareChanged;

        private void Awake()
        {
            Instance = this;
            InitSlots();
            InitDatabase();
        }

        private void InitSlots()
        {
            Slots = new CyberwareSlot[]
            {
                new CyberwareSlot { slotName = "神经接口", slotType = CyberwareSlotType.Neural },
                new CyberwareSlot { slotName = "眼部植入", slotType = CyberwareSlotType.Eyes },
                new CyberwareSlot { slotName = "左臂义肢", slotType = CyberwareSlotType.LeftArm },
                new CyberwareSlot { slotName = "右臂义肢", slotType = CyberwareSlotType.RightArm },
                new CyberwareSlot { slotName = "躯干强化", slotType = CyberwareSlotType.Torso },
                new CyberwareSlot { slotName = "腿部强化", slotType = CyberwareSlotType.Legs },
                new CyberwareSlot { slotName = "皮下装甲", slotType = CyberwareSlotType.SubdermalArmor },
                new CyberwareSlot { slotName = "循环系统", slotType = CyberwareSlotType.Circulatory },
            };
        }

        private void InitDatabase()
        {
            AllCyberware = new List<CyberwareData>
            {
                // 神经接口
                new CyberwareData { id = "sandevistan_mk1", name = "桑德维斯坦 MK.1", description = "子弹时间3秒", slotType = CyberwareSlotType.Neural, tier = 1, activeAbility = ActiveAbility.BulletTime, craftItemIds = new[]{33,32}, craftAmounts = new[]{3,2} },
                new CyberwareData { id = "sandevistan_mk3", name = "桑德维斯坦 MK.3", description = "子弹时间5秒+加速", slotType = CyberwareSlotType.Neural, tier = 3, activeAbility = ActiveAbility.BulletTime, bonusSpeed = 2f, craftItemIds = new[]{33,32,31}, craftAmounts = new[]{8,5,3} },
                new CyberwareData { id = "kerenzikov", name = "科伦济科夫", description = "自动闪避15%", slotType = CyberwareSlotType.Neural, tier = 2, passiveEffect = PassiveEffect.AutoDodge, craftItemIds = new[]{33,30}, craftAmounts = new[]{5,4} },

                // 眼部
                new CyberwareData { id = "kiroshi_mk1", name = "蛐蛐光学 MK.1", description = "标记敌人+暗视", slotType = CyberwareSlotType.Eyes, tier = 1, passiveEffect = PassiveEffect.EnemyHighlight, craftItemIds = new[]{31,22}, craftAmounts = new[]{3,4} },
                new CyberwareData { id = "kiroshi_mk3", name = "蛐蛐光学 MK.3", description = "透视墙壁", slotType = CyberwareSlotType.Eyes, tier = 3, passiveEffect = PassiveEffect.WallHack, craftItemIds = new[]{31,33}, craftAmounts = new[]{6,4} },

                // 左臂
                new CyberwareData { id = "mantis_blades", name = "螳螂刀", description = "攻速+50% 伤害+15", slotType = CyberwareSlotType.LeftArm, tier = 2, bonusDamage = 15f, passiveEffect = PassiveEffect.AttackSpeedBoost, craftItemIds = new[]{30,34}, craftAmounts = new[]{10,5} },
                new CyberwareData { id = "monowire", name = "单分子线", description = "超远程鞭击", slotType = CyberwareSlotType.LeftArm, tier = 3, bonusDamage = 10f, activeAbility = ActiveAbility.MonowireWhip, craftItemIds = new[]{33,31}, craftAmounts = new[]{6,4} },

                // 右臂
                new CyberwareData { id = "gorilla_arms", name = "大猩猩手臂", description = "击退+200%+破墙", slotType = CyberwareSlotType.RightArm, tier = 2, bonusDamage = 20f, passiveEffect = PassiveEffect.WallBreaker, craftItemIds = new[]{30,34}, craftAmounts = new[]{12,6} },
                new CyberwareData { id = "arm_launcher", name = "导弹发射臂", description = "发射追踪导弹", slotType = CyberwareSlotType.RightArm, tier = 3, activeAbility = ActiveAbility.ArmMissile, craftItemIds = new[]{30,33,32}, craftAmounts = new[]{12,5,3} },

                // 躯干
                new CyberwareData { id = "titanium_bones", name = "钛合金骨骼", description = "防御+10 免疫摔落", slotType = CyberwareSlotType.Torso, tier = 2, bonusDefense = 10f, passiveEffect = PassiveEffect.FallImmunity, craftItemIds = new[]{30,34}, craftAmounts = new[]{15,5} },
                new CyberwareData { id = "biomonitor", name = "生物监测仪", description = "HP<25%时自动回复", slotType = CyberwareSlotType.Torso, tier = 1, passiveEffect = PassiveEffect.AutoHeal, craftItemIds = new[]{32,22}, craftAmounts = new[]{3,4} },

                // 腿部
                new CyberwareData { id = "reinforced_tendons", name = "强化肌腱", description = "二段跳", slotType = CyberwareSlotType.Legs, tier = 1, passiveEffect = PassiveEffect.DoubleJump, bonusJump = 3f, craftItemIds = new[]{30,34}, craftAmounts = new[]{5,3} },
                new CyberwareData { id = "fortified_ankles", name = "强化脚踝", description = "蓄力跳跃+悬浮", slotType = CyberwareSlotType.Legs, tier = 2, passiveEffect = PassiveEffect.ChargedJump, bonusJump = 5f, craftItemIds = new[]{30,32}, craftAmounts = new[]{8,3} },
                new CyberwareData { id = "lynx_paws", name = "猞猁爪垫", description = "无声移动+速度+30%", slotType = CyberwareSlotType.Legs, tier = 2, bonusSpeed = 1.5f, passiveEffect = PassiveEffect.SilentMove, craftItemIds = new[]{34,31}, craftAmounts = new[]{6,3} },

                // 皮下装甲
                new CyberwareData { id = "subdermal_armor_1", name = "皮下纳米装甲", description = "防御+8", slotType = CyberwareSlotType.SubdermalArmor, tier = 1, bonusDefense = 8f, craftItemIds = new[]{30,34}, craftAmounts = new[]{6,3} },
                new CyberwareData { id = "subdermal_armor_3", name = "量子装甲护盾", description = "防御+20 反弹10%伤害", slotType = CyberwareSlotType.SubdermalArmor, tier = 3, bonusDefense = 20f, passiveEffect = PassiveEffect.DamageReflect, craftItemIds = new[]{30,33,34}, craftAmounts = new[]{15,5,8} },

                // 循环系统
                new CyberwareData { id = "blood_pump", name = "二次心脏", description = "最大HP+50 回血+100%", slotType = CyberwareSlotType.Circulatory, tier = 2, bonusHealth = 50f, passiveEffect = PassiveEffect.RegenBoost, craftItemIds = new[]{32,31}, craftAmounts = new[]{4,3} },
                new CyberwareData { id = "adrenaline_booster", name = "肾上腺素加速器", description = "击杀后5秒攻击+20%", slotType = CyberwareSlotType.Circulatory, tier = 2, passiveEffect = PassiveEffect.KillFrenzy, craftItemIds = new[]{32,33}, craftAmounts = new[]{5,2} },
            };
        }

        /// <summary>
        /// 安装义体
        /// </summary>
        public bool Install(string cyberwareId)
        {
            var data = AllCyberware.Find(c => c.id == cyberwareId);
            if (data == null) return false;

            // 检查材料
            var inv = Inventory.Instance;
            if (inv == null) return false;
            for (int i = 0; i < data.craftItemIds.Length; i++)
            {
                if (!inv.HasItem(data.craftItemIds[i], data.craftAmounts[i]))
                    return false;
            }

            // 找到对应槽位
            CyberwareSlot slot = null;
            foreach (var s in Slots)
                if (s.slotType == data.slotType) { slot = s; break; }
            if (slot == null) return false;

            // 消耗材料
            for (int i = 0; i < data.craftItemIds.Length; i++)
                inv.RemoveItem(data.craftItemIds[i], data.craftAmounts[i]);

            slot.installed = data;
            RecalculateBonuses();
            OnCyberwareChanged?.Invoke();
            return true;
        }

        public void Uninstall(CyberwareSlotType slotType)
        {
            foreach (var s in Slots)
            {
                if (s.slotType == slotType)
                {
                    s.installed = null;
                    break;
                }
            }
            RecalculateBonuses();
            OnCyberwareChanged?.Invoke();
        }

        public bool HasPassive(PassiveEffect effect)
        {
            foreach (var s in Slots)
                if (s.installed != null && s.installed.passiveEffect == effect)
                    return true;
            return false;
        }

        private void RecalculateBonuses()
        {
            BonusDamage = 0; BonusDefense = 0; BonusSpeed = 0; BonusJump = 0; BonusHealth = 0;
            foreach (var s in Slots)
            {
                if (s.installed == null) continue;
                BonusDamage += s.installed.bonusDamage;
                BonusDefense += s.installed.bonusDefense;
                BonusSpeed += s.installed.bonusSpeed;
                BonusJump += s.installed.bonusJump;
                BonusHealth += s.installed.bonusHealth;
            }

            // 应用到玩家属性
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.defense = (int)BonusDefense;
                PlayerStats.Instance.damageMultiplier = 1f + BonusDamage * 0.02f;
            }
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.moveSpeed = 6f + BonusSpeed;
                PlayerController.Instance.jumpForce = 12f + BonusJump;
                PlayerController.Instance.maxJumps = HasPassive(PassiveEffect.DoubleJump) ? 2 : 1;
            }
        }
    }
}
