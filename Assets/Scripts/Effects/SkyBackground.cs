using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 赛博废土天空背景 - 程序化生成渐变天空纹理
    /// 根据昼夜循环实时切换色板，始终跟随相机位置
    /// </summary>
    public class SkyBackground : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Texture2D _skyTex;

        private const int TEX_WIDTH = 512;
        private const int TEX_HEIGHT = 256;

        private float _lastProgress = -1f;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();

            _skyTex = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TextureFormat.RGBA32, false);
            _skyTex.filterMode = FilterMode.Bilinear;
            _skyTex.wrapMode = TextureWrapMode.Clamp;

            UpdateSkyTexture(0.5f); // 初始化为白天
        }

        private void LateUpdate()
        {
            // 跟随相机位置
            if (Camera.main != null)
            {
                transform.position = new Vector3(
                    Camera.main.transform.position.x,
                    Camera.main.transform.position.y,
                    10f);
            }

            // 根据时间更新天空（每隔一定变化量才更新，节省性能）
            if (GameManager.Instance != null)
            {
                float progress = GameManager.Instance.DayProgress;
                if (Mathf.Abs(progress - _lastProgress) > 0.005f)
                {
                    UpdateSkyTexture(progress);
                    _lastProgress = progress;
                }
            }
        }

        private void UpdateSkyTexture(float timeProgress)
        {
            // 根据时段确定三个关键色（顶、中、底）
            Color topColor, midColor, botColor;
            GetSkyColors(timeProgress, out topColor, out midColor, out botColor);

            // 填充渐变纹理
            for (int y = 0; y < TEX_HEIGHT; y++)
            {
                float t = (float)y / TEX_HEIGHT; // 0=底部, 1=顶部
                Color rowColor;
                if (t < 0.5f)
                {
                    rowColor = Color.Lerp(botColor, midColor, t * 2f);
                }
                else
                {
                    rowColor = Color.Lerp(midColor, topColor, (t - 0.5f) * 2f);
                }

                for (int x = 0; x < TEX_WIDTH; x++)
                {
                    // 添加微弱噪声使天空不完全平滑
                    float noise = Mathf.PerlinNoise(x * 0.02f + timeProgress * 10f, y * 0.05f) * 0.03f;
                    Color pixel = new Color(
                        Mathf.Clamp01(rowColor.r + noise),
                        Mathf.Clamp01(rowColor.g + noise * 0.5f),
                        Mathf.Clamp01(rowColor.b + noise),
                        1f);
                    _skyTex.SetPixel(x, y, pixel);
                }
            }

            // 夜晚添加星点/卫星碎片
            if (timeProgress < 0.2f || timeProgress > 0.85f)
            {
                AddNightDetails(timeProgress);
            }

            _skyTex.Apply();

            // 更新Sprite
            _sr.sprite = Sprite.Create(
                _skyTex,
                new Rect(0, 0, TEX_WIDTH, TEX_HEIGHT),
                new Vector2(0.5f, 0.5f),
                4f); // pixelsPerUnit=4 -> 512px覆盖128个Unity单位宽度
        }

        private void GetSkyColors(float t, out Color top, out Color mid, out Color bot)
        {
            if (t >= 0.30f && t < 0.75f) // 白天
            {
                top = new Color(0.35f, 0.45f, 0.55f);
                mid = new Color(0.6f, 0.5f, 0.35f);
                bot = new Color(0.75f, 0.4f, 0.2f);
            }
            else if (t < 0.19f || t >= 0.87f) // 夜晚
            {
                top = new Color(0.03f, 0.02f, 0.08f);
                mid = new Color(0.08f, 0.04f, 0.15f);
                bot = new Color(0.15f, 0.06f, 0.1f);
            }
            else if (t >= 0.19f && t < 0.30f) // 黎明过渡
            {
                float blend = (t - 0.19f) / 0.11f;
                Color nightTop = new Color(0.03f, 0.02f, 0.08f);
                Color nightMid = new Color(0.08f, 0.04f, 0.15f);
                Color nightBot = new Color(0.15f, 0.06f, 0.1f);
                Color dawnTop = new Color(0.15f, 0.1f, 0.2f);
                Color dawnMid = new Color(0.5f, 0.3f, 0.2f);
                Color dawnBot = new Color(0.7f, 0.25f, 0.1f);
                Color dayTop = new Color(0.35f, 0.45f, 0.55f);
                Color dayMid = new Color(0.6f, 0.5f, 0.35f);
                Color dayBot = new Color(0.75f, 0.4f, 0.2f);

                if (blend < 0.5f)
                {
                    float b2 = blend * 2f;
                    top = Color.Lerp(nightTop, dawnTop, b2);
                    mid = Color.Lerp(nightMid, dawnMid, b2);
                    bot = Color.Lerp(nightBot, dawnBot, b2);
                }
                else
                {
                    float b2 = (blend - 0.5f) * 2f;
                    top = Color.Lerp(dawnTop, dayTop, b2);
                    mid = Color.Lerp(dawnMid, dayMid, b2);
                    bot = Color.Lerp(dawnBot, dayBot, b2);
                }
            }
            else // 黄昏过渡 (0.75-0.87)
            {
                float blend = (t - 0.75f) / 0.12f;
                Color dayTop = new Color(0.35f, 0.45f, 0.55f);
                Color dayMid = new Color(0.6f, 0.5f, 0.35f);
                Color dayBot = new Color(0.75f, 0.4f, 0.2f);
                Color duskTop = new Color(0.1f, 0.05f, 0.2f);
                Color duskMid = new Color(0.4f, 0.15f, 0.3f);
                Color duskBot = new Color(0.6f, 0.2f, 0.15f);
                Color nightTop = new Color(0.03f, 0.02f, 0.08f);
                Color nightMid = new Color(0.08f, 0.04f, 0.15f);
                Color nightBot = new Color(0.15f, 0.06f, 0.1f);

                if (blend < 0.5f)
                {
                    float b2 = blend * 2f;
                    top = Color.Lerp(dayTop, duskTop, b2);
                    mid = Color.Lerp(dayMid, duskMid, b2);
                    bot = Color.Lerp(dayBot, duskBot, b2);
                }
                else
                {
                    float b2 = (blend - 0.5f) * 2f;
                    top = Color.Lerp(duskTop, nightTop, b2);
                    mid = Color.Lerp(duskMid, nightMid, b2);
                    bot = Color.Lerp(duskBot, nightBot, b2);
                }
            }
        }

        private void AddNightDetails(float timeProgress)
        {
            // 使用固定种子确保星点位置稳定
            System.Random rng = new System.Random(42);
            for (int i = 0; i < 30; i++)
            {
                int x = rng.Next(0, TEX_WIDTH);
                int y = rng.Next(TEX_HEIGHT / 2, TEX_HEIGHT); // 只在上半部分

                // 随机颜色：白色、青色或粉色（卫星碎片/远处霓虹广告）
                Color starColor;
                int colorType = rng.Next(3);
                if (colorType == 0)
                    starColor = new Color(0.9f, 0.9f, 1.0f, 0.8f);
                else if (colorType == 1)
                    starColor = new Color(0.2f, 0.9f, 0.8f, 0.7f);
                else
                    starColor = new Color(0.9f, 0.3f, 0.7f, 0.6f);

                // 闪烁效果
                float flicker = Mathf.Sin(timeProgress * 100f + i * 1.7f) * 0.5f + 0.5f;
                starColor.a *= flicker;

                _skyTex.SetPixel(x, y, starColor);
            }
        }
    }
}
