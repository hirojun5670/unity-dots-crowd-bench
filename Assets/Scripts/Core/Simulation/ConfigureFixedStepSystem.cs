using Unity.Burst;
using Unity.Entities;

namespace UnityDotsCrowdLab.Core.Simulation
{
    /// <summary>
    /// FixedStepSimulationSystemGroupのステップ時間を設定するシステム
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ConfigureFixedStepSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            fixedStepGroup.Timestep = 1f / 60f;
        }
    }
}