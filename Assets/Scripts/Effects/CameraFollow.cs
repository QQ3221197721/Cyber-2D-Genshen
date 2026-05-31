using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 摄像机跟随 + 震动效果
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随")]
        public Transform target;
        public float smoothSpeed = 5f;
        public Vector3 offset = new Vector3(0, 1, -10);

        [Header("边界")]
        public bool useBounds = true;
        public float minX = 0f;
        public float maxX = 420f;
        public float minY = -200f;
        public float maxY = 10f;

        private Vector3 _shakeOffset;
        private float _shakeDuration;
        private float _shakeIntensity;

        private void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerController.Instance != null)
                    target = PlayerController.Instance.transform;
                return;
            }

            // 目标位置
            Vector3 desiredPos = target.position + offset;

            // 平滑跟随
            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

            // 边界限制
            if (useBounds)
            {
                float halfH = Camera.main.orthographicSize;
                float halfW = halfH * Camera.main.aspect;
                smoothed.x = Mathf.Clamp(smoothed.x, minX + halfW, maxX - halfW);
                smoothed.y = Mathf.Clamp(smoothed.y, minY + halfH, maxY - halfH);
            }

            // 震动
            if (_shakeDuration > 0)
            {
                _shakeDuration -= Time.deltaTime;
                _shakeOffset = Random.insideUnitCircle * _shakeIntensity;
                _shakeIntensity *= 0.9f;
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }

            transform.position = smoothed + _shakeOffset;
        }

        /// <summary>
        /// 触发屏幕震动
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }
    }
}
