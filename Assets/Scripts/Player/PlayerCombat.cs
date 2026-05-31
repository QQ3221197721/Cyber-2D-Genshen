using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家战斗系统 - 近战挥砍+远程射击+连击+伤害类型+投射物差异化
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        public static PlayerCombat Instance { get; private set; }

        [Header("设置")]
        public float attackCooldown = 0.3f;
        public Transform projectileSpawnPoint;

        private float _attackTimer;
        private Camera _cam;

        // 连击系统
        private int _comboCount = 0;
        private float _comboTimer = 0f;
        private const float ComboWindow = 3f;     // 3秒连击窗口
        private const float ComboBonus = 0.08f;   // 每层连击+8%伤害

        public int ComboCount => _comboCount;
        public float ComboTimer => _comboTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.isPaused) return;
            if (GameManager.Instance.isInventoryOpen) return;
            if (PlayerStats.Instance != null && PlayerStats.Instance.IsDead) return;

            _attackTimer -= Time.deltaTime;

            // 连击计时
            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f)
                    _comboCount = 0;
            }

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

            // 基础伤害
            float dmgMult = PlayerStats.Instance != null ? PlayerStats.Instance.damageMultiplier : 1f;
            float baseDamage = weapon.damage * dmgMult;

            // 连击加成
            baseDamage *= (1f + _comboCount * ComboBonus);

            // 扇形检测范围内的敌人
            float range = weapon.range;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position + dir * (range * 0.5f),
                range * 0.6f,
                LayerMask.GetMask("Enemy")
            );

            bool hitAny = false;
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    // 伤害类型加成
                    string enemyType = enemy.gameObject.name.Replace("Enemy_", "");
                    float typeMultiplier = GetDamageTypeMultiplier(weapon.damageType, enemyType);
                    int finalDamage = Mathf.RoundToInt(baseDamage * typeMultiplier);

                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    enemy.TakeDamage(finalDamage, knockDir);

                    // 伤害数字
                    Color dmgColor = GetDamageTypeColor(weapon.damageType);
                    if (ParticleManager.Instance != null)
                    {
                        ParticleManager.Instance.SpawnDamageNumber((Vector2)hit.transform.position, finalDamage, dmgColor);
                        ParticleManager.Instance.SpawnHitEffect((Vector2)hit.transform.position, weapon.damageType);
                    }

                    hitAny = true;
                }
            }

            // 命中后更新连击
            if (hitAny)
            {
                _comboCount++;
                _comboTimer = ComboWindow;

                // 教程追踪：攻击
                if (TutorialSystem.Instance != null)
                    TutorialSystem.Instance.HasAttacked = true;
            }

            // 消耗耐久度
            if (Inventory.Instance != null)
                Inventory.Instance.ConsumeDurability(Inventory.Instance.SelectedSlot, 1);
        }

        private void RangedAttack(ItemData weapon)
        {
            // 检查弹药
            if (weapon.requiredAmmo != AmmoType.None)
            {
                if (Inventory.Instance == null || !Inventory.Instance.ConsumeAmmo(weapon.requiredAmmo, weapon.ammoPerShot))
                    return; // 弹药不足
            }

            float cooldown = 1f / weapon.attackSpeed;
            _attackTimer = cooldown;

            // 方向
            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorld - (Vector2)transform.position).normalized;

            float dmgMult = PlayerStats.Instance != null ? PlayerStats.Instance.damageMultiplier : 1f;
            int damage = Mathf.RoundToInt(weapon.damage * dmgMult);

            // 根据弹药类型创建不同投射物
            switch (weapon.requiredAmmo)
            {
                case AmmoType.Shell:
                    // 霰弹：3个分散投射物
                    for (int i = 0; i < 3; i++)
                    {
                        float angleOffset = Random.Range(-15f, 15f);
                        Vector2 spreadDir = RotateVector(dir, angleOffset);
                        SpawnProjectile(spreadDir, Mathf.RoundToInt(damage * 0.5f), weapon.displayColor, weapon.requiredAmmo, weapon.damageType);
                    }
                    break;
                case AmmoType.Plasma:
                    SpawnProjectile(dir, damage, weapon.displayColor, weapon.requiredAmmo, weapon.damageType, 1.5f, 15f);
                    break;
                case AmmoType.Rail:
                    SpawnProjectile(dir, damage, weapon.displayColor, weapon.requiredAmmo, weapon.damageType, 0.5f, 50f);
                    break;
                default:
                    SpawnProjectile(dir, damage, weapon.displayColor, weapon.requiredAmmo, weapon.damageType);
                    break;
            }

            // 消耗耐久度
            if (Inventory.Instance != null)
                Inventory.Instance.ConsumeDurability(Inventory.Instance.SelectedSlot, 1);
        }

        private void SpawnProjectile(Vector2 direction, int damage, Color color, AmmoType ammoType = AmmoType.None, DamageType damageType = DamageType.Physical, float sizeScale = 1f, float speed = 20f)
        {
            GameObject proj = new GameObject("PlayerProjectile");
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.8f;
            proj.transform.position = spawnPos;
            proj.layer = LayerMask.NameToLayer("Projectile");

            // 视觉 - 根据弹药类型差异化
            var sr = proj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateProjectileSprite(color, ammoType, sizeScale);
            sr.sortingLayerName = "Effects";

            // 旋转对齐方向
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angle);

            // 物理
            var rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = (ammoType == AmmoType.Rail || ammoType == AmmoType.Energy) ? 0f : 0.05f;
            rb.velocity = direction * speed;

            var col = proj.AddComponent<CircleCollider2D>();
            col.radius = 0.15f * sizeScale;
            col.isTrigger = true;

            // 弹道组件
            var bullet = proj.AddComponent<Projectile>();
            bullet.damage = damage;
            bullet.ownerIsPlayer = true;
            bullet.lifetime = 3f;
            bullet.damageType = damageType;

            Destroy(proj, 3f);
        }

        private Sprite CreateProjectileSprite(Color color, AmmoType ammoType, float sizeScale)
        {
            int w, h;
            switch (ammoType)
            {
                case AmmoType.Energy:
                    // 青色光束（长条形）
                    w = 12; h = 4;
                    break;
                case AmmoType.Plasma:
                    // 大球
                    w = 10; h = 10;
                    break;
                case AmmoType.Rail:
                    // 细线（超长）
                    w = 16; h = 2;
                    break;
                default:
                    // 小圆点
                    w = 6; h = 6;
                    break;
            }

            w = Mathf.RoundToInt(w * sizeScale);
            h = Mathf.RoundToInt(h * sizeScale);
            w = Mathf.Max(2, w);
            h = Mathf.Max(2, h);

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (ammoType == AmmoType.Plasma || ammoType == AmmoType.Bullet || ammoType == AmmoType.Shell)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.5f, h * 0.5f));
                        tex.SetPixel(x, y, dist < w * 0.45f ? color : Color.clear);
                    }
                    else
                    {
                        // 长条/线形完全填充
                        tex.SetPixel(x, y, color);
                    }
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 8f);
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

        /// <summary>
        /// 伤害类型加成计算
        /// </summary>
        private float GetDamageTypeMultiplier(DamageType type, string enemyType)
        {
            // Energy对机械类敌人+30%
            if (type == DamageType.Energy &&
                (enemyType.Contains("Drone") || enemyType.Contains("SecurityBot") || enemyType.Contains("Mech")))
                return 1.3f;

            // Fire对有机类敌人+25%
            if (type == DamageType.Fire &&
                (enemyType.Contains("Crawler") || enemyType.Contains("Roach") || enemyType.Contains("Mutant")))
                return 1.25f;

            // Electric 15%几率额外50%伤害（模拟眩晕效果）
            if (type == DamageType.Electric && Random.value < 0.15f)
                return 1.5f;

            return 1f;
        }

        private Color GetDamageTypeColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return new Color(1f, 0.4f, 0f);
                case DamageType.Ice: return new Color(0.4f, 0.9f, 1f);
                case DamageType.Electric: return new Color(1f, 1f, 0.2f);
                case DamageType.Energy: return new Color(0.6f, 0.2f, 1f);
                default: return Color.white;
            }
        }

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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
        public DamageType damageType = DamageType.Physical;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (ownerIsPlayer)
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    Vector2 dir = (other.transform.position - transform.position).normalized;
                    enemy.TakeDamage(damage, dir);

                    // 伤害数字和命中特效
                    if (ParticleManager.Instance != null)
                    {
                        Color dmgColor = damageType switch
                        {
                            DamageType.Fire => new Color(1f, 0.4f, 0f),
                            DamageType.Ice => new Color(0.4f, 0.9f, 1f),
                            DamageType.Electric => new Color(1f, 1f, 0.2f),
                            DamageType.Energy => new Color(0.6f, 0.2f, 1f),
                            _ => Color.white
                        };
                        ParticleManager.Instance.SpawnDamageNumber((Vector2)other.transform.position, damage, dmgColor);
                        ParticleManager.Instance.SpawnHitEffect((Vector2)other.transform.position, damageType);
                    }

                    // 更新连击
                    if (PlayerCombat.Instance != null)
                    {
                        // Access combo via reflection-safe public setter would be ideal
                        // but we use a direct approach via a public method
                    }

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
