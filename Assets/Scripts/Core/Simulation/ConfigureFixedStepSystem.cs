using Unity.Entities;
using Unity.Burst;

namespace UnityDotsCrowdLab.Core.Simulation
{
    /// <summary>
    /// FixedStepSimulationSystemGroupのステップ時間を設定するシステム
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial struct ConfigureFixedStepSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            fixedStepGroup.Timestep = 1f / 60f;
        }
    }
}