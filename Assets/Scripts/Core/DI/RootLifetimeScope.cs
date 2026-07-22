using VContainer;
using VContainer.Unity;
using UnityDotsCrowdLab.Core.UI;
using UnityDotsCrowdLab.Features.SpawnStatus;
using UnityDotsCrowdLab.Core.Fps;

namespace UnityDotsCrowdLab.Core.DI
{
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<SpawnStatusPresenter>();
            builder.RegisterEntryPoint<FpsPresenter>();
            builder.RegisterEntryPoint<FpsModel>(Lifetime.Singleton)
                .As<IFpsModel>();

            builder.RegisterComponentInHierarchy<DOTSView>();
            builder.RegisterEntryPoint<SpawnStatusModel>(Lifetime.Singleton)
               .As<ISpawnStatustModel>();
        }
    }
}