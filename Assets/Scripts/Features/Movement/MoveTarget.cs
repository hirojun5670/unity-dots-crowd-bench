using Unity.Entities;
using Unity.Mathematics;

namespace UnityDotsCrowdLab.Features.Movement
{
    public struct MoveTarget : IComponentData
    {
        public Entity TargetEntity;
        public float Speed;
    }
}