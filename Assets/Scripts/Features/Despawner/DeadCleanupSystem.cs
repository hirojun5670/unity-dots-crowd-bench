using Unity.Burst;
using Unity.Entities;

namespace UnityDotsCrowdLab.Features.Despawner
{
    /// <summary>
    /// DeadTag が付いたEntityを一括削除するSystem
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct DeadCleanupSystem : ISystem
    {
        private EntityQuery deadQuery;

        public void OnCreate(ref SystemState state)
        {
            deadQuery = SystemAPI.QueryBuilder().WithAll<DeadTag>().Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (deadQuery.IsEmptyIgnoreFilter)
                return;

            state.EntityManager.DestroyEntity(deadQuery);
        }
    }
}
