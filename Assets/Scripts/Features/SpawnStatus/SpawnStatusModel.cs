using System;
using R3;
using Unity.Entities;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using VContainer.Unity;

namespace UnityDotsCrowdLab.Features.SpawnStatus
{
    public interface ISpawnStatustModel
    {
        ReadOnlyReactiveProperty<int> Count { get; }
        void SetSpawnerActive(bool active);
    }
    public class SpawnStatusModel : ISpawnStatustModel, ITickable, IDisposable
    {
        readonly ReactiveProperty<int> count = new(0);
        public ReadOnlyReactiveProperty<int> Count => count;
        EntityQuery spawnerDataQuery;
        EntityQuery entityCountQuery;

        public SpawnStatusModel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            // MoveTargetが付加されているEntityを探すクエリ
            entityCountQuery = world.EntityManager.CreateEntityQuery(typeof(MoveTarget));
            spawnerDataQuery = world.EntityManager.CreateEntityQuery(typeof(SpawnerData));
        }

        public void SetSpawnerActive(bool active)
        {
            // 全SpawnerのIsActiveをtrueに書き換える処理
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            foreach (var entity in spawnerDataQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var spawnerData = entityManager.GetComponentData<SpawnerData>(entity);
                spawnerData.IsActive = active;
                entityManager.SetComponentData(entity, spawnerData);
            }
        }

        public void Tick()
        {
            count.Value = entityCountQuery.CalculateEntityCount();
        }

        public void Dispose() => count.Dispose();
    }
}