using UnityEngine;
using System;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家属性系统 - HP/Mana/Defense/各种属性
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("生命值")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public float healthRegen = 1f; // 每秒自动回复

        [Header("魔力(能量)")]
        public int maxMana = 50;
        public int currentMana = 50;
        public float manaRegen = 2f;

        [Header("属性")]
        public int defense = 0;
        public float moveSpeedBonus = 0f;
        public float damageMultiplier = 1f;
        public float miningSpeedMultiplier = 1f;

        [Header("无敌时间")]
        public float invincibilityDuration = 1f;
        private float _invincibilityTimer;

        // 事件
        public event Action<int, int> OnHealthChanged;  // current, max
        public event Action<int, int> OnManaChanged;
        public event Action OnDeath;
        public event Action<int> OnDamaged; // damage amount

        public bool IsAlive => currentHealth > 0;
        public bool IsInvincible => _invincibilityTimer > 0f;
        public float HealthPercent => (float)currentHealth / maxHealth;
        public float ManaPercent => (float)currentMana / maxMana;

        private float _regenTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!IsAlive) return;

            // 无敌帧倒计时
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= Time.deltaTime;

            // 自然回复
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 1f)
            {
                _regenTimer = 0f;
                if (currentHealth < maxHealth)
                {
                    currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.CeilToInt(healthRegen));
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                }
                if (currentMana < maxMana)
                {
                    currentMana = Mathf.Min(maxMana, currentMana + Mathf.CeilToInt(manaRegen));
                    OnManaChanged?.Invoke(currentMana, maxMana);
                }
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int rawDamage, Vector2 knockbackDir = default)
        {
            if (!IsAlive || IsInvincible) return;

            // 防御减伤: 每点防御减少0.5伤害
            int finalDamage = Mathf.Max(1, rawDamage - defense / 2);
            currentHealth -= finalDamage;

            _invincibilityTimer = invincibilityDuration;
            OnDamaged?.Invoke(finalDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // 击退
            if (knockbackDir != Vector2.zero)
            {
                var pc = GetComponent<PlayerController>();
                if (pc != null) pc.ApplyKnockback(knockbackDir, 8f);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// 消耗魔力
        /// </summary>
        public bool UseMana(int amount)
        {
            if (currentMana < amount) return false;
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }

        /// <summary>
        /// 恢复魔力
        /// </summary>
        public void RestoreMana(int amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            Debug.Log("[PlayerStats] 玩家死亡！");
        }

        /// <summary>
        /// 复活
        /// </summary>
        public void Respawn()
        {
            currentHealth = maxHealth / 2;
            currentMana = maxMana;
            _invincibilityTimer = 3f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        /// <summary>
        /// 增加最大生命值（类似Terraria的生命水晶）
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
