using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Rendering;
using Unity.Mathematics;
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

                    // 初期位置
                    var startPos = SystemAPI.GetComponent<LocalTransform>(spawner.ValueRO.StartPoint).Position;
                    ecb.SetComponent(newEntity, LocalTransform.FromPosition(startPos));

                    // チーム
                    ecb.AddComponent(newEntity, new FactionData
                    {
                        Team = spawner.ValueRO.FactionID
                    });

                    // 移動
                    ecb.AddComponent(newEntity, new MoveTarget
                    {
                        TargetEntity = spawner.ValueRO.TargetPoint,
                        Speed = 3f
                    });

                    // ベースカラーの変更
                    ecb.AddComponent(newEntity, new URPMaterialPropertyBaseColor { Value = spawner.ValueRO.FactionColor });
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}