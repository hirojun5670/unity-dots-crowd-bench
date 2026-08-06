using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using Unity.Mathematics;
using Unity.Collections;
using UnityDotsCrowdLab.Core.Spatial;
using UnityDotsCrowdLab.Features.SpatialHash;
using UnityDotsCrowdLab.Features.BoidModel;

namespace UnityDotsCrowdLab.Features.Targeting
{
    /// <summary>
    /// 空間ハッシュを用いたターゲティングを行う
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BoidSystem))]
    [BurstCompile]
    public partial struct SpatialHashTargetingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
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

            int frameParity = ((int)math.floor(SystemAPI.Time.ElapsedTime / math.max(SystemAPI.Time.DeltaTime, 1e-6f))) & 1;

            var singleton = SystemAPI.GetSingleton<SpatialHashMapSingleton>();

            var handle = new SpatialHashTargetingJob
            {
                spatialMap = singleton.SpatialMap,
                snapshotMap = singleton.SnapshotMap,
                cellSize = cellSize,
                frameParity = frameParity,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }

        [BurstCompile]
        public partial struct SpatialHashTargetingJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<long, Entity> spatialMap;
            [ReadOnly] public NativeParallelHashMap<Entity, BoidSnapshot> snapshotMap;
            [ReadOnly] public float cellSize;
            [ReadOnly] public int frameParity;

            public void Execute(Entity entity, ref CombatTarget combatTarget, in LocalTransform transform, in FactionData faction, in UnitRadius radius, in AttackPowerData attack)
            {
                // フレームパリティを用いて、半分のEntityだけが索敵を行うことで負荷を分散する
                if ((entity.Index & 1) != frameParity) return;

                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;
                if (!snapshotMap.TryGetValue(entity, out var mySnap)) return;

                int3 myCell = SpatialHashUtility.ComputeCellCoord(mySnap.Position, cellSize);
                float estimatedMaxTargetRadius = 0.5f; // 目安としての最大ターゲット半径、必要に応じて調整
                float maxPossibleDistance = attack.Range + radius.Radius + estimatedMaxTargetRadius;
                int cellSpan = (int)math.ceil(maxPossibleDistance / cellSize);

                // 既にターゲットが存在する場合は、ターゲットが有効かどうかを確認し索敵をスキップする
                if (combatTarget.Value != Entity.Null
                    && combatTarget.Value != entity
                    && snapshotMap.TryGetValue(combatTarget.Value, out var targetSnap)
                    && targetSnap.Team != faction.Team)
                {
                    var targetRadius = targetSnap.Radius;
                    float distSq = math.distancesq(mySnap.Position, targetSnap.Position);
                    float maxAttackDistance = attack.Range + radius.Radius + targetRadius;
                    // 射程境界で毎フレーム再索敵しないようにヒステリシスを持たせる
                    float keepTargetDistance = maxAttackDistance * 1.25f;
                    if (distSq < keepTargetDistance * keepTargetDistance) return;
                }

                for (int dx = -cellSpan; dx <= cellSpan; dx++)
                    for (int dy = -cellSpan; dy <= cellSpan; dy++)
                        for (int dz = -cellSpan; dz <= cellSpan; dz++)
                        {
                            long neighborHash = SpatialHashUtility.ComputeHashFromCellCoord(myCell + new int3(dx, dy, dz), 1 - mySnap.Team);

                            foreach (var candidate in spatialMap.GetValuesForKey(neighborHash))
                            {
                                if (candidate == entity) continue;
                                if (snapshotMap.TryGetValue(candidate, out var candidateSnap))
                                {
                                    var targetRadius = candidateSnap.Radius;
                                    float distSq = math.distancesq(mySnap.Position, candidateSnap.Position);
                                    float maxAttackDistance = attack.Range + radius.Radius + targetRadius;
                                    if (distSq > maxAttackDistance * maxAttackDistance) continue; // 射程外は対象外

                                    if (distSq < nearestDistSq)
                                    {
                                        nearestDistSq = distSq;
                                        nearest = candidate;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                combatTarget.Value = nearest;
            }
        }
    }
}
