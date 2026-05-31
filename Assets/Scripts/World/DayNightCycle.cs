using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 昼夜循环系统
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("颜色设置")]
        public Color dayAmbient = new Color(0.7f, 0.55f, 0.4f);
        public Color nightAmbient = new Color(0.12f, 0.08f, 0.18f);
        public Color dawnColor = new Color(0.55f, 0.3f, 0.2f);
        public Color duskColor = new Color(0.45f, 0.2f, 0.35f);

        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            float progress = GameManager.Instance.DayProgress;

            Color ambient = CalculateAmbientColor(progress);
            if (_mainCam != null)
                _mainCam.backgroundColor = ambient * 0.3f;

            RenderSettings.ambientLight = ambient;
        }

        private Color CalculateAmbientColor(float progress)
        {
            // 0.0 = 午夜, 0.25 = 日出, 0.5 = 正午, 0.75 = 日落
            if (progress < 0.19f) // 夜晚
                return nightAmbient;
            else if (progress < 0.25f) // 黎明
                return Color.Lerp(nightAmbient, dawnColor, (progress - 0.19f) / 0.06f);
            else if (progress < 0.30f) // 清晨
                return Color.Lerp(dawnColor, dayAmbient, (progress - 0.25f) / 0.05f);
            else if (progress < 0.75f) // 白天
                return dayAmbient;
            else if (progress < 0.81f) // 黄昏
                return Color.Lerp(dayAmbient, duskColor, (progress - 0.75f) / 0.06f);
            else if (progress < 0.87f) // 暮色
                return Color.Lerp(duskColor, nightAmbient, (progress - 0.81f) / 0.06f);
            else // 夜晚
                return nightAmbient;
        }

        /// <summary>
        /// 获取当前时间的光照乘数（影响地表光照强度）
        /// </summary>
        public float GetSunlightMultiplier()
        {
            if (GameManager.Instance == null) return 1f;
            float progress = GameManager.Instance.DayProgress;

            if (progress >= 0.25f && progress < 0.75f)
                return 1f; // 白天满光照
            else if (progress >= 0.19f && progress < 0.25f)
                return Mathf.Lerp(0.3f, 1f, (progress - 0.19f) / 0.06f);
            else if (progress >= 0.75f && progress < 0.81f)
                return Mathf.Lerp(1f, 0.3f, (progress - 0.75f) / 0.06f);
            else
                return 0.3f; // 夜晚低光照
        }

        public string GetTimeString()
        {
            if (GameManager.Instance == null) return "00:00";
            float progress = GameManager.Instance.DayProgress;
            int hours = Mathf.FloorToInt(progress * 24f) % 24;
            int minutes = Mathf.FloorToInt((progress * 24f - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }
    }
}
