using Unity.Entities;
using Unity.Mathematics;

namespace UnityDotsCrowdLab.Features.Spawner
{
    public struct SpawnerData : IComponentData
    {
        public Entity Prefab;
        public int FactionID;
        public float4 FactionColor;
        public Entity StartPoint;
        public Entity TargetPoint;
        public float Interval;
        public float Timer;
    }
}