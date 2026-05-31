using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    public enum BuffType
    {
        // Debuffs
        Poisoned, Burning, Bleeding, Irradiated, EMPDisabled, Slowed, Blinded,
        // Buffs
        SpeedBoost, DamageBoost, DefenseBoost, Regeneration, NightVision, Invisible, Frenzy
    }

    [System.Serializable]
    public class ActiveBuff
    {
        public BuffType type;
        public float duration;
        public float remainingTime;
        public float tickTimer;
        public int power;
    }

    /// <summary>
    /// Buff/Debuff系统 - 状态效果管理
    /// 毒/燃烧/辐射/EMP瘫痪/加速/增伤等
    /// </summary>
    public class BuffSystem : MonoBehaviour
    {
        public static BuffSystem Instance { get; private set; }

        public List<ActiveBuff> ActiveBuffs { get; private set; } = new List<ActiveBuff>();

        public event System.Action OnBuffsChanged;

        private void Awake() { Instance = this; }

        private void Update()
        {
            for (int i = ActiveBuffs.Count - 1; i >= 0; i--)
            {
                var buff = ActiveBuffs[i];
                buff.remainingTime -= Time.deltaTime;

                // Tick效果（每秒触发）
                buff.tickTimer -= Time.deltaTime;
                if (buff.tickTimer <= 0f)
                {
                    buff.tickTimer = 1f;
                    ApplyTickEffect(buff);
                }

                if (buff.remainingTime <= 0f)
                {
                    RemoveBuffEffect(buff);
                    ActiveBuffs.RemoveAt(i);
                    OnBuffsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// 添加Buff/Debuff
        /// </summary>
        public void AddBuff(BuffType type, float duration, int power = 1)
        {
            // 检查是否已存在同类型，刷新时间
            var existing = ActiveBuffs.Find(b => b.type == type);
            if (existing != null)
            {
                existing.remainingTime = Mathf.Max(existing.remainingTime, duration);
                existing.power = Mathf.Max(existing.power, power);
                return;
            }

            var buff = new ActiveBuff
            {
                type = type,
                duration = duration,
                remainingTime = duration,
                tickTimer = 1f,
                power = power
            };

            ActiveBuffs.Add(buff);
            ApplyBuffEffect(buff);
            OnBuffsChanged?.Invoke();
        }

        /// <summary>
        /// 移除指定类型的Buff
        /// </summary>
        public void RemoveBuff(BuffType type)
        {
            var buff = ActiveBuffs.Find(b => b.type == type);
            if (buff != null)
            {
                RemoveBuffEffect(buff);
                ActiveBuffs.Remove(buff);
                OnBuffsChanged?.Invoke();
            }
        }

        public bool HasBuff(BuffType type)
        {
            return ActiveBuffs.Exists(b => b.type == type);
        }

        private void ApplyBuffEffect(ActiveBuff buff)
        {
            var stats = PlayerStats.Instance;
            var controller = PlayerController.Instance;
            if (stats == null) return;

            switch (buff.type)
            {
                case BuffType.SpeedBoost:
                    if (controller) controller.moveSpeed += 2f * buff.power;
                    break;
                case BuffType.DamageBoost:
                    stats.damageMultiplier += 0.2f * buff.power;
                    break;
                case BuffType.DefenseBoost:
                    stats.defense += 5 * buff.power;
                    break;
                case BuffType.Slowed:
                    if (controller) controller.moveSpeed -= 2f * buff.power;
                    break;
            }
        }

        private void RemoveBuffEffect(ActiveBuff buff)
        {
            var stats = PlayerStats.Instance;
            var controller = PlayerController.Instance;
            if (stats == null) return;

            switch (buff.type)
            {
                case BuffType.SpeedBoost:
                    if (controller) controller.moveSpeed -= 2f * buff.power;
                    break;
                case BuffType.DamageBoost:
                    stats.damageMultiplier -= 0.2f * buff.power;
                    break;
                case BuffType.DefenseBoost:
                    stats.defense -= 5 * buff.power;
                    break;
                case BuffType.Slowed:
                    if (controller) controller.moveSpeed += 2f * buff.power;
                    break;
            }
        }

        private void ApplyTickEffect(ActiveBuff buff)
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return;

            switch (buff.type)
            {
                case BuffType.Poisoned:
                    stats.TakeDamage(3 * buff.power);
                    break;
                case BuffType.Burning:
                    stats.TakeDamage(5 * buff.power);
                    break;
                case BuffType.Bleeding:
                    stats.TakeDamage(2 * buff.power);
                    break;
                case BuffType.Irradiated:
                    stats.TakeDamage(4 * buff.power);
                    break;
                case BuffType.Regeneration:
                    stats.Heal(5 * buff.power);
                    break;
            }
        }

        /// <summary>
        /// 获取所有活跃的Debuff数量（用于判断净化效果）
        /// </summary>
        public int GetDebuffCount()
        {
            int count = 0;
            foreach (var b in ActiveBuffs)
            {
                if (b.type <= BuffType.Blinded) count++;
            }
            return count;
        }

        /// <summary>
        /// 清除所有Debuff
        /// </summary>
        public void ClearDebuffs()
        {
            for (int i = ActiveBuffs.Count - 1; i >= 0; i--)
            {
                if (ActiveBuffs[i].type <= BuffType.Blinded)
                {
                    RemoveBuffEffect(ActiveBuffs[i]);
                    ActiveBuffs.RemoveAt(i);
                }
            }
            OnBuffsChanged?.Invoke();
        }
    }
}
