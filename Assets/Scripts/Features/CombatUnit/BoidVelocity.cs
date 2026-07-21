using Unity.Entities;
using Unity.Mathematics;
namespace UnityDotsCrowdLab.Features.CombatUnit
{
    public struct BoidVelocity : IComponentData
    {
        public float3 Value;
    }
}
