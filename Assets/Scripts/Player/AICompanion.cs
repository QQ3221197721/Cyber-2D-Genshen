using UnityEngine;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// AI机器人伙伴 QR-7
    /// 跟随玩家、新手引导对话气泡、战斗辅助激光、量子雷达检测裂缝
    /// </summary>
    public class AICompanion : MonoBehaviour
    {
        public static AICompanion Instance { get; private set; }

        // === 跟随参数 ===
        private float _followDistance = 2.5f;
        private float _followSpeed = 5f;
        private float _floatAmplitude = 0.15f;
        private float _floatFrequency = 2f;

        // === 战斗参数 ===
        private float _attackInterval = 2f;
        private float _attackRange = 8f;
        private float _attackDamage = 3f;
        private float _lastAttackTime;

        // === 量子雷达 ===
        private float _radarInterval = 30f;
        private float _radarRange = 200;
        private float _lastRadarScan;
        private Vector2Int? _detectedRift = null;
        private float _alertTimer = 0f;

        // === 引导对话 ===
        private bool _hasGreeted = false;
        private bool _hasMineTip = false;
        private bool _hasCombatTip = false;
        private bool _hasRiftTip = false;
        private bool _hasLowHpTip = false;

        private string _currentDialogue = "";
        private float _dialogueTimer = 0f;

        // === 组件 ===
        private SpriteRenderer _sr;
        private Transform _playerTransform;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (PlayerController.Instance != null)
                _playerTransform = PlayerController.Instance.transform;

            _lastRadarScan = -25f; // 5秒后首次扫描

            // 初始问候（延迟2秒）
            Invoke(nameof(ShowGreeting), 2f);
        }

        void Update()
        {
            if (_playerTransform == null)
            {
                if (PlayerController.Instance != null)
                    _playerTransform = PlayerController.Instance.transform;
                return;
            }

            FollowPlayer();
            FloatAnimation();
            UpdateCombat();
            UpdateRadar();
            UpdateDialogue();
            CheckTriggers();
        }

        // === 跟随逻辑 ===
        private void FollowPlayer()
        {
            Vector2 targetPos = (Vector2)_playerTransform.position + new Vector2(-_followDistance * GetPlayerDirection(), 0.5f);
            Vector2 currentPos = transform.position;

            float dist = Vector2.Distance(currentPos, targetPos);

            if (dist > 0.5f)
            {
                float speed = dist > 5f ? _followSpeed * 2f : _followSpeed;
                transform.position = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
            }

            // 如果离太远直接传送
            if (dist > 15f)
            {
                transform.position = targetPos;
            }

            // 面向与玩家相同方向
            float scaleX = _playerTransform.localScale.x > 0 ? 1 : -1;
            transform.localScale = new Vector3(scaleX, 1, 1);
        }

        private float GetPlayerDirection()
        {
            return _playerTransform.localScale.x > 0 ? 1f : -1f;
        }

        // === 悬浮动画 ===
        private void FloatAnimation()
        {
            float floatOffset = Mathf.Sin(Time.time * _floatFrequency) * _floatAmplitude;
            Vector3 pos = transform.position;
            pos.y += floatOffset * Time.deltaTime;
            transform.position = pos;
        }

        // === 战斗辅助 ===
        private void UpdateCombat()
        {
            if (Time.time - _lastAttackTime < _attackInterval) return;

            // 查找最近敌人
            var enemyBases = FindObjectsOfType<EnemyBase>();
            if (enemyBases.Length == 0) return;

            float closestDist = float.MaxValue;
            EnemyBase closest = null;

            foreach (var enemy in enemyBases)
            {
                if (!enemy.IsAlive) continue;
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < _attackRange && dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            if (closest != null)
            {
                // 发射激光攻击
                ShootLaser(closest);
                _lastAttackTime = Time.time;

                // 首次遇敌提示
                if (!_hasCombatTip)
                {
                    _hasCombatTip = true;
                    ShowDialogue("警告：检测到敌对目标。按左键攻击。", 3f);
                }
            }
        }

        private void ShootLaser(EnemyBase target)
        {
            // 计算击退方向：从伙伴指向敌人
            Vector2 knockbackDir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

            // 对敌人造成伤害
            target.TakeDamage(Mathf.RoundToInt(_attackDamage), knockbackDir);

            // 激光视觉效果
            Debug.DrawLine(transform.position, target.transform.position, Color.cyan, 0.2f);

            // 粒子效果
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SpawnBreakParticles(
                    target.transform.position, new Color(0f, 0.9f, 1f, 1f));
            }
        }

        // === 量子雷达 ===
        private void UpdateRadar()
        {
            if (Time.time - _lastRadarScan < _radarInterval) return;
            _lastRadarScan = Time.time;

            // 获取玩家Tile坐标
            int playerTileX = Mathf.FloorToInt(_playerTransform.position.x);
            int playerTileY = Mathf.FloorToInt(-_playerTransform.position.y);

            // 调用DungeonRiftSystem查找裂缝
            if (DungeonRiftSystem.Instance != null)
            {
                _detectedRift = DungeonRiftSystem.Instance.FindNearestRift(
                    playerTileX, playerTileY, (int)_radarRange);

                if (_detectedRift.HasValue)
                {
                    _alertTimer = 5f;

                    if (!_hasRiftTip)
                    {
                        _hasRiftTip = true;
                        ShowDialogue("量子雷达检测到不稳定裂缝！靠近按E键可进入副本。", 4f);
                    }
                }
            }
        }

        // === 引导对话系统 ===
        private void CheckTriggers()
        {
            // 低血量提示
            if (!_hasLowHpTip && PlayerStats.Instance != null)
            {
                float hpRatio = (float)PlayerStats.Instance.currentHealth / PlayerStats.Instance.maxHealth;
                if (hpRatio < 0.3f)
                {
                    _hasLowHpTip = true;
                    ShowDialogue("生命值过低，建议撤退并使用回复物品。", 3f);
                }
            }
        }

        private void ShowGreeting()
        {
            if (!_hasGreeted)
            {
                _hasGreeted = true;
                ShowDialogue("检测到生命体...系统初始化完毕。我是QR-7，你的AI伙伴。", 4f);
            }
        }

        /// <summary>
        /// 供外部调用的引导提示 - 玩家挖矿时触发
        /// </summary>
        public void OnPlayerMined()
        {
            if (!_hasMineTip)
            {
                _hasMineTip = true;
                ShowDialogue("建议收集资源，按Tab键可查看可合成物品。", 3f);
            }
        }

        public void ShowDialogue(string text, float duration)
        {
            _currentDialogue = text;
            _dialogueTimer = duration;
        }

        private void UpdateDialogue()
        {
            if (_dialogueTimer > 0)
            {
                _dialogueTimer -= Time.deltaTime;
                if (_dialogueTimer <= 0)
                {
                    _currentDialogue = "";
                }
            }

            if (_alertTimer > 0)
                _alertTimer -= Time.deltaTime;
        }

        // === UI绘制（对话气泡+雷达警报）===
        void OnGUI()
        {
            if (Camera.main == null) return;

            // 对话气泡
            if (!string.IsNullOrEmpty(_currentDialogue))
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
                if (screenPos.z > 0)
                {
                    float width = _currentDialogue.Length * 14f + 20f;
                    width = Mathf.Min(width, 350f);
                    float height = 30f;
                    float x = screenPos.x - width / 2;
                    float y = Screen.height - screenPos.y - height;

                    // 背景
                    GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
                    GUI.DrawTexture(new Rect(x - 2, y - 2, width + 4, height + 4), Texture2D.whiteTexture);

                    // 文字
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 12;
                    style.normal.textColor = new Color(0f, 0.9f, 0.8f);
                    style.wordWrap = true;

                    GUI.color = Color.white;
                    GUI.Label(new Rect(x, y, width, height), _currentDialogue, style);
                }
            }

            // 雷达警报指示
            if (_alertTimer > 0 && _detectedRift.HasValue && _playerTransform != null)
            {
                GUIStyle alertStyle = new GUIStyle(GUI.skin.label);
                alertStyle.fontSize = 14;
                alertStyle.normal.textColor = new Color(0.9f, 0.3f, 1f);
                alertStyle.alignment = TextAnchor.MiddleCenter;

                // 计算方向
                int dx = _detectedRift.Value.x - Mathf.FloorToInt(_playerTransform.position.x);
                int dy = _detectedRift.Value.y - Mathf.FloorToInt(-_playerTransform.position.y);
                string direction = "";
                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                    direction = dx > 0 ? "→" : "←";
                else
                    direction = dy > 0 ? "↓" : "↑";

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                string alertText = $"⚠ 裂缝信号 {direction} {dist:F0}格";

                GUI.Label(new Rect(Screen.width / 2 - 100, 60, 200, 30), alertText, alertStyle);
            }

            // 头顶 "!" 标记
            if (_alertTimer > 0)
            {
                Vector3 headPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
                if (headPos.z > 0)
                {
                    GUIStyle excl = new GUIStyle(GUI.skin.label);
                    excl.fontSize = 20;
                    excl.normal.textColor = new Color(1f, 0.8f, 0f);
                    excl.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(headPos.x - 15, Screen.height - headPos.y - 15, 30, 30), "!", excl);
                }
            }
        }

        // === 精灵生成（白色机器人 QR-7）===
        public static Sprite GenerateCompanionSprite()
        {
            Texture2D tex = new Texture2D(14, 22, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0, 0, 0, 0);
            Color white = new Color(0.92f, 0.92f, 0.95f);
            Color darkGray = new Color(0.3f, 0.3f, 0.35f);
            Color orange = new Color(0.95f, 0.6f, 0.1f);
            Color pink = new Color(0.9f, 0.4f, 0.5f);
            Color lightGray = new Color(0.75f, 0.75f, 0.8f);

            // 清空
            for (int y = 0; y < 22; y++)
                for (int x = 0; x < 14; x++)
                    tex.SetPixel(x, y, clear);

            // 脚部 y=0-2 (深灰关节)
            for (int y = 0; y <= 2; y++)
            {
                for (int x = 4; x <= 6; x++) tex.SetPixel(x, y, darkGray);
                for (int x = 7; x <= 9; x++) tex.SetPixel(x, y, darkGray);
            }

            // 小腿 y=3-5 (白色)
            for (int y = 3; y <= 5; y++)
            {
                for (int x = 4; x <= 5; x++) tex.SetPixel(x, y, white);
                for (int x = 8; x <= 9; x++) tex.SetPixel(x, y, white);
            }

            // 膝关节 y=6 (深灰)
            for (int x = 4; x <= 5; x++) tex.SetPixel(x, 6, darkGray);
            for (int x = 8; x <= 9; x++) tex.SetPixel(x, 6, darkGray);

            // 大腿 y=7-9 (白色)
            for (int y = 7; y <= 9; y++)
                for (int x = 4; x <= 9; x++) tex.SetPixel(x, y, white);

            // 腰部关节 y=10 (深灰)
            for (int x = 4; x <= 9; x++) tex.SetPixel(x, 10, darkGray);

            // 躯干 y=11-15 (白色主体)
            for (int y = 11; y <= 15; y++)
                for (int x = 3; x <= 10; x++) tex.SetPixel(x, y, white);

            // 胸口粉色指示灯 y=12-13
            tex.SetPixel(6, 13, pink);
            tex.SetPixel(7, 13, pink);
            tex.SetPixel(6, 12, pink);
            tex.SetPixel(7, 12, pink);

            // 肩部 y=16 (浅灰)
            for (int x = 2; x <= 11; x++) tex.SetPixel(x, 16, lightGray);

            // 手臂 y=11-15 (白色+关节)
            for (int y = 11; y <= 15; y++)
            {
                tex.SetPixel(2, y, white);
                tex.SetPixel(11, y, white);
            }
            tex.SetPixel(2, 13, darkGray); // 肘关节
            tex.SetPixel(11, 13, darkGray);

            // 脖子 y=17 (深灰)
            for (int x = 5; x <= 8; x++) tex.SetPixel(x, 17, darkGray);

            // 头部 y=18-21 (白色圆头)
            for (int y = 18; y <= 21; y++)
                for (int x = 4; x <= 9; x++) tex.SetPixel(x, y, white);
            // 头部两侧扩展
            for (int y = 19; y <= 20; y++)
            {
                tex.SetPixel(3, y, white);
                tex.SetPixel(10, y, white);
            }

            // 面罩（橙色护目镜）y=19-20
            for (int x = 5; x <= 8; x++)
            {
                tex.SetPixel(x, 19, orange);
                tex.SetPixel(x, 20, orange);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 14, 22), new Vector2(0.5f, 0.5f), 14f);
        }
    }
}
