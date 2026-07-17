using VContainer;
using VContainer.Unity;
using UnityDotsCrowdLab.Core.UI;
using UnityDotsCrowdLab.Features.SpawnStatus;

namespace UnityDotsCrowdLab.Core.DI
{
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<SpawnStatusPresenter>();

            builder.RegisterComponentInHierarchy<DOTSView>();
            builder.RegisterEntryPoint<SpawnStatusModel>(Lifetime.Singleton)
               .As<ISpawnStatustModel>();
        }
    }
}