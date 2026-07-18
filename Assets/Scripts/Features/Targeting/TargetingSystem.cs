using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.Spawner;
using UnityDotsCrowdLab.Features.CombatUnit;
using Unity.Mathematics;

namespace UnityDotsCrowdLab.Features.Targeting
{
    /// <summary>
    /// 各Entityに対して索敵を行うSystem
    /// 自分の陣営と異なるEntityと距離との距離を計算し、射程内かつ一番近いものを探す
    /// </summary>
    [BurstCompile]
    public partial struct TargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, faction, radius, attack, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>, RefRO<AttackPowerData>>().WithEntityAccess())
            {
                // 一番近いターゲットを索敵する
                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;

                foreach (var (targetTransform, targetFaction, targetRadius, targetEntity) in
                    SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>>().WithEntityAccess())
                {
                    if (faction.ValueRO.Team == targetFaction.ValueRO.Team) continue;

                    float surfaceDist = math.distance(transform.ValueRO.Position, targetTransform.ValueRO.Position)
                     - radius.ValueRO.Radius - targetRadius.ValueRO.Radius;
                    if (surfaceDist > attack.ValueRO.Range) continue; // 射程外は対象外

                    float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = targetEntity;
                    }
                }

                ecb.SetComponent(entity, new CombatTarget { Value = nearest });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}