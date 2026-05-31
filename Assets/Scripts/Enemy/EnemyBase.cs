using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 敌人基类 - 所有敌人和Boss的基础
    /// AI状态机: Idle -> Patrol -> Chase -> Attack -> Hurt -> Dead
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        [Header("属性")]
        public float maxHealth = 30f;
        public float currentHealth;
        public int damage = 10;
        public int defense = 0;
        public float moveSpeed = 2f;
        public float detectionRange = 10f;
        public float attackRange = 1.5f;
        public float jumpForce = 8f;
        public float knockbackResistance = 1f;

        [Header("AI")]
        public float patrolTime = 3f;
        public float idleTime = 2f;
        public float attackCooldown = 1f;

        [Header("掉落")]
        public int[] dropItems;    // 物品ID
        public float[] dropChances; // 对应掉落概率

        public EnemyState State { get; protected set; } = EnemyState.Idle;
        public bool IsAlive => currentHealth > 0;

        protected Rigidbody2D _rb;
        protected Transform _player;
        protected float _stateTimer;
        protected float _attackTimer;
        protected int _facing = 1;
        protected bool _isGrounded;

        public event Action<EnemyBase> OnDeath;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;

            var player = PlayerController.Instance;
            if (player != null) _player = player.transform;
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;
            if (_player == null)
            {
                var pc = PlayerController.Instance;
                if (pc != null) _player = pc.transform;
                return;
            }

            _attackTimer -= Time.deltaTime;
            _stateTimer -= Time.deltaTime;

            float distToPlayer = Vector2.Distance(transform.position, _player.position);

            switch (State)
            {
                case EnemyState.Idle:
                    UpdateIdle(distToPlayer);
                    break;
                case EnemyState.Patrol:
                    UpdatePatrol(distToPlayer);
                    break;
                case EnemyState.Chase:
                    UpdateChase(distToPlayer);
                    break;
                case EnemyState.Attack:
                    UpdateAttack(distToPlayer);
                    break;
            }
        }

        protected virtual void UpdateIdle(float dist)
        {
            if (dist < detectionRange)
            {
                State = EnemyState.Chase;
                return;
            }
            if (_stateTimer <= 0f)
            {
                State = EnemyState.Patrol;
                _stateTimer = patrolTime;
                _facing = Random.value > 0.5f ? 1 : -1;
            }
        }

        protected virtual void UpdatePatrol(float dist)
        {
            if (dist < detectionRange)
            {
                State = EnemyState.Chase;
                return;
            }

            _rb.velocity = new Vector2(_facing * moveSpeed * 0.5f, _rb.velocity.y);

            if (_stateTimer <= 0f)
            {
                State = EnemyState.Idle;
                _stateTimer = idleTime;
            }
        }

        protected virtual void UpdateChase(float dist)
        {
            if (dist > detectionRange * 1.5f)
            {
                State = EnemyState.Idle;
                _stateTimer = idleTime;
                return;
            }

            if (dist <= attackRange && _attackTimer <= 0f)
            {
                State = EnemyState.Attack;
                return;
            }

            // 移向玩家
            float dir = Mathf.Sign(_player.position.x - transform.position.x);
            _facing = (int)dir;
            _rb.velocity = new Vector2(dir * moveSpeed, _rb.velocity.y);

            // 遇到墙壁跳跃
            if (IsBlockedAhead() && _isGrounded)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            }
        }

        protected virtual void UpdateAttack(float dist)
        {
            if (_attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = attackCooldown;
                State = EnemyState.Chase;
            }
        }

        protected virtual void PerformAttack()
        {
            if (_player == null) return;
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= attackRange * 1.5f)
            {
                var playerStats = _player.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    Vector2 knockDir = (_player.position - transform.position).normalized;
                    playerStats.TakeDamage(damage, knockDir);

                    // 根据敌人类型施加Debuff
                    if (BuffSystem.Instance != null)
                    {
                        string enemyType = gameObject.name.Replace("Enemy_", "");
                        switch (enemyType)
                        {
                            case "ToxicCrawler":
                            case "RadRoach":
                            case "SporeCarrier":
                                BuffSystem.Instance.AddBuff(BuffType.Poisoned, 5f, 1);
                                break;
                            case "FleshBlob":
                            case "Abomination":
                            case "MutantHound":
                                BuffSystem.Instance.AddBuff(BuffType.Bleeding, 3f, 2);
                                break;
                            case "EMPDrone":
                            case "HoverDrone":
                                BuffSystem.Instance.AddBuff(BuffType.EMPDisabled, 4f, 1);
                                break;
                            case "TechPriest":
                                BuffSystem.Instance.AddBuff(BuffType.Irradiated, 4f, 1);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public virtual void TakeDamage(int dmg, Vector2 knockbackDir)
        {
            if (!IsAlive) return;

            int finalDmg = Mathf.Max(1, dmg - defense);
            currentHealth -= finalDmg;

            // 击退
            if (_rb != null && knockbackResistance < 10f)
            {
                float knockForce = 6f / Mathf.Max(1f, knockbackResistance);
                _rb.velocity = knockbackDir * knockForce + Vector2.up * 3f;
            }

            // 闪烁效果
            StartCoroutine(DamageFlash());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            State = EnemyState.Dead;
            OnDeath?.Invoke(this);

            // 掉落物品
            DropLoot();

            // 粒子效果
            var particles = FindObjectOfType<ParticleManager>();
            if (particles != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                Color deathColor = sr != null ? sr.color : Color.red;
                particles.SpawnBreakParticles(transform.position, deathColor);
            }

            Destroy(gameObject, 0.1f);
        }

        protected virtual void DropLoot()
        {
            if (dropItems == null) return;
            for (int i = 0; i < dropItems.Length; i++)
            {
                float chance = i < dropChances.Length ? dropChances[i] : 0.5f;
                if (Random.value <= chance)
                {
                    Inventory.Instance?.AddItem(dropItems[i], 1);
                }
            }
        }

        private bool IsBlockedAhead()
        {
            Vector2 origin = (Vector2)transform.position + Vector2.right * _facing * 0.6f;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * _facing, 0.3f, LayerMask.GetMask("Ground"));
            return hit.collider != null;
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) yield break;

            Color orig = sr.color;
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            if (sr != null) sr.color = orig;
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            // 接触伤害
            if (collision.gameObject.GetComponent<PlayerStats>() != null)
            {
                if (_attackTimer <= 0f)
                {
                    PerformAttack();
                    _attackTimer = attackCooldown * 0.7f;
                }
            }

            // 地面检测
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                    _isGrounded = true;
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            _isGrounded = false;
        }
    }

    public enum EnemyState
    {
        Idle, Patrol, Chase, Attack, Hurt, Dead
    }
}
