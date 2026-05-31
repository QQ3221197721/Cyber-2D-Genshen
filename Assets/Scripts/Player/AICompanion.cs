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

        // === 自定义命名 ===
        public string CompanionName { get; private set; } = "QR-7";
        /// <summary>命名UI是否正在显示（外部可读取以暂停玩家输入）</summary>
        public bool IsNamingUIActive => _showNamingUI;

        private bool _hasBeenNamed = false;
        private bool _showNamingUI = false;
        private string _nameInput = "QR-7";
        private float _greetTimer = 0f;
        private Texture2D _namingBgTexture;

        public void SetName(string newName)
        {
            if (!string.IsNullOrEmpty(newName) && newName.Length <= 12)
            {
                CompanionName = newName;
            }
        }

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

        // === 废墟修复系统 ===
        private bool _isRepairing = false;
        private float _repairTimer = 0f;
        private const float RepairDuration = 3f;
        private Vector2Int _repairTarget;
        private bool _nearLuckyRuin = false;

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
            CheckNearbyLuckyRuin();
            UpdateRepair();

            // 命名UI计时：问候后5秒弹出
            if (_hasGreeted && !_hasBeenNamed)
            {
                _greetTimer += Time.deltaTime;
                if (_greetTimer > 5f)
                {
                    _showNamingUI = true;
                }
            }

            // 按N键随时重命名
            if (Input.GetKeyDown(KeyCode.N) && !_showNamingUI && !_isRepairing)
            {
                _showNamingUI = true;
                _nameInput = CompanionName;
            }
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
                ShowDialogue($"检测到生命体...系统初始化完毕。我是{CompanionName}，你的AI伙伴。", 4f);
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

        // === 废墟修复系统 ===

        private void CheckNearbyLuckyRuin()
        {
            if (_isRepairing) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 pos = player.transform.position;
            int px = Mathf.RoundToInt(pos.x);
            int py = Mathf.RoundToInt(-pos.y);

            _nearLuckyRuin = false;
            var gm = GameManager.Instance;
            if (gm == null) return;

            for (int dx = -3; dx <= 3 && !_nearLuckyRuin; dx++)
            {
                for (int dy = -3; dy <= 3 && !_nearLuckyRuin; dy++)
                {
                    int tx = px + dx;
                    int ty = py + dy;
                    if (tx < 0 || tx >= gm.worldWidth || ty < 0 || ty >= gm.worldHeight) continue;

                    if (gm.GetTile(tx, ty) == TileType.LuckyRuin)
                    {
                        _repairTarget = new Vector2Int(tx, ty);
                        _nearLuckyRuin = true;
                    }
                }
            }
        }

        private void UpdateRepair()
        {
            if (_isRepairing)
            {
                _repairTimer -= Time.deltaTime;
                if (_repairTimer <= 0f)
                {
                    // 修复完成
                    _isRepairing = false;
                    if (LuckyBlockSystem.Instance != null)
                    {
                        LuckyBlockSystem.Instance.OnRuinRepaired(_repairTarget.x, _repairTarget.y);
                    }
                }
                return;
            }

            if (_nearLuckyRuin && Input.GetKeyDown(KeyCode.R))
            {
                StartRepair();
            }
        }

        private void StartRepair()
        {
            _isRepairing = true;
            _repairTimer = RepairDuration;
            ShowDialogue($"{CompanionName}正在修复遗迹...请稍候", RepairDuration);
        }

        // === UI绘制（对话气泡+雷达警报+修复提示+命名界面）===
        void OnGUI()
        {
            if (Camera.main == null) return;

            // 初始化半透明背景纹理（只创建一次）
            if (_namingBgTexture == null)
            {
                _namingBgTexture = new Texture2D(1, 1);
                _namingBgTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.08f, 0.92f));
                _namingBgTexture.Apply();
            }

            // 伙伴头顶名字标签（始终显示）
            Vector3 namePos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.8f);
            if (namePos.z > 0)
            {
                float nameY = Screen.height - namePos.y;
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.fontSize = 11;
                nameStyle.normal.textColor = new Color(0.4f, 1f, 0.9f, 0.8f);
                nameStyle.alignment = TextAnchor.MiddleCenter;

                float nameWidth = CompanionName.Length * 12f + 10f;
                GUI.Label(new Rect(namePos.x - nameWidth / 2, nameY - 20, nameWidth, 20), CompanionName, nameStyle);
            }

            // 命名界面
            if (_showNamingUI)
            {
                DrawNamingUI();
            }

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

            // 废墟修复提示
            if (_nearLuckyRuin && !_isRepairing)
            {
                GUIStyle tipStyle = new GUIStyle(GUI.skin.label);
                tipStyle.fontSize = 13;
                tipStyle.normal.textColor = new Color(0.7f, 0.5f, 0.9f);
                tipStyle.alignment = TextAnchor.MiddleCenter;
                tipStyle.wordWrap = true;

                string tip = $"按R让{CompanionName}修复废墟（获得永久加成）\n或直接摧毁（获得随机物品）";
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 80, 300, 50), tip, tipStyle);
            }

            // 修复进度条
            if (_isRepairing)
            {
                float progress = 1f - (_repairTimer / RepairDuration);
                float barWidth = 200f;
                float barHeight = 20f;
                float barX = Screen.width / 2 - barWidth / 2;
                float barY = Screen.height - 70;

                // 背景
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
                GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

                // 进度
                GUI.color = new Color(0.3f, 0.9f, 0.7f, 0.9f);
                GUI.DrawTexture(new Rect(barX, barY, barWidth * progress, barHeight), Texture2D.whiteTexture);

                // 文字
                GUI.color = Color.white;
                GUIStyle barStyle = new GUIStyle(GUI.skin.label);
                barStyle.alignment = TextAnchor.MiddleCenter;
                barStyle.fontSize = 12;
                barStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(barX, barY, barWidth, barHeight), $"修复中... {progress * 100:F0}%", barStyle);
            }
        }

        // === 命名界面绘制 ===
        private void DrawNamingUI()
        {
            float w = 300, h = 160;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            // 半透明黑色背景
            GUI.DrawTexture(new Rect(x, y, w, h), _namingBgTexture);

            // 霓虹边框
            GUI.color = new Color(0f, 0.9f, 0.8f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, w, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 2, w, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 2, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 2, y, 2, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 标题
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = new Color(0f, 1f, 0.85f);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(x + 20, y + 15, w - 40, 25), "给你的AI伙伴取个名字吧！", titleStyle);

            // 输入框
            GUIStyle inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = 14;
            inputStyle.normal.textColor = Color.white;
            inputStyle.alignment = TextAnchor.MiddleCenter;
            _nameInput = GUI.TextField(new Rect(x + 30, y + 55, w - 60, 30), _nameInput, 12, inputStyle);

            // 字符数提示
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 10;
            hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.6f);
            hintStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x + 30, y + 87, w - 60, 16), $"{_nameInput.Length}/12", hintStyle);

            // 确认按钮
            bool canConfirm = !string.IsNullOrEmpty(_nameInput);
            GUI.enabled = canConfirm;
            if (GUI.Button(new Rect(x + 45, y + 112, 95, 32), "确认"))
            {
                SetName(_nameInput);
                _hasBeenNamed = true;
                _showNamingUI = false;
                ShowDialogue($"从现在起，叫我{CompanionName}就好！", 3f);
            }
            GUI.enabled = true;

            // 保持默认按钮
            if (GUI.Button(new Rect(x + 160, y + 112, 95, 32), "保持默认"))
            {
                _hasBeenNamed = true;
                _showNamingUI = false;
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
