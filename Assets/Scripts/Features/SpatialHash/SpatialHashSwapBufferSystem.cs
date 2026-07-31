using Unity.Entities;
using Unity.Burst;
using UnityDotsCrowdLab.Features.Targeting;
using UnityDotsCrowdLab.Core.Job;
using Unity.Jobs;

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
            var sharedJobhandle = SystemAPI.GetSingleton<SharedJobDependency>().Handle;
            var combined = JobHandle.CombineDependencies(state.Dependency, sharedJobhandle);

            state.Dependency = combined;
            SystemAPI.SetSingleton(new SharedJobDependency { Handle = combined });

            var singleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();
            singleton.ValueRW.SetReadBufferCompleteHandle(combined);
            singleton.ValueRW.SwapBuffers();
        }
    }
}