using Unity.Entities;

namespace UnityDotsCrowdLab.Features.Targeting
{
    public enum TargetingMode
    {
        BruteForce,
        SpatialHash
    }
    public struct TargetingConfig : IComponentData
    {
        public TargetingMode Mode;
        public float CellSize;
    }
}
