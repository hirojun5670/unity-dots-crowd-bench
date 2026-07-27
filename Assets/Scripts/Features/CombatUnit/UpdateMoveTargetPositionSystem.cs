using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.Spawner;

namespace UnityDotsCrowdLab.Features.CombatUnit
{
    /// <summary>
    /// MoveTarget.TargetEntityの位置をMoveTarget.TargetPositionに反映するSystem
    /// </summary>
    [UpdateAfter(typeof(SpawnerSystem))]
    [BurstCompile]
    public partial struct UpdateMoveTargetPositionSystem : ISystem
    {
        ComponentLookup<LocalTransform> transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            transformLookup.Update(ref state);

            foreach (var moveTarget in SystemAPI.Query<RefRW<MoveTarget>>())
            {
                var e = moveTarget.ValueRO.TargetEntity;
                if (e != Entity.Null)
                    continue;
                if (transformLookup.HasComponent(e))
                {
                    moveTarget.ValueRW.TargetPosition = transformLookup[e].Position;
                }
                else
                {
                    moveTarget.ValueRW.TargetEntity = Entity.Null;
                }
            }
        }
    }
}