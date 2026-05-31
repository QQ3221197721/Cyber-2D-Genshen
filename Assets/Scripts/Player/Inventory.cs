using UnityEngine;
using System;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 背包系统 - 管理物品存储、快捷栏、装备
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        [Header("容量")]
        public int inventorySize = 40;
        public int hotbarSize = 9;

        public int SelectedSlot { get; private set; } = 0;

        // 背包格子
        public InventorySlot[] Slots { get; private set; }

        // 耐久度追踪
        private int[] _slotDurability = new int[40];

        // 事件
        public event Action OnInventoryChanged;
        public event Action<int> OnHotbarSelected;
        public event Action<int, int> OnItemPickedUp; // itemId, count

        private void Awake()
        {
            Instance = this;
            Slots = new InventorySlot[inventorySize];
            for (int i = 0; i < inventorySize; i++)
                Slots[i] = new InventorySlot();
        }

        private void Start()
        {
            // 初始物品
            AddItem(100, 1); // 废铁镐
            AddItem(200, 1); // 废铁刀
            AddItem(300, 5); // 纳米修复剂
            AddItem(500, 50); // 混凝土块
        }

        private void Update()
        {
            // 数字键切换快捷栏
            for (int i = 0; i < hotbarSize; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectedSlot = i;
                    OnHotbarSelected?.Invoke(i);
                }
            }

            // 滚轮切换
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                SelectedSlot = (SelectedSlot - 1 + hotbarSize) % hotbarSize;
                OnHotbarSelected?.Invoke(SelectedSlot);
            }
            else if (scroll < 0f)
            {
                SelectedSlot = (SelectedSlot + 1) % hotbarSize;
                OnHotbarSelected?.Invoke(SelectedSlot);
            }
        }

        /// <summary>
        /// 当前选中的物品
        /// </summary>
        public ItemData GetSelectedItem()
        {
            var slot = Slots[SelectedSlot];
            if (slot.isEmpty) return null;
            return ItemDatabase.Get(slot.itemId);
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public bool AddItem(int itemId, int count = 1)
        {
            var itemData = ItemDatabase.Get(itemId);
            if (itemData == null) return false;
            int countAdded = count;

            // 先尝试堆叠到已有槽位
            for (int i = 0; i < inventorySize; i++)
            {
                if (Slots[i].itemId == itemId && Slots[i].count < itemData.maxStack)
                {
                    int canAdd = Mathf.Min(count, itemData.maxStack - Slots[i].count);
                    Slots[i].count += canAdd;
                    count -= canAdd;
                    if (count <= 0) break;
                }
            }

            // 还有剩余，放到空槽位
            while (count > 0)
            {
                int emptySlot = FindEmptySlot();
                if (emptySlot == -1) return false; // 背包满了

                int toAdd = Mathf.Min(count, itemData.maxStack);
                Slots[emptySlot].itemId = itemId;
                Slots[emptySlot].count = toAdd;
                // 初始化耐久度
                if (itemData.maxDurability > 0)
                    _slotDurability[emptySlot] = itemData.maxDurability;
                count -= toAdd;
            }

            OnInventoryChanged?.Invoke();
            OnItemPickedUp?.Invoke(itemId, countAdded);
            return true;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveItem(int itemId, int count = 1)
        {
            int remaining = count;
            for (int i = inventorySize - 1; i >= 0; i--)
            {
                if (Slots[i].itemId == itemId)
                {
                    int toRemove = Mathf.Min(remaining, Slots[i].count);
                    Slots[i].count -= toRemove;
                    remaining -= toRemove;

                    if (Slots[i].count <= 0)
                        Slots[i].Clear();

                    if (remaining <= 0) break;
                }
            }

            OnInventoryChanged?.Invoke();
            return remaining <= 0;
        }

        /// <summary>
        /// 检查是否有足够的物品
        /// </summary>
        public bool HasItem(int itemId, int count = 1)
        {
            int total = 0;
            for (int i = 0; i < inventorySize; i++)
            {
                if (Slots[i].itemId == itemId)
                    total += Slots[i].count;
            }
            return total >= count;
        }

        /// <summary>
        /// 统计某物品的总数量
        /// </summary>
        public int CountItem(int itemId)
        {
            int total = 0;
            for (int i = 0; i < inventorySize; i++)
            {
                if (Slots[i].itemId == itemId)
                    total += Slots[i].count;
            }
            return total;
        }

        /// <summary>
        /// 消耗当前选中的物品1个
        /// </summary>
        public bool ConsumeSelected()
        {
            var slot = Slots[SelectedSlot];
            if (slot.isEmpty) return false;

            slot.count--;
            if (slot.count <= 0) slot.Clear();

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 交换两个槽位的物品
        /// </summary>
        public void SwapSlots(int a, int b)
        {
            if (a < 0 || a >= inventorySize || b < 0 || b >= inventorySize) return;
            var temp = new InventorySlot { itemId = Slots[a].itemId, count = Slots[a].count };
            Slots[a].itemId = Slots[b].itemId;
            Slots[a].count = Slots[b].count;
            Slots[b].itemId = temp.itemId;
            Slots[b].count = temp.count;
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 移除指定槽位物品
        /// </summary>
        public void RemoveItemAt(int slot, int count = 1)
        {
            if (slot < 0 || slot >= inventorySize) return;
            Slots[slot].count -= count;
            if (Slots[slot].count <= 0)
            {
                Slots[slot].Clear();
                _slotDurability[slot] = 0;
            }
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 获取指定槽位物品数据
        /// </summary>
        public ItemData GetItemAt(int slot)
        {
            if (slot < 0 || slot >= inventorySize) return null;
            if (Slots[slot].isEmpty) return null;
            return ItemDatabase.Get(Slots[slot].itemId);
        }

        // ========== 耐久度系统 ==========

        public int GetSlotDurability(int slot)
        {
            if (slot < 0 || slot >= 40) return 0;
            return _slotDurability[slot];
        }

        public void SetSlotDurability(int slot, int value)
        {
            if (slot < 0 || slot >= 40) return;
            _slotDurability[slot] = Mathf.Max(0, value);
        }

        /// <summary>
        /// 消耗耐久度，返回false表示物品已损坏
        /// </summary>
        public bool ConsumeDurability(int slot, int amount = 1)
        {
            var item = GetItemAt(slot);
            if (item == null || item.maxDurability <= 0) return true; // 无耐久限制

            _slotDurability[slot] -= amount;
            if (_slotDurability[slot] <= 0)
            {
                // 工具/武器损坏
                RemoveItemAt(slot, 1);
                return false; // 物品已损坏
            }
            OnInventoryChanged?.Invoke();
            return true;
        }

        // ========== 弹药系统 ==========

        /// <summary>
        /// 获取当前弹药数量
        /// </summary>
        public int CountAmmo(AmmoType ammoType)
        {
            int ammoId = GetAmmoItemId(ammoType);
            if (ammoId <= 0) return 0;
            return CountItem(ammoId);
        }

        /// <summary>
        /// 消耗弹药
        /// </summary>
        public bool ConsumeAmmo(AmmoType ammoType, int amount)
        {
            int ammoId = GetAmmoItemId(ammoType);
            if (ammoId <= 0) return false;
            return RemoveItem(ammoId, amount);
        }

        private int GetAmmoItemId(AmmoType type)
        {
            switch (type)
            {
                case AmmoType.Bullet: return 310;
                case AmmoType.Shell: return 311;
                case AmmoType.Energy: return 312;
                case AmmoType.Plasma: return 313;
                case AmmoType.Rail: return 314;
                default: return 0;
            }
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < inventorySize; i++)
            {
                if (Slots[i].isEmpty) return i;
            }
            return -1;
        }
    }

    [System.Serializable]
    public class InventorySlot
    {
        public int itemId;
        public int count;

        public bool isEmpty => count <= 0 || itemId <= 0;

        public void Clear()
        {
            itemId = 0;
            count = 0;
        }
    }
}
