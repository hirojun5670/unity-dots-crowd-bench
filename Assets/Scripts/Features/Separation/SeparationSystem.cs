using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using Unity.Mathematics;
using Unity.Collections;
using UnityDotsCrowdLab.Features.Targeting;
using UnityDotsCrowdLab.Core.Spatial;

namespace UnityDotsCrowdLab.Features.Separation
{
    /// <summary>
    /// 空間ハッシュを用いて、ユニット間の重なりを防止するシステム
    /// 近いユニット同士が重ならないように分離力を計算・適用
    /// </summary>
    [BurstCompile]
    public partial struct SeparationSystem : ISystem
    {
        NativeParallelMultiHashMap<int, Entity> spatialMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spatialMap = new NativeParallelMultiHashMap<int, Entity>(1000, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            spatialMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;

            if (!SystemAPI.HasSingleton<TargetingConfig>()) return;
            var config = SystemAPI.GetSingleton<TargetingConfig>();
            float cellSize = config.CellSize;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            spatialMap.Clear();

            foreach (var (transform, radius, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<UnitRadius>>().WithEntityAccess())
            {
                int3 cellCoord = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, cellSize);
                spatialMap.Add(SpatialHashUtility.ComputeHash(cellCoord), entity);
            }

            // 近傍探索して分離ベクトルを計算
            foreach (var (transform, radius, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitRadius>>().WithEntityAccess())
            {
                float3 separation = float3.zero;
                int3 myCell = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, cellSize);

                // 重なり判定に必要な範囲は「自半径+相手の最大半径」程度なので、
                // 索敵と同様cellSpanを計算(今回は半径2つ分を目安に)
                int cellSpan = (int)math.ceil((radius.ValueRO.Radius * 2f) / cellSize);
                cellSpan = math.max(cellSpan, 1); // 最低でも隣接1セルは見る

                for (int dx = -cellSpan; dx <= cellSpan; dx++)
                    for (int dy = -cellSpan; dy <= cellSpan; dy++)
                        for (int dz = -cellSpan; dz <= cellSpan; dz++)
                        {
                            int neighborHash = SpatialHashUtility.ComputeHash(myCell + new int3(dx, dy, dz));

                            foreach (var other in spatialMap.GetValuesForKey(neighborHash))
                            {
                                if (other == entity) continue;

                                var otherTransform = SystemAPI.GetComponent<LocalTransform>(other);
                                var otherRadius = SystemAPI.GetComponent<UnitRadius>(other);

                                float dist = math.distance(transform.ValueRO.Position, otherTransform.Position);
                                float minDist = radius.ValueRO.Radius + otherRadius.Radius;

                                if (dist < minDist && dist > 0.0001f) // 0除算防止
                                {
                                    float3 away = (transform.ValueRO.Position - otherTransform.Position) / dist;
                                    float overlap = minDist - dist;
                                    separation += away * overlap;
                                }
                            }
                        }

                transform.ValueRW.Position += separation;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
