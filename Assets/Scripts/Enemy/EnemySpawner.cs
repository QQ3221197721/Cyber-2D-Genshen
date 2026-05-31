using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    public class EnemySpawner : MonoBehaviour
    {
        public float spawnInterval = 3f;
        public int maxEnemies = 15;
        public float spawnRadius = 30f;
        public float despawnRadius = 50f;

        private float _spawnTimer;
        private List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.isPaused) return;
            if (PlayerController.Instance == null) return;

            _spawnTimer -= Time.deltaTime;
            _activeEnemies.RemoveAll(e => e == null);

            Vector2 playerPos = PlayerController.Instance.transform.position;
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] == null) continue;
                if (Vector2.Distance(playerPos, _activeEnemies[i].transform.position) > despawnRadius)
                {
                    Destroy(_activeEnemies[i].gameObject);
                    _activeEnemies.RemoveAt(i);
                }
            }

            if (_spawnTimer <= 0f && _activeEnemies.Count < maxEnemies)
            {
                TrySpawnEnemy();
                _spawnTimer = spawnInterval;
            }
        }

        private void TrySpawnEnemy()
        {
            Vector2 playerPos = PlayerController.Instance.transform.position;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(spawnRadius * 0.7f, spawnRadius);
            Vector2 spawnPos = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            int tileX = Mathf.FloorToInt(spawnPos.x);
            int tileY = Mathf.FloorToInt(-spawnPos.y);
            var gm = GameManager.Instance;
            if (tileX < 0 || tileX >= gm.worldWidth || tileY < 0 || tileY >= gm.worldHeight) return;
            if (gm.GetTile(tileX, tileY) != TileType.Air) return;

            float depth = (float)tileY / gm.worldHeight;
            string enemyType = ChooseEnemyType(depth, gm.IsNight);

            var enemy = EnemyFactory.SpawnEnemy(enemyType, spawnPos);
            if (enemy != null)
            {
                var eb = enemy.GetComponent<EnemyBase>();
                if (eb != null) _activeEnemies.Add(eb);
            }
        }

        private string ChooseEnemyType(float depth, bool isNight)
        {
            if (depth < 0.3f)
            {
                if (isNight) return Random.value < 0.5f ? "MutantHound" : "HoverDrone";
                return Random.value < 0.5f ? "RadRoach" : "ScrapScavenger";
            }
            else if (depth < 0.6f)
            {
                float r = Random.value;
                if (r < 0.33f) return "SecurityBot";
                if (r < 0.66f) return "SpiderBot";
                return "RaiderGunner";
            }
            else
            {
                return Random.value < 0.5f ? "Abomination" : "HackedMech";
            }
        }
    }
}
