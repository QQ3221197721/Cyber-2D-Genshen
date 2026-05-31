using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 粒子效果管理器
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        private void Awake() { Instance = this; }

        /// <summary>
        /// 方块破碎粒子
        /// </summary>
        public void SpawnBreakParticles(Vector2 position, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject p = new GameObject("Particle");
                p.transform.position = position;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePixelSprite(color);
                sr.sortingLayerName = "Effects";
                sr.sortingOrder = 100;

                var rb = p.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2f;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float force = Random.Range(3f, 7f);
                rb.velocity = new Vector2(Mathf.Cos(angle) * force, Mathf.Sin(angle) * force + 2f);

                Destroy(p, Random.Range(0.5f, 1.2f));
            }
        }

        /// <summary>
        /// 伤害数字弹出 - 带浮动动画和淡出效果
        /// </summary>
        public void SpawnDamageNumber(Vector2 position, int damage, Color color)
        {
            var go = new GameObject("DmgNumber");
            go.transform.position = (Vector3)position + Vector3.up * 0.5f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateDamageNumberSprite(damage, color);
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 1000;

            // 添加向上飘动+淡出
            var floater = go.AddComponent<FloatingText>();
            floater.Init(color);
        }

        /// <summary>
        /// 命中特效（不同伤害类型不同颜色爆发）
        /// </summary>
        public void SpawnHitEffect(Vector2 position, DamageType type)
        {
            Color effectColor = type switch
            {
                DamageType.Fire => new Color(1f, 0.4f, 0f),
                DamageType.Ice => new Color(0.4f, 0.9f, 1f),
                DamageType.Electric => new Color(1f, 1f, 0.2f),
                DamageType.Energy => new Color(0.6f, 0.2f, 1f),
                _ => Color.white
            };

            // 生成小型爆发粒子
            for (int i = 0; i < 5; i++)
            {
                GameObject p = new GameObject("HitFX");
                p.transform.position = position;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePixelSprite(effectColor);
                sr.sortingLayerName = "Effects";
                sr.sortingOrder = 99;

                var rb = p.AddComponent<Rigidbody2D>();
                rb.gravityScale = 1f;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float force = Random.Range(2f, 5f);
                rb.velocity = new Vector2(Mathf.Cos(angle) * force, Mathf.Sin(angle) * force + 1.5f);

                Destroy(p, Random.Range(0.3f, 0.7f));
            }
        }

        /// <summary>
        /// 生成伤害数字精灵 - 用彩色像素方块表示数字位数
        /// </summary>
        private Sprite GenerateDamageNumberSprite(int damage, Color color)
        {
            // 根据伤害数字位数生成对应宽度的彩色条
            string numStr = damage.ToString();
            int digitCount = numStr.Length;
            int width = digitCount * 4 + 2;
            int height = 6;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // 清空
            Color clear = Color.clear;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, clear);

            // 为每个数字位绘制一个小方块
            for (int d = 0; d < digitCount; d++)
            {
                int xStart = d * 4 + 1;
                Color blockColor = Color.Lerp(color, Color.white, 0.3f);
                for (int x = xStart; x < xStart + 3; x++)
                    for (int y = 1; y < height - 1; y++)
                        tex.SetPixel(x, y, blockColor);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), Vector2.one * 0.5f, 8f);
        }

        private Sprite CreatePixelSprite(Color color)
        {
            Texture2D tex = new Texture2D(3, 3, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 3, 3), Vector2.one * 0.5f, 16f);
        }
    }

    /// <summary>
    /// 浮动文字组件 - 向上飘动并淡出
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private float _lifetime = 1f;
        private float _elapsed;
        private SpriteRenderer _sr;

        public void Init(Color c)
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * 2f * Time.deltaTime;
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 1f - (_elapsed / _lifetime);
                _sr.color = c;
            }
            if (_elapsed >= _lifetime) Destroy(gameObject);
        }
    }
}
