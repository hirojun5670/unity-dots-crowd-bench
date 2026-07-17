using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using UnityDotsCrowdLab.Features.Movement;

namespace UnityDotsCrowdLab.Features.Spawner
{

  [BurstCompile]
  public partial struct SpawnerSystem : ISystem
  {
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      float deltaTime = SystemAPI.Time.DeltaTime;
      var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

      foreach (var spawner in SystemAPI.Query<RefRW<SpawnerData>>())
      {
        spawner.ValueRW.Timer += deltaTime;
        if (spawner.ValueRW.Timer >= spawner.ValueRO.Interval)
        {
          spawner.ValueRW.Timer = 0f;
          var newEntity = ecb.Instantiate(spawner.ValueRO.Prefab);
          var startPos = SystemAPI.GetComponent<LocalTransform>(spawner.ValueRO.StartPoint).Position;
          ecb.SetComponent(newEntity, LocalTransform.FromPosition(startPos));
          ecb.AddComponent(newEntity, new MoveTarget
          {
            TargetEntity = spawner.ValueRO.TargetPoint,
            Speed = 3f
          });
        }
      }

      ecb.Playback(state.EntityManager);
      ecb.Dispose();
    }
  }
}