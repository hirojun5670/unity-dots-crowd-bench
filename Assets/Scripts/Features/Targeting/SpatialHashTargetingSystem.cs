using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using Unity.Mathematics;
using Unity.Collections;

namespace UnityDotsCrowdLab.Features.Targeting
{
    /// <summary>
    /// 空間ハッシュを用いたターゲティングを行う
    /// </summary>
    [BurstCompile]
    public partial struct SpatialHashTargetingSystem : ISystem
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
            if (!SystemAPI.HasSingleton<TargetingConfig>())
                return;

            if (SystemAPI.GetSingleton<TargetingConfig>().Mode != TargetingMode.SpatialHash)
                return;

            var config = SystemAPI.GetSingleton<TargetingConfig>();
            float cellSize = config.CellSize;
            if (cellSize <= 0f)
                return;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            spatialMap.Clear();

            // 空間ハッシュの構築
            foreach (var (transform, faction, radius, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>>().WithEntityAccess())
            {
                int3 cellCoord = ComputeCellCoord(transform.ValueRO.Position, cellSize);
                var hash = ComputeHash(cellCoord);
                spatialMap.Add(hash, entity);
            }

            foreach (var (transform, faction, radius, attack, entity) in
                        SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>, RefRO<AttackPowerData>>().WithEntityAccess())
            {
                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;
                int3 myCell = ComputeCellCoord(transform.ValueRO.Position, config.CellSize);
                float estimatedMaxTargetRadius = 1.0f; // 目安としての最大ターゲット半径、必要に応じて調整
                float maxPossibleDistance = attack.ValueRO.Range + radius.ValueRO.Radius + estimatedMaxTargetRadius;
                int cellSpan = (int)math.ceil(maxPossibleDistance / cellSize);

                for (int dx = -cellSpan; dx <= cellSpan; dx++)
                    for (int dy = -cellSpan; dy <= cellSpan; dy++)
                        for (int dz = -cellSpan; dz <= cellSpan; dz++)
                        {
                            int neighborHash = ComputeHash(myCell + new int3(dx, dy, dz));

                            foreach (var candidate in spatialMap.GetValuesForKey(neighborHash))
                            {
                                if (candidate == entity) continue;
                                if (faction.ValueRO.Team == SystemAPI.GetComponent<FactionData>(candidate).Team) continue;

                                var targetTransform = SystemAPI.GetComponent<LocalTransform>(candidate);
                                var targetRadius = SystemAPI.GetComponent<UnitRadius>(candidate);

                                float surfaceDist = math.distance(transform.ValueRO.Position, targetTransform.Position)
 - radius.ValueRO.Radius - targetRadius.Radius;
                                if (surfaceDist > attack.ValueRO.Range) continue; // 射程外は対象外

                                float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);
                                if (distSq < nearestDistSq)
                                {
                                    nearestDistSq = distSq;
                                    nearest = candidate;
                                }
                            }
                        }

                ecb.SetComponent(entity, new CombatTarget { Value = nearest });
            }


            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }

        int3 ComputeCellCoord(float3 position, float cellSize)
        {
            return (int3)math.floor(position / cellSize);
        }

        int ComputeHash(int3 cellCoord)
        {
            // 空間ハッシュの計算、よく使われる素数でXORを取る
            return (cellCoord.x * 73856093) ^ (cellCoord.y * 19349663) ^ (cellCoord.z * 83492791);
        }
    }
}
