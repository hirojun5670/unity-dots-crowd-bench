using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;

namespace UnityDotsCrowdLab.Features.Movement
{
    /// <summary>
    /// Entityの移動を行うSystem
    /// combatTargetがいる場合は移動を止める
    /// </summary>
    [BurstCompile]
    public partial struct MoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, unitRadius, attackPower, moveTarget, combatTarget, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitRadius>, RefRO<AttackPowerData>, RefRO<MoveTarget>, RefRO<CombatTarget>>().WithEntityAccess())
            {
                // 攻撃ターゲットがいない場合は移動
                if (combatTarget.ValueRO.Value == Entity.Null || !SystemAPI.Exists(combatTarget.ValueRO.Value))
                {
                    MoveToTarget(ref state, transform, moveTarget.ValueRO.Speed, deltaTime, moveTarget.ValueRO.TargetEntity);
                    continue;
                }
                else
                {
                    // 方向だけターゲットに向かせる
                    float3 destination = SystemAPI.GetComponent<LocalTransform>(combatTarget.ValueRO.Value).Position;
                    float3 direction = math.normalize(destination - transform.ValueRO.Position);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        void MoveToTarget(ref SystemState state, RefRW<LocalTransform> self, float speed, float deltaTime, Entity target)
        {
            float3 destination = SystemAPI.GetComponent<LocalTransform>(target).Position;
            float3 direction = math.normalize(destination - self.ValueRO.Position);
            self.ValueRW.Position += direction * speed * deltaTime;
            self.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
        }
    }
}