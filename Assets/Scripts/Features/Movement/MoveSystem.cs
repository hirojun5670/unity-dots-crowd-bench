using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnityDotsCrowdLab.Features.Movement
{

    [BurstCompile]
    public partial struct MoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, target, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveTarget>>().WithEntityAccess())
            {
                float3 destination = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.TargetEntity).Position;
                float3 direction = math.normalize(destination - transform.ValueRO.Position);
                transform.ValueRW.Position += direction * target.ValueRO.Speed * deltaTime;
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());

                if (math.distance(transform.ValueRO.Position, destination) < 0.5f)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}