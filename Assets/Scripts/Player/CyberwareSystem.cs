using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

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

        // 主动能力冷却
        private Dictionary<ActiveAbility, float> _abilityCooldowns = new Dictionary<ActiveAbility, float>();
        private const float BulletTimeDuration = 3f;
        private const float BulletTimeCooldown = 30f;
        private const float MissileCooldown = 15f;
        private const float MonowireCooldown = 8f;
        private const float EMPCooldown = 20f;
        private const float OverclockCooldown = 45f;

        public event Action OnCyberwareChanged;

        private void Awake()
        {
            Instance = this;
            InitSlots();
            InitDatabase();
        }

        private void Update()
        {
            // 更新冷却
            UpdateCooldowns();

            // Q键触发当前装备义体的主动能力
            if (Input.GetKeyDown(KeyCode.Q))
                TryActivateAbility();
        }

        private void UpdateCooldowns()
        {
            var keys = new List<ActiveAbility>(_abilityCooldowns.Keys);
            foreach (var key in keys)
            {
                _abilityCooldowns[key] -= Time.unscaledDeltaTime;
                if (_abilityCooldowns[key] <= 0f)
                    _abilityCooldowns.Remove(key);
            }
        }

        public void TryActivateAbility()
        {
            var active = GetEquippedActiveAbility();
            if (active == ActiveAbility.None) return;
            if (IsOnCooldown(active)) return;

            ExecuteAbility(active);
            SetCooldown(active);
        }

        public ActiveAbility GetEquippedActiveAbility()
        {
            foreach (var s in Slots)
            {
                if (s.installed != null && s.installed.activeAbility != ActiveAbility.None)
                    return s.installed.activeAbility;
            }
            return ActiveAbility.None;
        }

        public bool IsOnCooldown(ActiveAbility ability)
        {
            return _abilityCooldowns.ContainsKey(ability) && _abilityCooldowns[ability] > 0f;
        }

        public float GetCooldownRemaining(ActiveAbility ability)
        {
            if (_abilityCooldowns.ContainsKey(ability))
                return Mathf.Max(0f, _abilityCooldowns[ability]);
            return 0f;
        }

        public float GetCooldownMax(ActiveAbility ability)
        {
            switch (ability)
            {
                case ActiveAbility.BulletTime: return BulletTimeCooldown;
                case ActiveAbility.ArmMissile: return MissileCooldown;
                case ActiveAbility.MonowireWhip: return MonowireCooldown;
                case ActiveAbility.EMPBurst: return EMPCooldown;
                case ActiveAbility.Overclock: return OverclockCooldown;
                default: return 1f;
            }
        }

        private void SetCooldown(ActiveAbility ability)
        {
            _abilityCooldowns[ability] = GetCooldownMax(ability);
        }

        private void ExecuteAbility(ActiveAbility ability)
        {
            switch (ability)
            {
                case ActiveAbility.BulletTime:
                    StartCoroutine(BulletTimeCoroutine());
                    break;
                case ActiveAbility.ArmMissile:
                    FireHomingMissile();
                    break;
                case ActiveAbility.MonowireWhip:
                    MonowireWhipAttack();
                    break;
                case ActiveAbility.EMPBurst:
                    EMPBurstAttack();
                    break;
                case ActiveAbility.Overclock:
                    StartCoroutine(OverclockCoroutine());
                    break;
            }
        }

        private IEnumerator BulletTimeCoroutine()
        {
            Time.timeScale = 0.3f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            float elapsed = 0f;
            while (elapsed < BulletTimeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void FireHomingMissile()
        {
            // 找到最近敌人
            var enemies = Physics2D.OverlapCircleAll(transform.position, 15f, LayerMask.GetMask("Enemy"));
            Transform nearest = null;
            float minDist = float.MaxValue;
            foreach (var col in enemies)
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = col.transform;
                }
            }

            if (nearest == null) return;

            // 生成导弹投射物
            Vector2 dir = (nearest.position - transform.position).normalized;
            GameObject missile = new GameObject("HomingMissile");
            missile.transform.position = (Vector2)transform.position + dir * 1f;
            missile.layer = LayerMask.NameToLayer("Projectile");

            var sr = missile.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color missileColor = new Color(1f, 0.5f, 0f);
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
                    tex.SetPixel(x, y, dist < 3.5f ? missileColor : Color.clear);
                }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 8f);
            sr.sortingLayerName = "Effects";

            var rb = missile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.velocity = dir * 12f;

            var col2 = missile.AddComponent<CircleCollider2D>();
            col2.radius = 0.3f;
            col2.isTrigger = true;

            var proj = missile.AddComponent<Projectile>();
            proj.damage = 50;
            proj.ownerIsPlayer = true;
            proj.lifetime = 4f;
            proj.damageType = DamageType.Fire;

            Destroy(missile, 4f);
        }

        private void MonowireWhipAttack()
        {
            // 前方扇形范围攻击
            float range = 5f;
            int damage = 35;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    enemy.TakeDamage(damage, knockDir);

                    if (ParticleManager.Instance != null)
                    {
                        ParticleManager.Instance.SpawnDamageNumber((Vector2)hit.transform.position, damage, new Color(1f, 0f, 0.78f));
                        ParticleManager.Instance.SpawnHitEffect((Vector2)hit.transform.position, DamageType.Energy);
                    }
                }
            }
        }

        private void EMPBurstAttack()
        {
            // EMP爆发：范围内所有敌人眩晕+伤害
            float range = 8f;
            int damage = 25;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, Vector2.zero);
                    if (ParticleManager.Instance != null)
                        ParticleManager.Instance.SpawnHitEffect((Vector2)hit.transform.position, DamageType.Electric);
                }
            }
        }

        private IEnumerator OverclockCoroutine()
        {
            // 超频：攻速+50%, 移速+30% 持续10秒
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.damageMultiplier += 0.5f;
            if (PlayerController.Instance != null)
                PlayerController.Instance.moveSpeed += 2f;

            yield return new WaitForSeconds(10f);

            if (PlayerStats.Instance != null)
                PlayerStats.Instance.damageMultiplier -= 0.5f;
            if (PlayerController.Instance != null)
                PlayerController.Instance.moveSpeed -= 2f;
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
