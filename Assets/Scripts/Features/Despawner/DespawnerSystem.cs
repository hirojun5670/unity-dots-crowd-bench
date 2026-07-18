using Unity.Burst;
using Unity.Entities;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Features.Features.Despawner
{
    [UpdateAfter(typeof(CombatSystem))]
    [BurstCompile]
    public partial struct DespawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (health, entity) in SystemAPI.Query<RefRO<HealthData>>().WithEntityAccess())
            {
                if (health.ValueRO.Current <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}