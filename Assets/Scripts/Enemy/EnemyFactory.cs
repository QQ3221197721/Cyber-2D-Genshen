using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 敌人工厂 - 程序化生成各种赛博朋克/废土敌人
    /// 三大类: 变异生物 / 机械敌人 / 人形掠夺者
    /// </summary>
    public static class EnemyFactory
    {
        public static GameObject SpawnEnemy(string type, Vector2 position)
        {
            GameObject enemy = new GameObject($"Enemy_{type}");
            enemy.transform.position = position;
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Entities";

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;

            var col = enemy.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            switch (type)
            {
                // === 变异生物 ===
                case "RadRoach":
                    Configure(enemy, sr, 15, 5, 4f, 6f, new Color(0.3f, 0.2f, 0.1f), 8, 8);
                    col.size = new Vector2(0.6f, 0.4f);
                    break;
                case "ToxicCrawler":
                    Configure(enemy, sr, 35, 8, 2f, 8f, new Color(0.2f, 0.6f, 0.1f), 12, 10);
                    break;
                case "FleshBlob":
                    Configure(enemy, sr, 60, 15, 1.5f, 5f, new Color(0.5f, 0.15f, 0.2f), 16, 16);
                    col.size = new Vector2(1.2f, 1f);
                    break;
                case "MutantHound":
                    Configure(enemy, sr, 40, 18, 5f, 15f, new Color(0.4f, 0.25f, 0.3f), 12, 12);
                    break;
                case "SporeCarrier":
                    Configure(enemy, sr, 30, 10, 1.5f, 12f, new Color(0.3f, 0.5f, 0.2f), 12, 14);
                    break;
                case "Abomination":
                    Configure(enemy, sr, 150, 30, 1.8f, 10f, new Color(0.4f, 0.1f, 0.15f), 20, 24);
                    col.size = new Vector2(1.5f, 2f);
                    var ab = enemy.GetComponent<EnemyBase>();
                    ab.knockbackResistance = 4f;
                    ab.defense = 5;
                    break;

                // === 机械敌人 ===
                case "HoverDrone":
                    Configure(enemy, sr, 25, 12, 3.5f, 15f, new Color(0.5f, 0.5f, 0.6f), 10, 10);
                    rb.gravityScale = 0f;
                    break;
                case "SecurityBot":
                    Configure(enemy, sr, 80, 20, 2f, 12f, new Color(0.4f, 0.4f, 0.5f), 14, 16);
                    var sec = enemy.GetComponent<EnemyBase>();
                    sec.defense = 8;
                    break;
                case "SpiderBot":
                    Configure(enemy, sr, 30, 12, 4.5f, 10f, new Color(0.25f, 0.25f, 0.3f), 10, 10);
                    break;
                case "HackedMech":
                    Configure(enemy, sr, 200, 35, 1.5f, 12f, new Color(0.3f, 0.3f, 0.35f), 20, 26);
                    col.size = new Vector2(1.8f, 2.5f);
                    var mech = enemy.GetComponent<EnemyBase>();
                    mech.defense = 12;
                    mech.knockbackResistance = 8f;
                    break;
                case "EMPDrone":
                    Configure(enemy, sr, 15, 5, 5f, 12f, new Color(0.3f, 0.5f, 0.7f), 8, 8);
                    rb.gravityScale = 0f;
                    break;
                case "SentryTurret":
                    Configure(enemy, sr, 50, 15, 0f, 18f, new Color(0.35f, 0.35f, 0.4f), 12, 12);
                    rb.bodyType = RigidbodyType2D.Static;
                    var turret = enemy.GetComponent<EnemyBase>();
                    turret.defense = 10;
                    break;

                // === 人形掠夺者 ===
                case "ScrapScavenger":
                    Configure(enemy, sr, 35, 12, 2.5f, 10f, new Color(0.5f, 0.4f, 0.35f), 10, 16);
                    break;
                case "RaiderGunner":
                    Configure(enemy, sr, 40, 15, 1.8f, 15f, new Color(0.3f, 0.25f, 0.2f), 10, 16);
                    break;
                case "RaiderBerserker":
                    Configure(enemy, sr, 60, 25, 4f, 8f, new Color(0.4f, 0.15f, 0.15f), 12, 18);
                    var berserker = enemy.GetComponent<EnemyBase>();
                    berserker.knockbackResistance = 3f;
                    break;
                case "TechPriest":
                    Configure(enemy, sr, 50, 10, 1.5f, 14f, new Color(0.3f, 0.1f, 0.4f), 12, 18);
                    break;
                case "CyberNinja":
                    Configure(enemy, sr, 45, 30, 6f, 12f, new Color(0.1f, 0.1f, 0.15f), 10, 16);
                    break;

                default:
                    Configure(enemy, sr, 25, 8, 2f, 8f, new Color(0.3f, 0.1f, 0.1f), 12, 14);
                    break;
            }

            // 设置掉落
            var eb = enemy.GetComponent<EnemyBase>();
            if (eb != null)
            {
                eb.dropItems = new int[] { 11, 22 }; // 锈铁、电路板
                eb.dropChances = new float[] { 0.4f, 0.15f };
            }

            return enemy;
        }

        private static void Configure(GameObject enemy, SpriteRenderer sr,
            float hp, int dmg, float speed, float detect, Color color, int sprW, int sprH)
        {
            sr.sprite = GenerateEnemySprite(color, sprW, sprH);
            sr.color = Color.white;

            var ai = enemy.AddComponent<EnemyBase>();
            ai.maxHealth = hp;
            ai.damage = dmg;
            ai.moveSpeed = speed;
            ai.detectionRange = detect;
        }

        /// <summary>
        /// 程序化生成敌人像素精灵
        /// </summary>
        private static Sprite GenerateEnemySprite(Color baseColor, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.3f + baseColor.r * 100, y * 0.3f + baseColor.g * 100);
                    Color c = baseColor * (0.7f + noise * 0.6f);

                    // 轮廓
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        c *= 0.5f;

                    // 眼睛区域（上半部分两个亮点）
                    if (y > height * 0.6f && y < height * 0.8f)
                    {
                        if ((x == width / 3 || x == width * 2 / 3) && y == (int)(height * 0.7f))
                            c = new Color(1f, 0.8f, 0f); // 黄色眼睛
                    }

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 16f);
        }
    }
}
