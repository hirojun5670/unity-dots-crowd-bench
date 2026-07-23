using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using Unity.Mathematics;
using Unity.Collections;
using UnityDotsCrowdLab.Core.Spatial;

namespace UnityDotsCrowdLab.Features.Targeting
{
    /// <summary>
    /// 空間ハッシュを用いたターゲティングを行う
    /// </summary>
    [BurstCompile]
    public partial struct SpatialHashTargetingSystem : ISystem
    {
        NativeParallelMultiHashMap<int, Entity> spatialMap;

        ComponentLookup<FactionData> factionLookup;
        ComponentLookup<LocalTransform> transformLookup;
        ComponentLookup<UnitRadius> radiusLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spatialMap = new NativeParallelMultiHashMap<int, Entity>(1000, Allocator.Persistent);

            factionLookup = state.GetComponentLookup<FactionData>(isReadOnly: true);
            transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            radiusLookup = state.GetComponentLookup<UnitRadius>(isReadOnly: true);
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
            factionLookup.Update(ref state);
            transformLookup.Update(ref state);
            radiusLookup.Update(ref state);

            // 空間ハッシュの構築
            foreach (var (transform, faction, radius, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>>().WithEntityAccess())
            {
                int3 cellCoord = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, cellSize);
                var hash = SpatialHashUtility.ComputeHash(cellCoord);
                spatialMap.Add(hash, entity);
            }


            foreach (var (transform, faction, radius, attack, entity) in
                        SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>, RefRO<AttackPowerData>>().WithEntityAccess())
            {
                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;
                int3 myCell = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, config.CellSize);
                float estimatedMaxTargetRadius = 0.5f; // 目安としての最大ターゲット半径、必要に応じて調整
                float maxPossibleDistance = attack.ValueRO.Range + radius.ValueRO.Radius + estimatedMaxTargetRadius;
                int cellSpan = (int)math.ceil(maxPossibleDistance / cellSize);


                for (int dx = -cellSpan; dx <= cellSpan; dx++)
                    for (int dy = -cellSpan; dy <= cellSpan; dy++)
                        for (int dz = -cellSpan; dz <= cellSpan; dz++)
                        {
                            int neighborHash = SpatialHashUtility.ComputeHash(myCell + new int3(dx, dy, dz));

                            foreach (var candidate in spatialMap.GetValuesForKey(neighborHash))
                            {
                                if (candidate == entity) continue;
                                if (faction.ValueRO.Team == factionLookup[candidate].Team) continue;

                                var targetTransform = transformLookup[candidate];
                                var targetRadius = radiusLookup[candidate];

                                float distSq = math.distancesq(transform.ValueRO.Position, targetTransform.Position);
                                float maxAttackDistance = attack.ValueRO.Range + radius.ValueRO.Radius + targetRadius.Radius;
                                if (distSq > maxAttackDistance * maxAttackDistance) continue; // 射程外は対象外


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
    }
}
