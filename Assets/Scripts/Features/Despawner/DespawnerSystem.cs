using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityDotsCrowdLab.Core.Job;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Damage;

namespace UnityDotsCrowdLab.Features.Despawner
{
    /// <summary>
    /// healthが０以下のEntityを削除するSystem
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    [BurstCompile]
    public partial struct DespawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SharedJobDependency>())
                return;

            var sharedJobhandle = SystemAPI.GetSingleton<SharedJobDependency>().Handle;
            var combined = JobHandle.CombineDependencies(state.Dependency, sharedJobhandle);

            // FixedStep の終端で再生される ECB を取得
            var ecbSingleton =
                SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            var ecb = ecbSingleton
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            var handle = new DespawnerJob
            {
                ecb = ecb
            }.ScheduleParallel(combined);

            state.Dependency = handle;
            SystemAPI.SetSingleton(new SharedJobDependency { Handle = handle });
        }

        [BurstCompile]
        public partial struct DespawnerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in HealthData health)
            {
                if (health.Current <= 0f)
                {
                    ecb.DestroyEntity(chunkIndex, entity);
                }
            }
        }
    }
}