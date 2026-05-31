using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家战斗系统 - 近战挥砍+远程射击
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("设置")]
        public float attackCooldown = 0.3f;
        public Transform projectileSpawnPoint;

        private float _attackTimer;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.isPaused) return;
            if (GameManager.Instance.isInventoryOpen) return;

            _attackTimer -= Time.deltaTime;

            if (Input.GetMouseButton(0) && _attackTimer <= 0f)
            {
                var item = Inventory.Instance?.GetSelectedItem();
                if (item == null) return;

                if (item.category == ItemCategory.MeleeWeapon)
                    MeleeAttack(item);
                else if (item.category == ItemCategory.RangedWeapon)
                    RangedAttack(item);
            }
        }

        private void MeleeAttack(ItemData weapon)
        {
            float cooldown = 0.4f / weapon.attackSpeed;
            _attackTimer = cooldown;

            // 计算攻击方向
            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

            // 扇形检测范围内的敌人
            float range = weapon.range;
            float dmgMult = PlayerStats.Instance != null ? PlayerStats.Instance.damageMultiplier : 1f;
            int damage = Mathf.RoundToInt(weapon.damage * dmgMult);

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position + dir * (range * 0.5f),
                range * 0.6f,
                LayerMask.GetMask("Enemy")
            );

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    enemy.TakeDamage(damage, knockDir);
                }
            }
        }

        private void RangedAttack(ItemData weapon)
        {
            float cooldown = 1f / weapon.attackSpeed;
            _attackTimer = cooldown;

            // 方向
            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

            float dmgMult = PlayerStats.Instance != null ? PlayerStats.Instance.damageMultiplier : 1f;
            int damage = Mathf.RoundToInt(weapon.damage * dmgMult);

            SpawnProjectile(dir, damage, weapon.displayColor);
        }

        private void SpawnProjectile(Vector2 direction, int damage, Color color)
        {
            GameObject proj = new GameObject("PlayerProjectile");
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.8f;
            proj.transform.position = spawnPos;
            proj.layer = LayerMask.NameToLayer("Projectile");

            // 视觉
            var sr = proj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBulletSprite(color);
            sr.sortingLayerName = "Effects";

            // 物理
            var rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.05f;
            rb.velocity = direction * 20f;

            var col = proj.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            col.isTrigger = true;

            // 弹道组件
            var bullet = proj.AddComponent<Projectile>();
            bullet.damage = damage;
            bullet.ownerIsPlayer = true;
            bullet.lifetime = 3f;

            Destroy(proj, 3f);
        }

        private Sprite CreateBulletSprite(Color color)
        {
            Texture2D tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 6; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(2.5f, 2.5f));
                    tex.SetPixel(x, y, dist < 2.5f ? color : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 6, 6), Vector2.one * 0.5f, 6f);
        }
    }

    /// <summary>
    /// 投射物组件
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public int damage = 10;
        public bool ownerIsPlayer = true;
        public float lifetime = 3f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (ownerIsPlayer)
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    Vector2 dir = (other.transform.position - transform.position).normalized;
                    enemy.TakeDamage(damage, dir);
                    Destroy(gameObject);
                }
            }
            else
            {
                var player = other.GetComponent<PlayerStats>();
                if (player != null)
                {
                    Vector2 dir = (other.transform.position - transform.position).normalized;
                    player.TakeDamage(damage, dir);
                    Destroy(gameObject);
                }
            }

            // 碰到地面
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Destroy(gameObject);
            }
        }
    }
}
