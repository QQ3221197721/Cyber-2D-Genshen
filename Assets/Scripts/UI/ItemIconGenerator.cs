using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 物品图标生成器 - 根据物品类别程序化生成16x16像素Sprite图标
    /// 每种类别有独特形状轮廓，使用物品的displayColor着色
    /// </summary>
    public static class ItemIconGenerator
    {
        private static Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();

        public static Sprite GetIcon(int itemId)
        {
            if (_cache.TryGetValue(itemId, out var cached)) return cached;
            var itemData = ItemDatabase.Get(itemId);
            if (itemData == null) return null;
            var sprite = GenerateIcon(itemData);
            _cache[itemId] = sprite;
            return sprite;
        }

        private static Sprite GenerateIcon(ItemData data)
        {
            // 弹药特殊处理 (ID 310-314)
            if (data.id >= 310 && data.id <= 314)
                return DrawBulletIcon(data);

            switch (data.category)
            {
                case ItemCategory.Tool: return DrawToolIcon(data);
                case ItemCategory.MeleeWeapon: return DrawSwordIcon(data);
                case ItemCategory.RangedWeapon: return DrawGunIcon(data);
                case ItemCategory.Material: return DrawMaterialIcon(data);
                case ItemCategory.Placeable: return DrawBlockIcon(data);
                case ItemCategory.Consumable: return DrawPotionIcon(data);
                case ItemCategory.Armor: return DrawArmorIcon(data);
                case ItemCategory.Accessory: return DrawAccessoryIcon(data);
                case ItemCategory.BossSummon: return DrawBossSummonIcon(data);
                case ItemCategory.Cyberware: return DrawCyberwareIcon(data);
                default: return DrawDefaultIcon(data);
            }
        }

        // ===== 工具/镐 =====
        private static Sprite DrawToolIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);
            Color handle = new Color(0.35f, 0.25f, 0.15f);

            // 手柄 - 对角线从左下到右上
            SetPixel(tex, 3, 2, handle);
            SetPixel(tex, 4, 3, handle);
            SetPixel(tex, 5, 4, handle);
            SetPixel(tex, 6, 5, handle);
            SetPixel(tex, 7, 6, handle);
            SetPixel(tex, 8, 7, handle);

            // 镐头 - 顶部横向
            SetPixel(tex, 6, 9, dark);
            SetPixel(tex, 7, 10, dark);
            SetPixel(tex, 8, 11, main);
            SetPixel(tex, 9, 12, main);
            SetPixel(tex, 10, 13, light);
            SetPixel(tex, 11, 14, light);

            // 镐头另一侧
            SetPixel(tex, 9, 8, dark);
            SetPixel(tex, 10, 9, main);
            SetPixel(tex, 11, 10, main);
            SetPixel(tex, 12, 11, light);
            SetPixel(tex, 13, 10, main);
            SetPixel(tex, 14, 9, dark);

            // 镐尖连接
            SetPixel(tex, 12, 12, main);
            SetPixel(tex, 13, 11, dark);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 近战武器/剑 =====
        private static Sprite DrawSwordIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);
            Color guard = new Color(0.7f, 0.6f, 0.2f);
            Color handle = new Color(0.35f, 0.25f, 0.15f);

            // 剑柄 (左下)
            SetPixel(tex, 1, 1, handle);
            SetPixel(tex, 2, 2, handle);
            SetPixel(tex, 3, 3, handle);

            // 护手
            SetPixel(tex, 3, 5, guard);
            SetPixel(tex, 4, 4, guard);
            SetPixel(tex, 5, 3, guard);

            // 剑身 (对角线向右上)
            SetPixel(tex, 5, 5, dark);
            SetPixel(tex, 6, 6, main);
            SetPixel(tex, 7, 7, main);
            SetPixel(tex, 8, 8, main);
            SetPixel(tex, 9, 9, main);
            SetPixel(tex, 10, 10, light);
            SetPixel(tex, 11, 11, light);
            SetPixel(tex, 12, 12, light);
            SetPixel(tex, 13, 13, light);

            // 剑身宽度（第二排像素）
            SetPixel(tex, 6, 5, dark);
            SetPixel(tex, 7, 6, dark);
            SetPixel(tex, 8, 7, main);
            SetPixel(tex, 9, 8, main);
            SetPixel(tex, 10, 9, main);
            SetPixel(tex, 11, 10, light);
            SetPixel(tex, 12, 11, light);

            // 剑尖
            SetPixel(tex, 14, 14, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 远程武器/枪 =====
        private static Sprite DrawGunIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);

            // 枪管 (水平)
            for (int x = 4; x <= 14; x++)
            {
                SetPixel(tex, x, 9, main);
                SetPixel(tex, x, 10, dark);
            }
            // 枪管高光
            SetPixel(tex, 12, 9, light);
            SetPixel(tex, 13, 9, light);
            SetPixel(tex, 14, 9, light);

            // 枪身/机匣
            for (int x = 4; x <= 9; x++)
            {
                SetPixel(tex, x, 8, dark);
                SetPixel(tex, x, 7, main);
            }

            // 握把 (向下)
            SetPixel(tex, 5, 6, dark);
            SetPixel(tex, 6, 6, dark);
            SetPixel(tex, 5, 5, main);
            SetPixel(tex, 6, 5, main);
            SetPixel(tex, 5, 4, main);
            SetPixel(tex, 6, 4, main);
            SetPixel(tex, 5, 3, dark);
            SetPixel(tex, 6, 3, dark);

            // 扳机
            SetPixel(tex, 7, 6, dark);

            // 枪口闪光
            SetPixel(tex, 15, 9, light);
            SetPixel(tex, 15, 10, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 材料/矿石 =====
        private static Sprite DrawMaterialIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);

            // 菱形/矿石形状
            // 中心行
            for (int x = 4; x <= 11; x++) SetPixel(tex, x, 7, main);
            for (int x = 4; x <= 11; x++) SetPixel(tex, x, 8, main);

            // 上部收窄
            for (int x = 5; x <= 10; x++) SetPixel(tex, x, 9, main);
            for (int x = 5; x <= 10; x++) SetPixel(tex, x, 10, light);
            for (int x = 6; x <= 9; x++) SetPixel(tex, x, 11, light);
            for (int x = 7; x <= 8; x++) SetPixel(tex, x, 12, light);

            // 下部收窄
            for (int x = 5; x <= 10; x++) SetPixel(tex, x, 6, dark);
            for (int x = 6; x <= 9; x++) SetPixel(tex, x, 5, dark);
            for (int x = 7; x <= 8; x++) SetPixel(tex, x, 4, dark);

            // 边缘轮廓深色
            SetPixel(tex, 4, 7, dark);
            SetPixel(tex, 4, 8, dark);
            SetPixel(tex, 11, 7, dark);
            SetPixel(tex, 11, 8, dark);

            // 高光点
            SetPixel(tex, 6, 10, Color.Lerp(light, Color.white, 0.5f));
            SetPixel(tex, 7, 11, Color.Lerp(light, Color.white, 0.3f));

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 可放置方块 =====
        private static Sprite DrawBlockIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.2f);

            // 12x12方块居中
            for (int x = 2; x <= 13; x++)
            {
                for (int y = 2; y <= 13; y++)
                {
                    // 边框
                    if (x == 2 || x == 13 || y == 2 || y == 13)
                        SetPixel(tex, x, y, dark);
                    else
                        SetPixel(tex, x, y, main);
                }
            }

            // 简单纹理线条 - 对角线
            SetPixel(tex, 4, 4, light);
            SetPixel(tex, 5, 5, light);
            SetPixel(tex, 9, 9, light);
            SetPixel(tex, 10, 10, light);
            SetPixel(tex, 6, 11, light);
            SetPixel(tex, 11, 6, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 消耗品/药水瓶 =====
        private static Sprite DrawPotionIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);
            Color glass = new Color(0.6f, 0.7f, 0.8f, 1f);

            // 瓶盖
            SetPixel(tex, 7, 14, glass);
            SetPixel(tex, 8, 14, glass);

            // 窄颈
            SetPixel(tex, 7, 13, glass);
            SetPixel(tex, 8, 13, glass);
            SetPixel(tex, 7, 12, glass);
            SetPixel(tex, 8, 12, glass);

            // 颈部到瓶身过渡
            SetPixel(tex, 6, 11, glass);
            SetPixel(tex, 7, 11, main);
            SetPixel(tex, 8, 11, main);
            SetPixel(tex, 9, 11, glass);

            // 瓶身 (宽)
            for (int y = 4; y <= 10; y++)
            {
                SetPixel(tex, 5, y, dark);
                SetPixel(tex, 6, y, main);
                SetPixel(tex, 7, y, main);
                SetPixel(tex, 8, y, main);
                SetPixel(tex, 9, y, main);
                SetPixel(tex, 10, y, dark);
            }

            // 瓶底
            for (int x = 5; x <= 10; x++) SetPixel(tex, x, 3, dark);

            // 液体高光
            SetPixel(tex, 6, 9, light);
            SetPixel(tex, 6, 8, light);
            SetPixel(tex, 7, 10, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 盔甲/胸甲 =====
        private static Sprite DrawArmorIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);

            // 肩部
            for (int x = 3; x <= 12; x++) SetPixel(tex, x, 13, dark);
            for (int x = 3; x <= 12; x++) SetPixel(tex, x, 12, main);

            // 领口凹陷
            for (int x = 4; x <= 11; x++) SetPixel(tex, x, 11, main);
            SetPixel(tex, 7, 11, Color.clear);
            SetPixel(tex, 8, 11, Color.clear);

            // 胸甲主体
            for (int y = 5; y <= 10; y++)
            {
                SetPixel(tex, 4, y, dark);
                SetPixel(tex, 5, y, main);
                SetPixel(tex, 6, y, main);
                SetPixel(tex, 7, y, main);
                SetPixel(tex, 8, y, main);
                SetPixel(tex, 9, y, main);
                SetPixel(tex, 10, y, main);
                SetPixel(tex, 11, y, dark);
            }

            // 底部收窄
            for (int x = 5; x <= 10; x++) SetPixel(tex, x, 4, dark);
            for (int x = 6; x <= 9; x++) SetPixel(tex, x, 3, dark);

            // 中线装饰
            for (int y = 5; y <= 10; y++) SetPixel(tex, 7, y, light);

            // 高光
            SetPixel(tex, 5, 10, light);
            SetPixel(tex, 6, 9, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 饰品/圆形宝石 =====
        private static Sprite DrawAccessoryIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.4f);

            // 圆形环
            // 外圈
            SetPixel(tex, 6, 13, dark); SetPixel(tex, 7, 13, dark);
            SetPixel(tex, 8, 13, dark); SetPixel(tex, 9, 13, dark);
            SetPixel(tex, 5, 12, dark); SetPixel(tex, 10, 12, dark);
            SetPixel(tex, 4, 11, dark); SetPixel(tex, 11, 11, dark);
            SetPixel(tex, 4, 10, dark); SetPixel(tex, 11, 10, dark);
            SetPixel(tex, 3, 9, dark); SetPixel(tex, 12, 9, dark);
            SetPixel(tex, 3, 8, dark); SetPixel(tex, 12, 8, dark);
            SetPixel(tex, 3, 7, dark); SetPixel(tex, 12, 7, dark);
            SetPixel(tex, 4, 6, dark); SetPixel(tex, 11, 6, dark);
            SetPixel(tex, 4, 5, dark); SetPixel(tex, 11, 5, dark);
            SetPixel(tex, 5, 4, dark); SetPixel(tex, 10, 4, dark);
            SetPixel(tex, 6, 3, dark); SetPixel(tex, 7, 3, dark);
            SetPixel(tex, 8, 3, dark); SetPixel(tex, 9, 3, dark);

            // 宝石中心填充
            for (int x = 6; x <= 9; x++)
                for (int y = 6; y <= 10; y++)
                    SetPixel(tex, x, y, main);
            for (int x = 5; x <= 10; x++)
                for (int y = 7; y <= 9; y++)
                    SetPixel(tex, x, y, main);

            // 宝石高光
            SetPixel(tex, 6, 10, light);
            SetPixel(tex, 7, 10, light);
            SetPixel(tex, 6, 9, light);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== Boss召唤物/骷髅星 =====
        private static Sprite DrawBossSummonIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.3f);
            Color light = Color.Lerp(main, Color.white, 0.4f);

            // 星形 - 5角交叉
            // 中心
            SetPixel(tex, 7, 7, light);
            SetPixel(tex, 8, 7, light);
            SetPixel(tex, 7, 8, light);
            SetPixel(tex, 8, 8, light);

            // 上尖
            SetPixel(tex, 7, 9, main); SetPixel(tex, 8, 9, main);
            SetPixel(tex, 7, 10, main); SetPixel(tex, 8, 10, main);
            SetPixel(tex, 7, 11, main); SetPixel(tex, 8, 11, main);
            SetPixel(tex, 7, 12, dark); SetPixel(tex, 8, 12, dark);
            SetPixel(tex, 7, 13, dark);

            // 下尖
            SetPixel(tex, 7, 6, main); SetPixel(tex, 8, 6, main);
            SetPixel(tex, 7, 5, main); SetPixel(tex, 8, 5, main);
            SetPixel(tex, 7, 4, dark); SetPixel(tex, 8, 4, dark);
            SetPixel(tex, 7, 3, dark);

            // 左尖
            SetPixel(tex, 6, 7, main); SetPixel(tex, 6, 8, main);
            SetPixel(tex, 5, 7, main); SetPixel(tex, 5, 8, main);
            SetPixel(tex, 4, 7, main); SetPixel(tex, 4, 8, main);
            SetPixel(tex, 3, 8, dark); SetPixel(tex, 3, 7, dark);
            SetPixel(tex, 2, 8, dark);

            // 右尖
            SetPixel(tex, 9, 7, main); SetPixel(tex, 9, 8, main);
            SetPixel(tex, 10, 7, main); SetPixel(tex, 10, 8, main);
            SetPixel(tex, 11, 7, main); SetPixel(tex, 11, 8, main);
            SetPixel(tex, 12, 8, dark); SetPixel(tex, 12, 7, dark);
            SetPixel(tex, 13, 7, dark);

            // 对角装饰
            SetPixel(tex, 5, 10, dark); SetPixel(tex, 6, 11, dark);
            SetPixel(tex, 10, 10, dark); SetPixel(tex, 9, 11, dark);
            SetPixel(tex, 5, 5, dark); SetPixel(tex, 6, 4, dark);
            SetPixel(tex, 10, 5, dark); SetPixel(tex, 9, 4, dark);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 赛博义体/芯片 =====
        private static Sprite DrawCyberwareIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);
            Color pin = new Color(0.7f, 0.7f, 0.7f);

            // 芯片主体方块 (6x6居中)
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 5; y <= 10; y++)
                {
                    if (x == 5 || x == 10 || y == 5 || y == 10)
                        SetPixel(tex, x, y, dark);
                    else
                        SetPixel(tex, x, y, main);
                }
            }

            // 内部电路纹路
            SetPixel(tex, 7, 8, light);
            SetPixel(tex, 8, 8, light);
            SetPixel(tex, 8, 7, light);
            SetPixel(tex, 7, 7, light);

            // 上方引脚
            SetPixel(tex, 6, 11, pin); SetPixel(tex, 6, 12, pin);
            SetPixel(tex, 8, 11, pin); SetPixel(tex, 8, 12, pin);
            SetPixel(tex, 9, 11, pin); SetPixel(tex, 9, 12, pin);

            // 下方引脚
            SetPixel(tex, 6, 4, pin); SetPixel(tex, 6, 3, pin);
            SetPixel(tex, 8, 4, pin); SetPixel(tex, 8, 3, pin);
            SetPixel(tex, 9, 4, pin); SetPixel(tex, 9, 3, pin);

            // 左侧引脚
            SetPixel(tex, 4, 6, pin); SetPixel(tex, 3, 6, pin);
            SetPixel(tex, 4, 8, pin); SetPixel(tex, 3, 8, pin);
            SetPixel(tex, 4, 9, pin); SetPixel(tex, 3, 9, pin);

            // 右侧引脚
            SetPixel(tex, 11, 6, pin); SetPixel(tex, 12, 6, pin);
            SetPixel(tex, 11, 8, pin); SetPixel(tex, 12, 8, pin);
            SetPixel(tex, 11, 9, pin); SetPixel(tex, 12, 9, pin);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 弹药/子弹 =====
        private static Sprite DrawBulletIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);
            Color light = Color.Lerp(main, Color.white, 0.3f);
            Color brass = new Color(0.7f, 0.55f, 0.2f);

            // 子弹尖端 (顶部)
            SetPixel(tex, 7, 13, light);
            SetPixel(tex, 8, 13, light);
            SetPixel(tex, 7, 12, main);
            SetPixel(tex, 8, 12, main);

            // 弹头
            SetPixel(tex, 6, 11, dark);
            SetPixel(tex, 7, 11, main);
            SetPixel(tex, 8, 11, main);
            SetPixel(tex, 9, 11, dark);
            SetPixel(tex, 6, 10, dark);
            SetPixel(tex, 7, 10, main);
            SetPixel(tex, 8, 10, main);
            SetPixel(tex, 9, 10, dark);

            // 弹壳
            for (int y = 4; y <= 9; y++)
            {
                SetPixel(tex, 6, y, Color.Lerp(brass, Color.black, 0.3f));
                SetPixel(tex, 7, y, brass);
                SetPixel(tex, 8, y, brass);
                SetPixel(tex, 9, y, Color.Lerp(brass, Color.black, 0.3f));
            }

            // 底火
            SetPixel(tex, 7, 3, dark);
            SetPixel(tex, 8, 3, dark);

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 默认图标 =====
        private static Sprite DrawDefaultIcon(ItemData data)
        {
            var tex = CreateTexture();
            Color main = data.displayColor;
            Color dark = Color.Lerp(main, Color.black, 0.4f);

            // 简单的问号形状 / 方块
            for (int x = 4; x <= 11; x++)
            {
                for (int y = 4; y <= 11; y++)
                {
                    if (x == 4 || x == 11 || y == 4 || y == 11)
                        SetPixel(tex, x, y, dark);
                    else
                        SetPixel(tex, x, y, main);
                }
            }

            tex.Apply();
            return CreateSprite(tex);
        }

        // ===== 工具方法 =====

        private static Texture2D CreateTexture()
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            // 清空为透明
            var clearPixels = new Color[16 * 16];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;
            tex.SetPixels(clearPixels);
            return tex;
        }

        private static void SetPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x >= 0 && x < 16 && y >= 0 && y < 16)
                tex.SetPixel(x, y, color);
        }

        private static Sprite CreateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
