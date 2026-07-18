using Unity.Entities;

namespace UnityDotsCrowdLab.Features.CombatUnit
{
    public struct HealthData : IComponentData
    {
        public float Current;
        public float Max;
    }
}
