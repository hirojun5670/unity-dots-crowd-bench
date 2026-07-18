using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;


namespace UnityDotsCrowdLab.Features.Targeting
{
    [BurstCompile]
    public partial struct CombatSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, faction, health, attackPower, radius, entity) in
                SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<FactionData>,
                    RefRW<HealthData>,
                    RefRW<AttackPowerData>,
                    RefRO<UnitRadius>
                >().WithEntityAccess())
            {
                attackPower.ValueRW.Timer += deltaTime;
                if (attackPower.ValueRW.Cooldown > attackPower.ValueRW.Timer) continue;
                else
                {
                    ScoutingEnemy(ref state, transform.ValueRO, faction.ValueRO, radius.ValueRO, attackPower.ValueRO);
                    attackPower.ValueRW.Timer = 0f;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void ScoutingEnemy(
            ref SystemState state, // SystemAPIがBurstCompile時に使用するため必要
            LocalTransform transform,
            FactionData faction,
            UnitRadius radius,
            AttackPowerData attack)
        {
            foreach (var (targetTransform, targetFaction, targetRadius, targetHealth, targetEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>, RefRW<HealthData>>().WithEntityAccess())
            {
                if (faction.Team == targetFaction.ValueRO.Team) continue;

                float surfaceDistance = math.distance(transform.Position, targetTransform.ValueRO.Position)
                                         - radius.Radius - targetRadius.ValueRO.Radius;
                if (surfaceDistance > attack.Range) continue;

                targetHealth.ValueRW.Current -= attack.Damage;
                UnityEngine.Debug.Log($"Hit! Damage: {attack.Damage}, Remaining HP: {targetHealth.ValueRW.Current}");
                return;
            }
        }
    }
}