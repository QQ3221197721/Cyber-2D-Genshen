using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    /// <summary>
    /// 泰拉瑞亚级别程序化像素精灵生成器
    /// 生成16x16 Tile纹理：带噪声变化、边缘暗化、裂缝细节、发光效果、自动贴图变体
    /// 生成角色精灵：多帧动画（idle/run/jump/fall/mine/attack）
    /// 生成敌人精灵：基于种类的详细像素画
    /// </summary>
    public static class PixelSpriteGenerator
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // ═══════════════════════════════════════════════════════
        // TILE SPRITE GENERATION - 16x16 with Terraria-level detail
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 生成带有4个噪声变体的方块Sprite集合（类似泰拉瑞亚的方块细节）
        /// </summary>
        public static Sprite[] GenerateTileSprites(TileType type, int variants = 4)
        {
            var props = TileRegistry.Get(type);
            if (props == null) return null;

            Sprite[] sprites = new Sprite[variants];
            for (int v = 0; v < variants; v++)
            {
                string key = $"tile_{type}_{v}";
                if (_spriteCache.ContainsKey(key))
                {
                    sprites[v] = _spriteCache[key];
                    continue;
                }
                sprites[v] = GenerateSingleTileSprite(props, v);
                _spriteCache[key] = sprites[v];
            }
            return sprites;
        }

        private static Sprite GenerateSingleTileSprite(TileProperties props, int variant)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color baseCol = props.baseColor;
            int seed = (int)props.type * 137 + variant * 31;

            for (int px = 0; px < 16; px++)
            {
                for (int py = 0; py < 16; py++)
                {
                    // 多层噪声（模拟Terraria的方块纹理细节）
                    float n1 = SeededNoise(seed + px * 13 + py * 7) * 0.18f - 0.09f;
                    float n2 = SeededNoise(seed + px * 31 + py * 53 + variant * 99) * 0.08f - 0.04f;
                    float n3 = SeededNoise(seed + px * 97 + py * 61) * 0.05f - 0.025f;

                    float r = baseCol.r + n1 + n2 + n3;
                    float g = baseCol.g + n1 + n2 + n3;
                    float b = baseCol.b + n1 + n2 + n3;

                    // === 边缘处理（1px暗边 + 高光顶边）===
                    if (px == 0 || py == 0) { r *= 0.6f; g *= 0.6f; b *= 0.6f; }
                    if (px == 15 || py == 15) { r *= 0.78f; g *= 0.78f; b *= 0.78f; }
                    if (px == 1 && py > 0 && py < 15) { r *= 0.75f; g *= 0.75f; b *= 0.75f; }
                    // 顶部高光（模拟Terraria顶边亮线）
                    if (py == 14 && px > 0 && px < 15) { r += 0.06f; g += 0.06f; b += 0.06f; }

                    // === 类型特定细节 ===
                    ApplyTypeSpecificDetails(props.type, px, py, seed, variant, ref r, ref g, ref b);

                    tex.SetPixel(px, py, new Color(
                        Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        private static void ApplyTypeSpecificDetails(TileType type, int px, int py,
            int seed, int variant, ref float r, ref float g, ref float b)
        {
            switch (type)
            {
                // 混凝土 - 裂缝 + 碎石纹理
                case TileType.Concrete:
                case TileType.ReinforcedConcrete:
                    if (SeededNoise(seed + px * 3 + py * 11) > 0.93f)
                    { r *= 0.4f; g *= 0.4f; b *= 0.4f; } // 裂缝
                    if (SeededNoise(seed + px * 17 + py * 23) > 0.9f)
                    { r += 0.05f; g += 0.05f; b += 0.05f; } // 碎石亮点
                    break;

                // 锈铁 - 锈斑 + 金属反光
                case TileType.RustedMetal:
                    if (SeededNoise(seed + px * 7 + py * 3) > 0.85f)
                    { r *= 1.4f; g *= 0.5f; b *= 0.3f; } // 深锈斑
                    if (SeededNoise(seed + px * 41 + py * 37) > 0.95f)
                    { r = 0.8f; g = 0.7f; b = 0.5f; } // 金属反光
                    break;

                // 霓虹晶体 - 发光中心 + 闪烁点
                case TileType.NeonCrystal:
                    float dist = Mathf.Sqrt((px - 7.5f) * (px - 7.5f) + (py - 7.5f) * (py - 7.5f));
                    if (dist < 5f)
                    {
                        float glow = 1f + 0.6f * (1f - dist / 5f);
                        r *= glow; g *= glow; b *= glow;
                    }
                    if (SeededNoise(seed + px * 97 + py * 61) > 0.92f)
                    { r = 1f; g = 1f; b = 1f; } // 闪烁点
                    // 晶体棱角
                    if ((px + py) % 3 == 0 && dist < 6f)
                    { r += 0.1f; g += 0.15f; b += 0.1f; }
                    break;

                // 量子芯片 - 电路图案 + 紫色脉冲
                case TileType.QuantumChip:
                    if (px % 4 == 0 || py % 4 == 0)
                    { r += 0.15f; g += 0.05f; b += 0.2f; } // 电路线
                    if (px % 4 == 2 && py % 4 == 2)
                    { r = 0.9f; g = 0.4f; b = 1f; } // 节点亮点
                    float qDist = Mathf.Sqrt((px - 8) * (px - 8) + (py - 8) * (py - 8));
                    if (qDist < 3f) { r += 0.2f; g += 0.1f; b += 0.3f; }
                    break;

                // 等离子电池 - 能量条纹 + 橙色光晕
                case TileType.PlasmaCell:
                    if ((py + variant) % 3 == 0)
                    { r += 0.15f; g += 0.08f; b -= 0.05f; } // 水平能量条
                    float pDist = Mathf.Sqrt((px - 8) * (px - 8) + (py - 8) * (py - 8));
                    if (pDist < 4f) { r += 0.3f; g += 0.15f; }
                    if (SeededNoise(seed + px * 47 + py * 29) > 0.94f)
                    { r = 1f; g = 0.8f; b = 0.2f; }
                    break;

                // 电路板 - 精确电路图案
                case TileType.CircuitBoard:
                    if (px % 4 == 0 || py % 4 == 0)
                    { r += 0.08f; g += 0.15f; b += 0.1f; }
                    if (px % 4 == 2 && py % 4 == 2)
                    { r = 0.1f; g = 0.6f; b = 0.4f; } // 焊点
                    if (px % 8 == 4 && py % 8 == 4)
                    { r = 0.7f; g = 0.7f; b = 0.2f; } // IC芯片
                    break;

                // 异变肉块 - 脉动纹理 + 血管
                case TileType.FleshBlock:
                    float pulse = Mathf.Sin(px * 0.5f + py * 0.3f + variant) * 0.1f;
                    r += pulse; g -= pulse * 0.5f;
                    if (SeededNoise(seed + px * 19 + py * 23) > 0.9f)
                    { r += 0.2f; g -= 0.05f; b -= 0.05f; } // 血管
                    break;

                // 紫晶簇 - 棱角形状 + 内部发光
                case TileType.CrystalCluster:
                    if (SeededNoise(seed + px * 19 + py * 23) > 0.82f)
                    { r += 0.15f; g += 0.08f; b += 0.25f; }
                    // 晶体尖端效果
                    if (py > 10 && Mathf.Abs(px - 8) < (16 - py))
                    { r += 0.1f; b += 0.15f; }
                    break;

                // 防弹玻璃 - 反射条纹
                case TileType.BulletproofGlass:
                    if ((px + py + variant) % 5 == 0)
                    { r *= 1.4f; g *= 1.4f; b *= 1.4f; }
                    // 对角反光
                    if (Mathf.Abs(px - py) < 2) { r += 0.08f; g += 0.08f; b += 0.1f; }
                    break;

                // 霓虹面板 - 发光条纹 + 闪烁
                case TileType.NeonPanel:
                    if (py >= 5 && py <= 10)
                    { r += 0.3f; g += 0.0f; b += 0.15f; } // 中间发光带
                    if (SeededNoise(seed + px * 43 + py * 17) > 0.96f)
                    { r = 1f; g = 0.5f; b = 1f; }
                    break;

                // 全息方块 - 扫描线 + 半透明感
                case TileType.HologramBlock:
                    if (py % 2 == 0) { r *= 0.8f; g *= 0.85f; b *= 0.9f; } // 扫描线
                    float hDist = Mathf.Sqrt((px - 8) * (px - 8) + (py - 8) * (py - 8));
                    if (hDist < 6f) { r += 0.1f; g += 0.2f; b += 0.3f; }
                    break;

                // 管道 - 圆柱阴影
                case TileType.Pipe:
                    float pipeShade = 1f - Mathf.Abs(px - 8f) / 8f * 0.4f;
                    r *= pipeShade; g *= pipeShade; b *= pipeShade;
                    // 铆钉
                    if (py % 6 == 0 && (px == 3 || px == 12))
                    { r += 0.15f; g += 0.15f; b += 0.15f; }
                    break;

                // 焦土 - 颗粒感 + 偶尔的植物残骸
                case TileType.ScorchedEarth:
                    if (SeededNoise(seed + px * 5 + py * 9) > 0.88f)
                    { r += 0.06f; g += 0.04f; b += 0.02f; } // 石子
                    if (variant == 0 && py > 12 && SeededNoise(seed + px * 33) > 0.9f)
                    { r -= 0.05f; g += 0.05f; b -= 0.03f; } // 草根
                    break;

                // 变异苔 - 顶部有苔藓纤毛
                case TileType.ToxicMoss:
                    if (py > 12)
                    {
                        float moss = SeededNoise(seed + px * 7 + variant * 50);
                        if (moss > 0.4f) { g += 0.15f; r -= 0.05f; }
                        if (moss > 0.7f && py > 13) { r = 0.1f; g = 0.5f; b = 0.15f; }
                    }
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════
        // CHARACTER SPRITE - Multi-frame animation like Terraria
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 生成完整的玩家角色动画帧集（16x24像素，含idle/run/jump/mine）
        /// </summary>
        public static Dictionary<string, Sprite[]> GeneratePlayerAnimations()
        {
            var anims = new Dictionary<string, Sprite[]>();

            Color skin = new Color(0.60f, 0.48f, 0.41f);
            Color jacket = new Color(0.07f, 0.07f, 0.10f);
            Color neon = new Color(0f, 0.87f, 0.72f);
            Color boot = new Color(0.08f, 0.08f, 0.12f);
            Color hair = new Color(0f, 0.70f, 0.53f);
            Color goggle = new Color(0f, 0.90f, 0.75f);
            Color mechArm = new Color(0.53f, 0.53f, 0.60f);

            // Idle: 2帧（微微呼吸动画）
            anims["idle"] = new Sprite[]
            {
                MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm, 0, 0, 0),
                MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm, 0, 0, 1),
            };

            // Run: 6帧
            anims["run"] = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                int legOff = (int)(Mathf.Sin(i / 6f * Mathf.PI * 2f) * 2f);
                anims["run"][i] = MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm,
                    legOff, 0, Mathf.Abs(legOff) > 1 ? 1 : 0);
            }

            // Jump: 1帧
            anims["jump"] = new Sprite[]
            {
                MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm, -2, 1, 0)
            };

            // Fall: 1帧
            anims["fall"] = new Sprite[]
            {
                MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm, 1, -1, -1)
            };

            // Mine: 3帧（手臂角度变化）
            anims["mine"] = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                anims["mine"][i] = MakePlayerFrame(skin, jacket, neon, boot, hair, goggle, mechArm,
                    0, i + 1, 0);
            }

            return anims;
        }

        private static Sprite MakePlayerFrame(Color skin, Color jacket, Color neon,
            Color boot, Color hair, Color goggle, Color mechArm,
            int legOffset, int armAngle, int headBob)
        {
            int w = 16, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Clear
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            // 脚 (y 0-5) - 带腿部动画
            FillRect(tex, 4, 0 + Mathf.Max(0, legOffset), 3, 5, boot);
            FillRect(tex, 9, 0 + Mathf.Max(0, -legOffset), 3, 5, boot);
            // 靴子高光
            tex.SetPixel(5, 1 + Mathf.Max(0, legOffset), new Color(0.15f, 0.15f, 0.2f));
            tex.SetPixel(10, 1 + Mathf.Max(0, -legOffset), new Color(0.15f, 0.15f, 0.2f));

            // 身体 (y 6-14)
            FillRect(tex, 3, 6, 10, 9, jacket);
            // 霓虹边线（赛博朋克标志性）
            for (int y = 6; y < 15; y++) { tex.SetPixel(3, y, neon); tex.SetPixel(12, y, neon); }
            // 胸前霓虹装饰
            tex.SetPixel(6, 10, neon); tex.SetPixel(7, 10, neon);
            tex.SetPixel(8, 10, neon); tex.SetPixel(9, 10, neon);
            tex.SetPixel(7, 11, neon); tex.SetPixel(8, 11, neon);

            // 机械手臂（根据armAngle调整）
            FillRect(tex, 1, 7 + armAngle, 2, 5, mechArm);
            FillRect(tex, 13, 7, 2, 5, mechArm);
            // 手臂霓虹线
            tex.SetPixel(1, 9 + armAngle, neon);
            tex.SetPixel(2, 9 + armAngle, neon);
            tex.SetPixel(13, 9, neon);
            tex.SetPixel(14, 9, neon);

            // 头 (y 15-22)
            int hb = Mathf.Clamp(headBob, -1, 1);
            FillRect(tex, 4, 15 + hb, 8, 6, skin);
            // 护目镜（赛博朋克经典）
            FillRect(tex, 4, 17 + hb, 8, 2, goggle);
            // 护目镜边框
            tex.SetPixel(4, 18 + hb, new Color(0.2f, 0.2f, 0.25f));
            tex.SetPixel(11, 18 + hb, new Color(0.2f, 0.2f, 0.25f));
            // 头发（赛博绿）
            FillRect(tex, 5, 21 + hb, 6, 2, hair);
            FillRect(tex, 6, 23 + hb, 4, 1, hair);
            // 耳环/装饰
            tex.SetPixel(3, 18 + hb, neon);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        // ═══════════════════════════════════════════════════════
        // ENEMY SPRITES - Detailed per-type generation
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 生成详细的敌人精灵（每种类型有独特外观）
        /// </summary>
        public static Sprite GenerateDetailedEnemySprite(string enemyType)
        {
            string key = $"enemy_{enemyType}";
            if (_spriteCache.ContainsKey(key)) return _spriteCache[key];

            Sprite sprite;
            switch (enemyType)
            {
                case "RadRoach": sprite = MakeInsectSprite(10, 6, new Color(0.3f, 0.2f, 0.1f)); break;
                case "ToxicCrawler": sprite = MakeInsectSprite(14, 10, new Color(0.15f, 0.5f, 0.1f)); break;
                case "MutantHound": sprite = MakeQuadrupedSprite(14, 10, new Color(0.35f, 0.2f, 0.25f)); break;
                case "FleshBlob": sprite = MakeBlobSprite(16, 14, new Color(0.5f, 0.12f, 0.18f)); break;
                case "Abomination": sprite = MakeBlobSprite(22, 26, new Color(0.4f, 0.08f, 0.12f)); break;
                case "HoverDrone": sprite = MakeDroneSprite(12, 10, new Color(0.45f, 0.45f, 0.55f)); break;
                case "SecurityBot": sprite = MakeRobotSprite(14, 18, new Color(0.35f, 0.35f, 0.45f)); break;
                case "SpiderBot": sprite = MakeInsectSprite(12, 10, new Color(0.2f, 0.2f, 0.28f)); break;
                case "HackedMech": sprite = MakeRobotSprite(20, 28, new Color(0.25f, 0.25f, 0.32f)); break;
                case "ScrapScavenger": sprite = MakeHumanoidSprite(12, 18, new Color(0.4f, 0.35f, 0.3f), new Color(0.6f, 0.4f, 0.1f)); break;
                case "RaiderGunner": sprite = MakeHumanoidSprite(12, 18, new Color(0.2f, 0.18f, 0.15f), new Color(1f, 0.3f, 0.1f)); break;
                case "RaiderBerserker": sprite = MakeHumanoidSprite(14, 20, new Color(0.12f, 0.08f, 0.08f), new Color(1f, 0.1f, 0.1f)); break;
                case "CyberNinja": sprite = MakeHumanoidSprite(12, 18, new Color(0.04f, 0.04f, 0.06f), new Color(0f, 1f, 1f)); break;
                default: sprite = MakeGenericEnemySprite(12, 14, new Color(0.3f, 0.1f, 0.1f)); break;
            }

            _spriteCache[key] = sprite;
            return sprite;
        }

        // --- 昆虫型精灵 ---
        private static Sprite MakeInsectSprite(int w, int h, Color body)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            // 椭圆形身体
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float ex = (x - w / 2f) / (w / 2f);
                    float ey = (y - h / 2f) / (h / 2f);
                    if (ex * ex + ey * ey < 0.85f)
                    {
                        float shade = 1f - (ex * ex + ey * ey) * 0.3f;
                        tex.SetPixel(x, y, body * shade);
                    }
                }
            // 腿
            for (int i = 0; i < 3; i++)
            {
                int lx = w / 4 + i * w / 4;
                tex.SetPixel(lx, 0, body * 0.7f);
                tex.SetPixel(lx, 1, body * 0.7f);
            }
            // 眼睛
            tex.SetPixel(w / 3, h * 2 / 3, new Color(1f, 0.8f, 0f));
            tex.SetPixel(w * 2 / 3, h * 2 / 3, new Color(1f, 0.8f, 0f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        // --- 四足兽型 ---
        private static Sprite MakeQuadrupedSprite(int w, int h, Color body)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            // 身体（水平椭圆）
            for (int x = 2; x < w - 2; x++)
                for (int y = 3; y < h - 2; y++)
                {
                    float noise = SeededNoise(x * 13 + y * 7) * 0.15f;
                    tex.SetPixel(x, y, body * (0.85f + noise));
                }
            // 4条腿
            FillRect(tex, 2, 0, 2, 3, body * 0.7f);
            FillRect(tex, w - 4, 0, 2, 3, body * 0.7f);
            FillRect(tex, 4, 0, 2, 3, body * 0.7f);
            FillRect(tex, w - 6, 0, 2, 3, body * 0.7f);
            // 头
            FillRect(tex, w - 4, h - 4, 4, 4, body * 1.1f);
            // 红眼
            tex.SetPixel(w - 2, h - 2, new Color(1f, 0.2f, 0.1f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        // --- 团状/史莱姆型 ---
        private static Sprite MakeBlobSprite(int w, int h, Color body)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float ex = (x - w / 2f) / (w / 2.2f);
                    float ey = (y - h / 2f) / (h / 2.2f);
                    float d = ex * ex + ey * ey * 0.8f;
                    if (d < 1f)
                    {
                        float shade = 1f - d * 0.4f;
                        float pulse = Mathf.Sin(x * 0.5f + y * 0.3f) * 0.08f;
                        Color c = body * (shade + pulse);
                        c.a = 1f;
                        tex.SetPixel(x, y, c);
                    }
                }
            // 嘴
            for (int x = w / 3; x < w * 2 / 3; x++)
                tex.SetPixel(x, h / 3, new Color(0.2f, 0f, 0f));
            // 眼
            tex.SetPixel(w / 3, h * 2 / 3, Color.yellow);
            tex.SetPixel(w * 2 / 3, h * 2 / 3, Color.yellow);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.3f), 16f);
        }

        // --- 无人机型 ---
        private static Sprite MakeDroneSprite(int w, int h, Color body)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            // 扁平六边形体
            for (int x = 2; x < w - 2; x++)
                for (int y = 3; y < h - 3; y++)
                    tex.SetPixel(x, y, body);
            // 旋翼
            FillRect(tex, 0, h - 2, 3, 2, body * 0.6f);
            FillRect(tex, w - 3, h - 2, 3, 2, body * 0.6f);
            // 红色LED
            tex.SetPixel(w / 2, h / 2, new Color(1f, 0f, 0f));
            // 霓虹线
            for (int x = 3; x < w - 3; x++)
                tex.SetPixel(x, h / 2 - 1, new Color(0f, 0.8f, 1f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // --- 机器人型 ---
        private static Sprite MakeRobotSprite(int w, int h, Color body)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            // 方形躯干
            FillRect(tex, 2, 4, w - 4, h - 8, body);
            // 腿
            FillRect(tex, 3, 0, 3, 4, body * 0.8f);
            FillRect(tex, w - 6, 0, 3, 4, body * 0.8f);
            // 头
            FillRect(tex, 3, h - 5, w - 6, 5, body * 1.1f);
            // 眼（红色传感器）
            tex.SetPixel(w / 3, h - 3, new Color(1f, 0f, 0f));
            tex.SetPixel(w * 2 / 3, h - 3, new Color(1f, 0f, 0f));
            // 天线
            tex.SetPixel(w / 2, h - 1, new Color(0.6f, 0.6f, 0.6f));
            // 胸部面板
            FillRect(tex, 4, h / 2 - 1, w - 8, 3, new Color(0f, 0.5f, 0.8f, 1f));

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        // --- 人形敌人 ---
        private static Sprite MakeHumanoidSprite(int w, int h, Color outfit, Color accent)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTex(tex);

            Color skin = new Color(0.5f, 0.38f, 0.32f);
            // 脚
            FillRect(tex, 2, 0, 3, 4, outfit * 0.8f);
            FillRect(tex, w - 5, 0, 3, 4, outfit * 0.8f);
            // 身体
            FillRect(tex, 2, 4, w - 4, h - 9, outfit);
            // 霓虹条
            for (int y = 5; y < h - 6; y++)
            { tex.SetPixel(2, y, accent); tex.SetPixel(w - 3, y, accent); }
            // 头
            FillRect(tex, 3, h - 6, w - 6, 5, skin);
            // 面罩
            FillRect(tex, 3, h - 5, w - 6, 2, outfit * 0.6f);
            // 眼
            tex.SetPixel(w / 3, h - 4, accent);
            tex.SetPixel(w * 2 / 3, h - 4, accent);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        }

        private static Sprite MakeGenericEnemySprite(int w, int h, Color body)
        {
            return MakeBlobSprite(w, h, body);
        }

        // ═══════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════

        private static float SeededNoise(int seed)
        {
            float s = Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int px = x; px < x + w && px < tex.width; px++)
                for (int py = y; py < y + h && py < tex.height; py++)
                    if (px >= 0 && py >= 0)
                        tex.SetPixel(px, py, c);
        }

        private static void ClearTex(Texture2D tex)
        {
            Color[] clear = new Color[tex.width * tex.height];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
        }
    }
}
