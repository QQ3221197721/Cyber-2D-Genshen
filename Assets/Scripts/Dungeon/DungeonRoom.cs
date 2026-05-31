using UnityEngine;

namespace CyberTerraria
{
    public enum RoomType
    {
        Combat,      // 战斗房间（击杀所有敌人开门）
        Treasure,    // 宝箱房间
        Trap,        // 陷阱走廊
        Hidden,      // 隐藏房间
        Boss,        // Boss房间
        Entrance     // 入口房间
    }

    public class DungeonRoom
    {
        public RoomType Type;
        public int X, Y;           // 房间左下角在副本地图中的坐标
        public int Width, Height;   // 房间大小
        public bool IsCleared;      // 是否已清理
        public int EnemyCount;      // 剩余敌人数

        // 连接到下一个房间的走廊出口位置
        public Vector2Int ExitPoint;

        public DungeonRoom(RoomType type, int x, int y, int w, int h)
        {
            Type = type;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            IsCleared = (type == RoomType.Entrance);
        }
    }
}
