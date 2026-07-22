using R3;
using VContainer.Unity;
using System;
using Unity.Entities;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Core.Fps
{
    public interface IFpsModel
    {
        ReadOnlyReactiveProperty<float> CurrentFps { get; }
        ReadOnlyReactiveProperty<float> AverageFps { get; }
        ReadOnlyReactiveProperty<TargetingMode> Mode { get; }
    }

    public class FpsModel : IFpsModel, ITickable, IDisposable
    {
        readonly ReactiveProperty<float> currentFps = new(0);
        readonly ReactiveProperty<float> averageFps = new(0);
        readonly ReactiveProperty<TargetingMode> mode = new(TargetingMode.BruteForce);

        public ReadOnlyReactiveProperty<float> CurrentFps => currentFps;
        public ReadOnlyReactiveProperty<float> AverageFps => averageFps;
        public ReadOnlyReactiveProperty<TargetingMode> Mode => mode;

        float accumTime;
        int accumFrames;
        const float SampleInterval = 1f;

        EntityQuery configQuery;

        public FpsModel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            configQuery = world.EntityManager.CreateEntityQuery(typeof(TargetingConfig));
        }

        public void Tick()
        {
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            currentFps.Value = 1f / deltaTime;

            accumTime += deltaTime;
            accumFrames++;
            if (accumTime >= SampleInterval)
            {
                averageFps.Value = accumFrames / accumTime;
                accumTime = 0f;
                accumFrames = 0;
            }

            if (!configQuery.IsEmpty)
            {
                mode.Value = configQuery.GetSingleton<TargetingConfig>().Mode;
            }
        }

        public void Dispose()
        {
            currentFps.Dispose();
            averageFps.Dispose();
            mode.Dispose();
        }
    }
}
