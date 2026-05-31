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
        /// 伤害数字弹出
        /// </summary>
        public void SpawnDamageNumber(Vector2 position, int damage, Color color)
        {
            // 简化版 - 使用SpriteRenderer暂代
            GameObject dmgObj = new GameObject("DmgNum");
            dmgObj.transform.position = position + Vector2.up * 0.5f;

            var rb = dmgObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.5f;
            rb.velocity = new Vector2(Random.Range(-1f, 1f), 3f);

            Destroy(dmgObj, 1f);
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
}
