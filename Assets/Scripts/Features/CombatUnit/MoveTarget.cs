using Unity.Entities;
using Unity.Mathematics;

namespace UnityDotsCrowdLab.Features.CombatUnit
{
    public struct MoveTarget : IComponentData
    {
        public Entity TargetEntity;
        public float Speed;
    }
}