using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    public class DungeonGenerator
    {
        public const int DUNGEON_WIDTH = 300;
        public const int DUNGEON_HEIGHT = 200;

        private TileType[,] _tiles;
        private List<DungeonRoom> _rooms;

        public TileType[,] Tiles => _tiles;
        public List<DungeonRoom> Rooms => _rooms;

        public void Generate(int difficulty)
        {
            _tiles = new TileType[DUNGEON_WIDTH, DUNGEON_HEIGHT];
            _rooms = new List<DungeonRoom>();

            // 填充墙壁
            FillWalls();

            // 生成入口房间
            GenerateEntrance();

            // 生成 5-8 个房间
            int roomCount = Random.Range(5, 9);
            for (int i = 0; i < roomCount; i++)
            {
                RoomType type;
                if (i == roomCount - 1)
                    type = RoomType.Boss;
                else
                    type = GetRandomRoomType();

                GenerateRoom(type);
            }

            // 连接房间
            ConnectRooms();

            // 放置房间内容（敌人标记、宝箱、陷阱）
            PopulateRooms(difficulty);
        }

        private void FillWalls()
        {
            for (int x = 0; x < DUNGEON_WIDTH; x++)
                for (int y = 0; y < DUNGEON_HEIGHT; y++)
                    _tiles[x, y] = TileType.ReinforcedConcrete; // 副本墙壁
        }

        private void GenerateEntrance()
        {
            var room = new DungeonRoom(RoomType.Entrance, 5, DUNGEON_HEIGHT / 2 - 5, 12, 10);
            CarveRoom(room);
            _rooms.Add(room);
        }

        private RoomType GetRandomRoomType()
        {
            float roll = Random.value;
            if (roll < 0.50f) return RoomType.Combat;
            if (roll < 0.70f) return RoomType.Treasure;
            if (roll < 0.85f) return RoomType.Trap;
            return RoomType.Hidden;
        }

        private void GenerateRoom(RoomType type)
        {
            int w = Random.Range(20, 40);
            int h = Random.Range(15, 25);

            // Boss房间更大
            if (type == RoomType.Boss) { w = 45; h = 30; }
            // 陷阱走廊长而窄
            if (type == RoomType.Trap) { w = Random.Range(30, 50); h = Random.Range(8, 12); }

            // 基于上一个房间的出口确定位置
            int x, y;
            if (_rooms.Count > 0)
            {
                var lastRoom = _rooms[_rooms.Count - 1];
                x = lastRoom.X + lastRoom.Width + Random.Range(5, 10); // 走廊间距
                y = Mathf.Clamp(lastRoom.Y + Random.Range(-10, 10), 5, DUNGEON_HEIGHT - h - 5);
            }
            else
            {
                x = 25;
                y = DUNGEON_HEIGHT / 2 - h / 2;
            }

            // 边界检查
            x = Mathf.Clamp(x, 2, DUNGEON_WIDTH - w - 2);
            y = Mathf.Clamp(y, 2, DUNGEON_HEIGHT - h - 2);

            var room = new DungeonRoom(type, x, y, w, h);
            CarveRoom(room);
            _rooms.Add(room);
        }

        private void CarveRoom(DungeonRoom room)
        {
            // 挖空房间内部
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    if (x > room.X && x < room.X + room.Width - 1 &&
                        y > room.Y && y < room.Y + room.Height - 1)
                    {
                        _tiles[x, y] = TileType.Air;
                    }
                }
            }

            // 地板使用特殊材质
            for (int x = room.X + 1; x < room.X + room.Width - 1; x++)
            {
                _tiles[x, room.Y + room.Height - 2] = TileType.CarbonFiber; // 地板
            }

            // Boss房间用霓虹面板装饰
            if (room.Type == RoomType.Boss)
            {
                for (int x = room.X + 1; x < room.X + room.Width - 1; x += 4)
                {
                    _tiles[x, room.Y + 1] = TileType.NeonPanel;
                    _tiles[x, room.Y + room.Height - 1] = TileType.NeonPanel;
                }
            }
        }

        private void ConnectRooms()
        {
            for (int i = 0; i < _rooms.Count - 1; i++)
            {
                var roomA = _rooms[i];
                var roomB = _rooms[i + 1];

                // 走廊从A的右侧中间到B的左侧中间
                int startX = roomA.X + roomA.Width - 1;
                int startY = roomA.Y + roomA.Height / 2;
                int endX = roomB.X;
                int endY = roomB.Y + roomB.Height / 2;

                // 水平走廊
                int corridorY = startY;
                for (int x = startX; x <= endX; x++)
                {
                    for (int dy = -1; dy <= 1; dy++) // 3格宽走廊
                    {
                        int cy = corridorY + dy;
                        if (cy >= 0 && cy < DUNGEON_HEIGHT && x >= 0 && x < DUNGEON_WIDTH)
                            _tiles[x, cy] = TileType.Air;
                    }
                }

                // 如果Y不同，加垂直段
                if (startY != endY)
                {
                    int midX = (startX + endX) / 2;
                    int minY = Mathf.Min(startY, endY);
                    int maxY = Mathf.Max(startY, endY);
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int cx = midX + dx;
                            if (cx >= 0 && cx < DUNGEON_WIDTH && y >= 0 && y < DUNGEON_HEIGHT)
                                _tiles[cx, y] = TileType.Air;
                        }
                    }
                    // 水平连接到B
                    corridorY = endY;
                    for (int x = midX; x <= endX; x++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int cy = corridorY + dy;
                            if (cy >= 0 && cy < DUNGEON_HEIGHT && x >= 0 && x < DUNGEON_WIDTH)
                                _tiles[x, cy] = TileType.Air;
                        }
                    }
                }

                // 设置出口点
                roomA.ExitPoint = new Vector2Int(startX, startY);
            }
        }

        private void PopulateRooms(int difficulty)
        {
            foreach (var room in _rooms)
            {
                switch (room.Type)
                {
                    case RoomType.Combat:
                        room.EnemyCount = Random.Range(3, 6) + difficulty;
                        break;
                    case RoomType.Treasure:
                        // 放置宝箱方块
                        int chestCount = Random.Range(1, 4);
                        for (int i = 0; i < chestCount; i++)
                        {
                            int cx = room.X + Random.Range(3, room.Width - 3);
                            int cy = room.Y + room.Height - 3; // 地面上
                            if (_tiles[cx, cy] == TileType.Air)
                                _tiles[cx, cy] = TileType.Chest;
                        }
                        break;
                    case RoomType.Trap:
                        // 放置陷阱（用管道方块模拟激光发射器）
                        for (int x = room.X + 5; x < room.X + room.Width - 5; x += 5)
                        {
                            _tiles[x, room.Y + 1] = TileType.Pipe; // 顶部发射器
                            _tiles[x, room.Y + room.Height - 2] = TileType.Pipe; // 底部发射器
                        }
                        room.EnemyCount = Random.Range(1, 3);
                        break;
                    case RoomType.Boss:
                        room.EnemyCount = 1; // Boss
                        break;
                    case RoomType.Hidden:
                        // 入口墙壁用普通混凝土（可破坏）代替强化混凝土
                        for (int y = room.Y + 2; y < room.Y + room.Height - 2; y++)
                            _tiles[room.X, y] = TileType.Concrete;
                        // 放置稀有奖励宝箱
                        _tiles[room.X + room.Width / 2, room.Y + room.Height - 3] = TileType.Chest;
                        break;
                }
            }
        }
    }
}
