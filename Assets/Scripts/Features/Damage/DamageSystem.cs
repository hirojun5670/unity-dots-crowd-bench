using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Core.Job;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Features.Damage
{
    /// <summary>
    /// ターゲットに対してダメージを与えるシステム
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(TargetingSystem))]
    [UpdateAfter(typeof(SpatialHashTargetingSystem))]
    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        private NativeParallelMultiHashMap<Entity, float> damageMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            damageMap = new NativeParallelMultiHashMap<Entity, float>(1000, Allocator.Persistent);
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // 全てのジョブの終了を待つ
            if (SystemAPI.HasSingleton<SharedJobDependency>())
                SystemAPI.GetSingleton<SharedJobDependency>().Handle.Complete();
            state.Dependency.Complete();

            if (damageMap.IsCreated)
            {
                damageMap.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var sharedJobhandle = SystemAPI.GetSingleton<SharedJobDependency>().Handle;
            var combined = JobHandle.CombineDependencies(state.Dependency, sharedJobhandle);

            damageMap.Clear();
            // entity数に応じて容量を調整する
            int count = SystemAPI.QueryBuilder().WithAll<LocalTransform, CombatTarget>().Build().CalculateEntityCount();
            int required = math.ceilpow2(math.max(1, count));
            if (damageMap.Capacity < required)
                damageMap.Capacity = required;

            // ダメージマップを構築
            var buildDamageMapJobHandle = new BuildDamageMapJob
            {
                damageMap = damageMap.AsParallelWriter(),
                deltaTime = deltaTime,
            }.ScheduleParallel(combined);

            // ダメージを適用
            var applyDamageJobHandle = new ApplyDamageJob
            {
                damageMap = damageMap,
            }.ScheduleParallel(buildDamageMapJobHandle);

            state.Dependency = applyDamageJobHandle;
            SystemAPI.SetSingleton(new SharedJobDependency { Handle = applyDamageJobHandle });
        }

        [BurstCompile]
        public partial struct BuildDamageMapJob : IJobEntity
        {
            public NativeParallelMultiHashMap<Entity, float>.ParallelWriter damageMap;
            public float deltaTime;

            public void Execute(Entity entity, ref AttackPowerData attackPower, in CombatTarget combatTarget)
            {
                attackPower.Timer += deltaTime;
                if (combatTarget.Value == Entity.Null) return;
                if (attackPower.Timer < attackPower.Cooldown) return;

                damageMap.Add(combatTarget.Value, attackPower.Damage);

                attackPower.Timer = 0f;
            }
        }

        [BurstCompile]
        public partial struct ApplyDamageJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<Entity, float> damageMap;

            public void Execute(Entity entity, ref HealthData health)
            {
                float totalDamage = 0f;
                foreach (var damage in damageMap.GetValuesForKey(entity))
                {
                    totalDamage += damage;
                }
                if (totalDamage > 0f)
                {
                    health.Current -= totalDamage;
                }
            }
        }
    }
}