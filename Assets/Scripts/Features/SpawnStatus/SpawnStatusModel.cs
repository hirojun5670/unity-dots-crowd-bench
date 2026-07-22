using System;
using R3;
using Unity.Entities;
using UnityDotsCrowdLab.Features.CombatUnit;
using VContainer.Unity;

namespace UnityDotsCrowdLab.Features.SpawnStatus
{
    public interface ISpawnStatustModel
    {
        ReadOnlyReactiveProperty<int> Count { get; }
    }
    public class SpawnStatusModel : ISpawnStatustModel, ITickable, IDisposable
    {
        readonly ReactiveProperty<int> count = new(0);
        public ReadOnlyReactiveProperty<int> Count => count;

        EntityQuery query;

        public SpawnStatusModel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            // MoveTargetが付加されているEntityを探すクエリ
            query = world.EntityManager.CreateEntityQuery(typeof(MoveTarget));
        }

        public void Tick()
        {
            count.Value = query.CalculateEntityCount();
        }

        public void Dispose() => count.Dispose();
    }
}