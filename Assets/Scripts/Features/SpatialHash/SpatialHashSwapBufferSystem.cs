using Unity.Entities;
using Unity.Burst;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Features.SpatialHash
{
    /// <summary>
    /// 空間ハッシュの読み書きバッファを入れ替えるシステム
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialHashTargetingSystem))]
    [UpdateAfter(typeof(TargetingSystem))]
    [BurstCompile]
    public partial struct SpatialHashSwapBufferSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();
            singleton.ValueRW.SwapBuffers();
        }
    }
}