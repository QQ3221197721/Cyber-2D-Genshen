using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// Boss: 机械之眼 (赛博版克苏鲁之眼)
    /// 阶段1: 飞行+冲刺+召唤小无人机
    /// 阶段2: HP<50%时狂暴，扫描激光+连续冲刺
    /// </summary>
    public class BossMechEye : MonoBehaviour
    {
        [Header("属性")]
        public float maxHealth = 2000f;
        public float currentHealth;
        public int contactDamage = 30;
        public int laserDamage = 45;
        public float moveSpeed = 5f;

        public bool IsAlive => currentHealth > 0;
        public bool IsPhase2 => currentHealth < maxHealth * 0.5f;

        private Transform _player;
        private Rigidbody2D _rb;
        private float _actionTimer;
        private int _actionIndex;
        private bool _isEnraged;

        public static BossMechEye Spawn(Vector2 position)
        {
            var go = new GameObject("Boss_MechEye");
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Enemy");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateBossSprite();
            sr.sortingLayerName = "Entities";
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.5f;
            col.isTrigger = true;

            var boss = go.AddComponent<BossMechEye>();
            boss.currentHealth = boss.maxHealth;
            boss._rb = rb;

            return boss;
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
                _player = PlayerController.Instance.transform;
            _actionTimer = 2f;
        }

        private void Update()
        {
            if (!IsAlive || _player == null) return;

            _actionTimer -= Time.deltaTime;
            if (_actionTimer <= 0f)
            {
                PerformAction();
            }

            // 被动移动：围绕玩家缓慢环绕
            Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x).normalized;
            float dist = toPlayer.magnitude;

            if (dist > 8f)
                _rb.velocity = toPlayer.normalized * moveSpeed;
            else if (dist < 4f)
                _rb.velocity = -toPlayer.normalized * moveSpeed * 0.5f + perpendicular * moveSpeed;
            else
                _rb.velocity = perpendicular * moveSpeed * 0.8f;

            // 朝向玩家
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // 阶段2检测
            if (IsPhase2 && !_isEnraged)
            {
                _isEnraged = true;
                moveSpeed *= 1.5f;
                contactDamage = 45;
            }
        }

        private void PerformAction()
        {
            _actionIndex = (_actionIndex + 1) % (IsPhase2 ? 4 : 3);

            switch (_actionIndex)
            {
                case 0: StartCoroutine(DashAttack()); break;
                case 1: SpawnMiniDrones(); break;
                case 2: StartCoroutine(LaserSweep()); break;
                case 3: StartCoroutine(RapidDash()); break;
            }

            _actionTimer = IsPhase2 ? 2f : 3f;
        }

        private IEnumerator DashAttack()
        {
            if (_player == null) yield break;
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;

            // 蓄力
            _rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.5f);

            // 冲刺
            _rb.velocity = dir * 20f;
            yield return new WaitForSeconds(0.4f);
            _rb.velocity *= 0.2f;
        }

        private IEnumerator RapidDash()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_player == null) yield break;
                Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                _rb.velocity = dir * 25f;
                yield return new WaitForSeconds(0.25f);
                _rb.velocity *= 0.1f;
                yield return new WaitForSeconds(0.15f);
            }
        }

        private void SpawnMiniDrones()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 3f;
                EnemyFactory.SpawnEnemy("HoverDrone", (Vector2)transform.position + offset);
            }
        }

        private IEnumerator LaserSweep()
        {
            // 简化版激光（生成一系列投射物）
            if (_player == null) yield break;

            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * 360f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnLaserProjectile(dir);
                yield return new WaitForSeconds(0.08f);
            }
        }

        private void SpawnLaserProjectile(Vector2 direction)
        {
            GameObject proj = new GameObject("BossLaser");
            proj.transform.position = transform.position;
            proj.layer = LayerMask.NameToLayer("Projectile");

            var sr = proj.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0f, 0.3f);
            sr.sortingLayerName = "Effects";

            var rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.velocity = direction * 15f;

            var col = proj.AddComponent<CircleCollider2D>();
            col.radius = 0.2f;
            col.isTrigger = true;

            var bullet = proj.AddComponent<Projectile>();
            bullet.damage = laserDamage;
            bullet.ownerIsPlayer = false;

            Destroy(proj, 3f);
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            // 上报进度系统
            if (ProgressionSystem.Instance != null)
            {
                ProgressionSystem.Instance.RegisterBossKill("MechEye");
            }

            // 掉落奖励
            Inventory.Instance?.AddItem(33, 15); // 量子芯片
            Inventory.Instance?.AddItem(32, 10); // 等离子电池
            Inventory.Instance?.AddItem(31, 20); // 霓虹晶体

            var particles = FindObjectOfType<ParticleManager>();
            if (particles != null)
                particles.SpawnBreakParticles(transform.position, new Color(1f, 0f, 0.5f));

            Destroy(gameObject);
            Debug.Log("[BOSS] 机械之眼 已被击败！");
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerStats>();
            if (player != null)
            {
                Vector2 dir = (other.transform.position - transform.position).normalized;
                player.TakeDamage(contactDamage, dir);
            }
        }

        private static Sprite GenerateBossSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] clear = new Color[size * size];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            // 圆形主体
            Color body = new Color(0.3f, 0.3f, 0.4f);
            Color iris = new Color(1f, 0f, 0.3f);
            Color pupil = new Color(0.1f, 0f, 0.05f);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - 16f, dy = y - 16f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < 14f) // 外壳
                    {
                        float shade = 1f - dist / 14f * 0.3f;
                        tex.SetPixel(x, y, body * shade);
                    }
                    if (dist < 8f) // 虹膜
                    {
                        tex.SetPixel(x, y, iris * (1f - dist / 8f * 0.3f));
                    }
                    if (dist < 3f) // 瞳孔
                    {
                        tex.SetPixel(x, y, pupil);
                    }
                }
            }

            // 机械纹理
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                int bx = 16 + (int)(Mathf.Cos(a) * 12);
                int by = 16 + (int)(Mathf.Sin(a) * 12);
                if (bx >= 0 && bx < size && by >= 0 && by < size)
                    tex.SetPixel(bx, by, new Color(0f, 0.8f, 1f));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 16f);
        }
    }
}
