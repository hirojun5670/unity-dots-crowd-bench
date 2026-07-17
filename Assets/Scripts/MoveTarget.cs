using Unity.Entities;
using Unity.Mathematics;

public struct MoveTarget : IComponentData
{
    public Entity TargetEntity;
    public float Speed;
}