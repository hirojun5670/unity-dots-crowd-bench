using Unity.Burst;
using Unity.Entities;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Damage;

namespace UnityDotsCrowdLab.Features.Despawner
{
    /// <summary>
    /// 削除対象を示すタグ
    /// </summary>
    public struct DeadTag : IComponentData
    {
    }

    /// <summary>
    /// healthが０以下のEntityにDeadTagを付与するSystem
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    [BurstCompile]
    public partial struct DespawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // FixedStep の終端で再生される ECB を取得
            var ecbSingleton =
                SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            var ecb = ecbSingleton
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            var handle = new DespawnerJob
            {
                ecb = ecb
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }

        [BurstCompile]
        [WithNone(typeof(DeadTag))]
        public partial struct DespawnerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in HealthData health)
            {
                if (health.Current <= 0f)
                {
                    ecb.AddComponent<DeadTag>(chunkIndex, entity);
                }
            }
        }
    }
}