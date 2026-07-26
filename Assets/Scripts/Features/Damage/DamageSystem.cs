using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Features.Damage
{
    /// <summary>
    /// ターゲットに対してダメージを与えるシステム
    /// </summary>
    [UpdateAfter(typeof(SpatialHashTargetingSystem))]
    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (attackPower, combatTarget) in
                SystemAPI.Query<
                    RefRW<AttackPowerData>,
                    RefRO<CombatTarget>>())
            {
                attackPower.ValueRW.Timer += deltaTime;
                if (combatTarget.ValueRO.Value == Entity.Null) continue;
                if (attackPower.ValueRO.Timer < attackPower.ValueRO.Cooldown) continue;

                attackPower.ValueRW.Timer = 0f;
                var targetHealth = SystemAPI.GetComponent<HealthData>(combatTarget.ValueRO.Value);
                targetHealth.Current -= attackPower.ValueRO.Damage;
                SystemAPI.SetComponent(combatTarget.ValueRO.Value, targetHealth);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}