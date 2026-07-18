using Unity.Entities;

namespace UnityDotsCrowdLab.Features.Spawner
{
    public struct FactionData : IComponentData
    {
        public int Team; // 0 = West, 1 = East など
    }
}