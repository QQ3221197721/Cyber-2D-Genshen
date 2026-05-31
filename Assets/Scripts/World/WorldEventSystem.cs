using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace CyberTerraria
{
    /// <summary>
    /// 世界事件系统 - 赛博废土版的Blood Moon/哥布林入侵等
    /// 事件列表:
    /// - 辐射风暴 (类似Blood Moon, 夜间怪物增强)
    /// - 机械军团入侵 (类似哥布林军队)
    /// - EMP脉冲 (短时间内禁用义体)
    /// - 异变潮汐 (肉域扩张)
    /// - 数据洪流 (全息幽灵出现)
    /// - 日蚀：钢铁天幕 (类似Solar Eclipse)
    /// </summary>
    public class WorldEventSystem : MonoBehaviour
    {
        public static WorldEventSystem Instance { get; private set; }

        [Header("事件状态")]
        public WorldEvent activeEvent = WorldEvent.None;
        public float eventTimer;
        public int eventWaveCount;
        public int eventKillCount;
        public int eventKillTarget;

        [Header("事件概率")]
        public float radiationStormChance = 0.05f;  // 每晚5%
        public float mechInvasionChance = 0.03f;    // 每天3%（击败首Boss后）
        public float empPulseChance = 0.02f;

        public bool IsEventActive => activeEvent != WorldEvent.None;

        public event System.Action<WorldEvent> OnEventStarted;
        public event System.Action<WorldEvent> OnEventEnded;

        private float _checkTimer;

        private void Awake() { Instance = this; }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // 事件进行中
            if (IsEventActive)
            {
                UpdateActiveEvent();
                return;
            }

            // 定期检查是否触发事件
            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 60f; // 每60秒检查一次
                TryTriggerEvent();
            }
        }

        private void TryTriggerEvent()
        {
            var gm = GameManager.Instance;

            // 夜间检查辐射风暴
            if (gm.IsNight && Random.value < radiationStormChance)
            {
                StartEvent(WorldEvent.RadiationStorm);
                return;
            }

            // 击败Boss后可能触发机械军团
            if (gm.bossesDefeated >= 1 && Random.value < mechInvasionChance)
            {
                StartEvent(WorldEvent.MechLegionInvasion);
                return;
            }

            // 困难模式事件
            if (gm.hardmodeActivated)
            {
                if (Random.value < empPulseChance)
                {
                    StartEvent(WorldEvent.EMPPulse);
                    return;
                }
                if (Random.value < 0.02f)
                {
                    StartEvent(WorldEvent.SteelEclipse);
                    return;
                }
            }
        }

        public void StartEvent(WorldEvent evt)
        {
            activeEvent = evt;
            eventKillCount = 0;

            switch (evt)
            {
                case WorldEvent.RadiationStorm:
                    eventTimer = 180f; // 3分钟
                    Debug.Log("[事件] ⚠ 辐射风暴来袭！怪物变得更加狂暴！");
                    break;

                case WorldEvent.MechLegionInvasion:
                    eventKillTarget = 80;
                    Debug.Log("[事件] ⚠ 机械军团入侵！击败80个机械敌人来结束入侵！");
                    break;

                case WorldEvent.EMPPulse:
                    eventTimer = 60f; // 1分钟
                    Debug.Log("[事件] ⚠ EMP脉冲！义体暂时失效！");
                    break;

                case WorldEvent.MutantTide:
                    eventTimer = 240f;
                    Debug.Log("[事件] ⚠ 异变潮汐！肉域生物大量涌现！");
                    break;

                case WorldEvent.DataFlood:
                    eventTimer = 120f;
                    Debug.Log("[事件] ⚠ 数据洪流！全息幽灵出没！");
                    break;

                case WorldEvent.SteelEclipse:
                    eventTimer = 300f; // 5分钟
                    Debug.Log("[事件] ⚠ 钢铁天幕！强力机械怪物出没！");
                    break;
            }

            OnEventStarted?.Invoke(evt);
        }

        private void UpdateActiveEvent()
        {
            switch (activeEvent)
            {
                case WorldEvent.RadiationStorm:
                case WorldEvent.EMPPulse:
                case WorldEvent.MutantTide:
                case WorldEvent.DataFlood:
                case WorldEvent.SteelEclipse:
                    eventTimer -= Time.deltaTime;
                    if (eventTimer <= 0f) EndEvent();
                    break;

                case WorldEvent.MechLegionInvasion:
                    if (eventKillCount >= eventKillTarget) EndEvent();
                    break;
            }
        }

        public void RegisterEventKill()
        {
            if (activeEvent == WorldEvent.MechLegionInvasion)
                eventKillCount++;
        }

        private void EndEvent()
        {
            var ended = activeEvent;
            activeEvent = WorldEvent.None;
            Debug.Log($"[事件] {ended} 已结束！");
            OnEventEnded?.Invoke(ended);
        }

        /// <summary>
        /// 获取当前事件对刷怪的影响
        /// </summary>
        public float GetSpawnRateMultiplier()
        {
            switch (activeEvent)
            {
                case WorldEvent.RadiationStorm: return 3f;
                case WorldEvent.MechLegionInvasion: return 5f;
                case WorldEvent.MutantTide: return 4f;
                case WorldEvent.SteelEclipse: return 4f;
                default: return 1f;
            }
        }

        /// <summary>
        /// 获取事件期间敌人伤害加成
        /// </summary>
        public float GetEnemyDamageMultiplier()
        {
            switch (activeEvent)
            {
                case WorldEvent.RadiationStorm: return 1.5f;
                case WorldEvent.SteelEclipse: return 2f;
                default: return 1f;
            }
        }
    }

    public enum WorldEvent
    {
        None,
        RadiationStorm,      // 辐射风暴（夜间增强版）
        MechLegionInvasion,  // 机械军团入侵
        EMPPulse,            // EMP脉冲（义体失效）
        MutantTide,          // 异变潮汐
        DataFlood,           // 数据洪流
        SteelEclipse         // 钢铁天幕（日蚀等价）
    }
}
