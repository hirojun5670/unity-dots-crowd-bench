using Unity.Entities;
using Unity.Jobs;

namespace UnityDotsCrowdLab.Core.Job
{
    public struct SharedJobDependency : IComponentData
    {
        public JobHandle Handle;
    }
}