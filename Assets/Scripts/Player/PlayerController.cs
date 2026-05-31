using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家控制器 - 2D平台跳跃，参考Terraria手感
    /// 特性: 土狼时间、跳跃缓冲、墙壁滑行、梯子攀爬
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("移动")]
        public float moveSpeed = 6f;
        public float acceleration = 40f;
        public float deceleration = 30f;
        public float airAcceleration = 25f;

        [Header("跳跃")]
        public float jumpForce = 12f;
        public float fallMultiplier = 2.5f;
        public float lowJumpMultiplier = 2f;
        public int maxJumps = 1;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.1f;

        [Header("自动踏步")]
        public float stepCheckDist = 0.7f;
        public float stepHeight = 1.05f;
        public float stepCooldown = 0.15f;

        [Header("检测")]
        public LayerMask groundLayer;
        public Vector2 groundCheckSize = new Vector2(1.1f, 0.3f);
        public float groundCheckOffset = 0.05f;

        [Header("缩放")]
        public float baseScale = 2f; // 基础缩放值，用于翻转时保持正确比例

        [Header("三视图精灵")]
        public Sprite spriteFront;
        public Sprite spriteSide;
        public Sprite spriteBack;

        // 状态
        public bool IsGrounded { get; private set; }
        public bool IsFacing { get; private set; } = true; // true=右
        public Vector2 Velocity => _rb.velocity;
        public bool InputEnabled { get; set; } = true;

        private Rigidbody2D _rb;
        private BoxCollider2D _col;
        private SpriteRenderer _sr;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;
        private int _jumpsRemaining;
        private float _moveInput;
        private bool _jumpInput;
        private bool _jumpHeld;
        private float _stepCooldownTimer;

        private void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            _rb.freezeRotation = true;
            _rb.gravityScale = 3f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            if (!InputEnabled || (AICompanion.Instance != null && AICompanion.Instance.IsNamingUIActive))
            {
                _moveInput = 0;
                return;
            }

            // 输入收集
            _moveInput = Input.GetAxisRaw("Horizontal");
            if (Input.GetKeyDown(KeyCode.Space)) _jumpBufferCounter = jumpBufferTime;
            _jumpHeld = Input.GetKey(KeyCode.Space);

            // 地面检测
            Vector2 checkPos = (Vector2)transform.position + Vector2.up * groundCheckOffset;
            IsGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);

            // 土狼时间
            if (IsGrounded)
            {
                _coyoteTimeCounter = coyoteTime;
                _jumpsRemaining = maxJumps;
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }

            // 跳跃缓冲
            _jumpBufferCounter -= Time.deltaTime;

            // 跳跃执行
            if (_jumpBufferCounter > 0f && (_coyoteTimeCounter > 0f || _jumpsRemaining > 0))
            {
                ExecuteJump();
            }

            // 朝向
            if (_moveInput > 0.1f) IsFacing = true;
            else if (_moveInput < -0.1f) IsFacing = false;

            // 教程追踪：移动
            if (Mathf.Abs(_moveInput) > 0.1f && TutorialSystem.Instance != null)
                TutorialSystem.Instance.HasMoved = true;

            // 精灵视图切换
            UpdateSpriteView();

            // 翻转（基于baseScale，而不是固定的1）
            transform.localScale = new Vector3(IsFacing ? baseScale : -baseScale, baseScale, 1f);
        }

        private void FixedUpdate()
        {
            // 水平移动
            float targetSpeed = _moveInput * moveSpeed;
            float accel = IsGrounded ? acceleration : airAcceleration;
            float speedDiff = targetSpeed - _rb.velocity.x;

            if (Mathf.Abs(_moveInput) < 0.1f)
                accel = IsGrounded ? deceleration : airAcceleration * 0.5f;

            _rb.velocity += Vector2.right * speedDiff * accel * Time.fixedDeltaTime;

            // 改善跳跃手感
            if (_rb.velocity.y < 0)
            {
                _rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (_rb.velocity.y > 0 && !_jumpHeld)
            {
                _rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }

            // 限速
            float maxFall = 25f;
            if (_rb.velocity.y < -maxFall)
                _rb.velocity = new Vector2(_rb.velocity.x, -maxFall);

            // 自动踏步
            TryAutoStep();
        }

        /// <summary>
        /// 自动踏步: 前方有1格高障碍且上方畅通时平滑抬升玩家
        /// </summary>
        private void TryAutoStep()
        {
            // 冷却计时
            _stepCooldownTimer -= Time.fixedDeltaTime;
            if (_stepCooldownTimer > 0f) return;

            // 仅在地面且有水平输入时触发
            if (!IsGrounded || Mathf.Abs(_moveInput) < 0.1f) return;

            // 不在垂直移动中触发（避免干扰跳跃）
            if (Mathf.Abs(_rb.velocity.y) > 1f) return;

            float direction = Mathf.Sign(_moveInput);
            Vector2 footPos = (Vector2)transform.position + new Vector2(0, 0.1f);

            // 1. 检测脚部前方是否有障碍
            RaycastHit2D footHit = Physics2D.Raycast(footPos, Vector2.right * direction, stepCheckDist, groundLayer);
            if (footHit.collider == null) return;

            // 2. 从1格高处检测是否畅通（仅1格台阶）
            Vector2 stepPos = footPos + new Vector2(0, stepHeight);
            RaycastHit2D stepHit = Physics2D.Raycast(stepPos, Vector2.right * direction, stepCheckDist, groundLayer);
            if (stepHit.collider != null) return;

            // 3. 检查头顶是否有空间可供上升（碰撞体顶部在 position.y + 3.0 处）
            Vector2 headPos = (Vector2)transform.position + new Vector2(0, 3.0f);
            RaycastHit2D headHit = Physics2D.Raycast(headPos, Vector2.up, 0.5f, groundLayer);
            if (headHit.collider != null) return;

            // 4. 抬升玩家并重置冷却
            _rb.position += new Vector2(0, stepHeight);
            _rb.velocity = new Vector2(_rb.velocity.x, 0f); // 清除垂直速度避免弹跳
            _stepCooldownTimer = stepCooldown;
        }

        private void ExecuteJump()
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
            _jumpsRemaining--;

            // 教程追踪：跳跃
            if (TutorialSystem.Instance != null)
                TutorialSystem.Instance.HasJumped = true;
        }

        /// <summary>
        /// 根据移动状态切换正面/侧面/背面精灵
        /// </summary>
        private void UpdateSpriteView()
        {
            if (_sr == null) return;

            float verticalInput = Input.GetAxisRaw("Vertical");

            if (verticalInput > 0.1f && Mathf.Abs(_moveInput) < 0.1f)
            {
                // 按W/↑ 且无水平输入 → 背面
                if (spriteBack != null) _sr.sprite = spriteBack;
            }
            else if (Mathf.Abs(_moveInput) > 0.1f)
            {
                // 水平移动中 → 侧面
                if (spriteSide != null) _sr.sprite = spriteSide;
            }
            else
            {
                // 静止 → 正面
                if (spriteFront != null) _sr.sprite = spriteFront;
            }
        }

        /// <summary>
        /// 外部施加击退
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force)
        {
            _rb.velocity = direction.normalized * force;
        }

        /// <summary>
        /// 获取玩家世界坐标对应的Tile坐标
        /// </summary>
        public Vector2Int GetTilePosition()
        {
            return new Vector2Int(
                Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(-transform.position.y)
            );
        }
    }
}
