using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

namespace UnityDotsCrowdLab.Features.CombatUnit
{

    public class CombatUnitAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float AttackDamage = 10f;
        public float AttackRange = 2f;
        public float AttackCoolTime = 1f;
        public float Radius = 0.5f;

        class Baker : Baker<CombatUnitAuthoring>
        {
            public override void Bake(CombatUnitAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthData { Current = authoring.MaxHealth, Max = authoring.MaxHealth });
                AddComponent(entity, new AttackPowerData { Damage = authoring.AttackDamage, Range = authoring.AttackRange, Cooldown = authoring.AttackCoolTime });
                AddComponent(entity, new UnitRadius { Radius = authoring.Radius });
                AddComponent(entity, new CombatTarget { Value = Entity.Null });
                AddComponent(entity, new BoidVelocity { Value = 0f });
            }
        }
    }
}
