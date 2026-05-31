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
        public bool IsDead { get; private set; }
        public float HealthPercent => (float)currentHealth / maxHealth;
        public float ManaPercent => (float)currentMana / maxMana;

        private float _regenTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!IsAlive || IsDead) return;

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

        private Texture2D _deathOverlayTex;

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log("[PlayerStats] 玩家死亡！");

            // 禁用输入
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.InputEnabled = false;
                // 角色倒地动画（旋转90度）
                PlayerController.Instance.transform.rotation = Quaternion.Euler(0, 0, 90f);
                // 停止移动
                var rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.isKinematic = true;
                }
            }

            // 初始化遗罩贴图
            if (_deathOverlayTex == null)
            {
                _deathOverlayTex = new Texture2D(1, 1);
                _deathOverlayTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
                _deathOverlayTex.Apply();
            }
        }

        private void ExecuteRespawn()
        {
            // 恢复满HP
            currentHealth = maxHealth;
            currentMana = maxMana;
            _invincibilityTimer = 3f;
            IsDead = false;

            if (PlayerController.Instance != null)
            {
                // 角色站起来（旋转归零）
                PlayerController.Instance.transform.rotation = Quaternion.identity;

                // 传送回出生点
                var gm = GameManager.Instance;
                float spawnX = gm != null ? gm.worldWidth / 2f : 210f;
                float spawnY = -55f;

                var worldGen = UnityEngine.Object.FindObjectOfType<WorldGenerator>();
                if (worldGen != null)
                {
                    Vector2 spawn = worldGen.GetSpawnPoint();
                    spawnX = spawn.x;
                    spawnY = -spawn.y;
                }

                PlayerController.Instance.transform.position = new Vector3(spawnX, spawnY, 0);

                // 重置物理
                var rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector2.zero;
                }

                // 恢复输入
                PlayerController.Instance.InputEnabled = true;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnManaChanged?.Invoke(currentMana, maxMana);
            Debug.Log("[PlayerStats] 玩家已重生");
        }

        private void OnGUI()
        {
            if (!IsDead) return;

            // 全屏半透明黑色遮罩
            if (_deathOverlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _deathOverlayTex);

            // "你已死亡" 大号居中白色文字
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 52;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 80), "你已死亡", titleStyle);

            // 按钮区域
            float btnW = 180f;
            float btnH = 50f;
            float btnY = Screen.height * 0.55f;
            float centerX = Screen.width / 2f;

            // 按钮样式
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 20;
            btnStyle.normal.textColor = Color.white;
            btnStyle.fontStyle = FontStyle.Bold;

            // "复活" 按钮
            if (GUI.Button(new Rect(centerX - btnW - 20, btnY, btnW, btnH), "复活", btnStyle))
            {
                ExecuteRespawn();
            }

            // "退出游戏" 按钮
            if (GUI.Button(new Rect(centerX + 20, btnY, btnW, btnH), "退出游戏", btnStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        /// <summary>
        /// 复活（外部调用备用）
        /// </summary>
        public void Respawn()
        {
            ExecuteRespawn();
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
