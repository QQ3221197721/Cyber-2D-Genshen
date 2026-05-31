using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    public class IntroVideoPlayer : MonoBehaviour
    {
        public static IntroVideoPlayer Instance { get; private set; }
        
        private VideoPlayer _videoPlayer;
        private bool _videoFinished = false;
        private bool _skipped = false;
        private bool _isPlaying = false;
        private float _skipHintTimer = 0f;
        
        // 当视频结束或跳过时触发
        public System.Action OnIntroComplete;
        
        void Awake()
        {
            Instance = this;
        }
        
        public void PlayIntro()
        {
            _isPlaying = true;
            _videoFinished = false;
            _skipped = false;
            
            // 确保有可用的相机
            if (Camera.main == null)
            {
                var camObj = new GameObject("IntroCamera");
                var cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.backgroundColor = Color.black;
            }
            
            // 检查视频文件是否存在
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "IntroVideo.mp4");
            if (!System.IO.File.Exists(videoPath))
            {
                Debug.LogWarning("[IntroVideoPlayer] 视频文件不存在，跳过开场动画: " + videoPath);
                CompleteIntro();
                return;
            }
            
            // 创建 VideoPlayer 组件
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _videoPlayer.targetCamera = Camera.main;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
            _videoPlayer.isLooping = false;
            
            // 设置视频源
            _videoPlayer.url = videoPath;
            
            // 注册结束回调
            _videoPlayer.loopPointReached += OnVideoEnd;
            
            // 注册错误回调，防止加载失败卡死
            _videoPlayer.errorReceived += OnVideoError;
            
            // 准备并播放
            _videoPlayer.prepareCompleted += (source) => { _videoPlayer.Play(); };
            _videoPlayer.Prepare();
        }
        
        void Update()
        {
            if (!_isPlaying) return;
            
            _skipHintTimer += Time.deltaTime;
            
            // 任意键跳过（等待0.5秒后才允许跳过，防止误触）
            if (_skipHintTimer > 0.5f && Input.anyKeyDown)
            {
                SkipVideo();
            }
        }
        
        private void SkipVideo()
        {
            if (_skipped) return;
            _skipped = true;
            _isPlaying = false;
            
            CleanupVideoPlayer();
            CompleteIntro();
        }
        
        private void OnVideoEnd(VideoPlayer vp)
        {
            if (_videoFinished) return;
            _videoFinished = true;
            _isPlaying = false;
            
            CleanupVideoPlayer();
            CompleteIntro();
        }
        
        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogWarning("[IntroVideoPlayer] 视频播放错误，跳过开场动画: " + message);
            _isPlaying = false;
            
            CleanupVideoPlayer();
            CompleteIntro();
        }
        
        private void CleanupVideoPlayer()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                Destroy(_videoPlayer);
                _videoPlayer = null;
            }
        }
        
        private void CompleteIntro()
        {
            OnIntroComplete?.Invoke();
        }
        
        void OnGUI()
        {
            if (!_isPlaying) return;
            
            // 右下角显示"按任意键跳过"提示
            if (_skipHintTimer > 1.5f) // 1.5秒后显示跳过提示
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 16;
                style.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                style.alignment = TextAnchor.LowerRight;
                
                float alpha = Mathf.PingPong(Time.time * 2f, 1f) * 0.5f + 0.5f;
                style.normal.textColor = new Color(1f, 1f, 1f, alpha);
                
                Rect rect = new Rect(Screen.width - 220, Screen.height - 50, 200, 30);
                GUI.Label(rect, "按任意键跳过 >>", style);
            }
        }
        
        public bool IsPlaying => _isPlaying;
    }
}
