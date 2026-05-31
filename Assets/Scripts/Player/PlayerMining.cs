using UnityEngine;

namespace CyberTerraria
{
    /// <summary>
    /// 玩家挖掘/放置系统
    /// </summary>
    public class PlayerMining : MonoBehaviour
    {
        [Header("设置")]
        public float miningRange = 5f;
        public float placingRange = 5f;
        public LayerMask tileLayer;

        private float _miningProgress;
        private Vector2Int _miningTarget = new Vector2Int(-1, -1);
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.isPaused) return;
            if (GameManager.Instance.isInventoryOpen) return;

            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int tilePos = new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(-mouseWorld.y));

            float dist = Vector2.Distance(transform.position, mouseWorld);

            // 左键: 挖掘/攻击
            if (Input.GetMouseButton(0) && dist <= miningRange)
            {
                HandleMining(tilePos);
            }
            else
            {
                _miningProgress = 0f;
                _miningTarget = new Vector2Int(-1, -1);
            }

            // 右键: 放置
            if (Input.GetMouseButtonDown(1) && dist <= placingRange)
            {
                HandlePlacing(tilePos);
            }
        }

        private void HandleMining(Vector2Int tilePos)
        {
            var gm = GameManager.Instance;
            TileType tile = gm.GetTile(tilePos.x, tilePos.y);

            if (tile == TileType.Air || tile == TileType.ReinforcedSteel) return;

            // 检查是否换了目标
            if (tilePos != _miningTarget)
            {
                _miningProgress = 0f;
                _miningTarget = tilePos;
            }

            // 获取工具镐力
            float pickPower = 1f;
            float miningSpeed = 1f;
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem != null && selectedItem.category == ItemCategory.Tool)
            {
                pickPower = selectedItem.pickPower;
                miningSpeed = selectedItem.miningSpeed;
            }

            // 检查镐力是否足够
            var tileProps = TileRegistry.Get(tile);
            if (tileProps == null) return;

            // 进度累积
            float speedMult = PlayerStats.Instance != null ? PlayerStats.Instance.miningSpeedMultiplier : 1f;
            _miningProgress += Time.deltaTime * miningSpeed * speedMult * pickPower;

            // 达到硬度阈值 -> 破坏
            float requiredTime = tileProps.hardness * 0.15f / pickPower;
            if (_miningProgress >= requiredTime)
            {
                BreakTile(tilePos, tile, tileProps);
                _miningProgress = 0f;
            }
        }

        private void BreakTile(Vector2Int pos, TileType tile, TileProperties props)
        {
            var gm = GameManager.Instance;

            // 设置为空气
            gm.SetTile(pos.x, pos.y, TileType.Air);

            // 通知AI伙伴
            if (AICompanion.Instance != null)
                AICompanion.Instance.OnPlayerMined();

            // 掉落物品
            if (props.dropItemId > 0)
            {
                SpawnItemDrop(pos, props.dropItemId);
            }

            // 更新光照
            var lighting = FindObjectOfType<LightingSystem>();
            if (lighting != null)
                lighting.UpdateLocalLighting(pos.x, pos.y);

            // 更新Tilemap显示
            var chunks = FindObjectOfType<ChunkManager>();
            if (chunks != null)
                chunks.RefreshTile(pos.x, pos.y);

            // 粒子效果
            var particles = FindObjectOfType<ParticleManager>();
            if (particles != null)
                particles.SpawnBreakParticles(new Vector2(pos.x + 0.5f, -pos.y - 0.5f), props.baseColor);
        }

        private void HandlePlacing(Vector2Int tilePos)
        {
            var gm = GameManager.Instance;

            // 检查目标位置是否为空
            if (gm.GetTile(tilePos.x, tilePos.y) != TileType.Air) return;

            // 检查手持物品是否可放置
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem == null || selectedItem.category != ItemCategory.Placeable) return;

            // 放置方块
            gm.SetTile(tilePos.x, tilePos.y, selectedItem.placesTile);

            // 消耗物品
            Inventory.Instance.ConsumeSelected();

            // 更新显示
            var chunks = FindObjectOfType<ChunkManager>();
            if (chunks != null)
                chunks.RefreshTile(tilePos.x, tilePos.y);

            // 更新光照
            var lighting = FindObjectOfType<LightingSystem>();
            if (lighting != null)
                lighting.UpdateLocalLighting(tilePos.x, tilePos.y);
        }

        private void SpawnItemDrop(Vector2Int tilePos, int itemId)
        {
            // 直接添加到背包（简化版，后续可改为物理掉落）
            Inventory.Instance?.AddItem(itemId, 1);
        }

        /// <summary>
        /// 获取当前挖掘进度 0-1
        /// </summary>
        public float GetMiningProgress()
        {
            if (_miningTarget.x < 0) return 0f;
            var tile = GameManager.Instance.GetTile(_miningTarget.x, _miningTarget.y);
            var props = TileRegistry.Get(tile);
            if (props == null) return 0f;

            float pickPower = 1f;
            var selectedItem = Inventory.Instance?.GetSelectedItem();
            if (selectedItem != null && selectedItem.category == ItemCategory.Tool)
                pickPower = selectedItem.pickPower;

            float requiredTime = props.hardness * 0.15f / pickPower;
            return requiredTime > 0 ? _miningProgress / requiredTime : 0f;
        }

        public Vector2Int GetMiningTarget() => _miningTarget;
    }
}
