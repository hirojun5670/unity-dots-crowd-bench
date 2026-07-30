using Unity.Entities;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using Unity.Mathematics;
using Unity.Collections;
using UnityDotsCrowdLab.Features.Targeting;
using UnityDotsCrowdLab.Core.Spatial;
using UnityDotsCrowdLab.Features.Spawner;
using Unity.Burst;
using UnityDotsCrowdLab.Features.SpatialHash;
using Unity.Jobs;
using UnityDotsCrowdLab.Core.Job;

namespace UnityDotsCrowdLab.Features.BoidModel
{
    /// <summary>
    /// Boidモデルの挙動を計算するシステム     
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(SpatialHashBuildSystem))]
    [UpdateAfter(typeof(UpdateMoveTargetPositionSystem))]
    [BurstCompile]
    public partial struct BoidSystem : ISystem
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
            if (!SystemAPI.HasSingleton<TargetingConfig>()) return;
            var config = SystemAPI.GetSingleton<TargetingConfig>();
            if (config.CellSize <= 0f) return;

            var sharedJobhandle = SystemAPI.GetSingleton<SharedJobDependency>().Handle;
            var combined = JobHandle.CombineDependencies(state.Dependency, sharedJobhandle);

            var singleton = SystemAPI.GetSingleton<SpatialHashMapSingleton>();

            var handle = new BoidModelJob
            {
                spatialMap = singleton.SpatialMap,
                snapshotMap = singleton.SnapshotMap,
                cellSize = config.CellSize,
                deltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(combined);

            state.Dependency = handle;
            SystemAPI.SetSingleton(new SharedJobDependency { Handle = handle });
        }

        [BurstCompile]
        public partial struct BoidModelJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> spatialMap;
            [ReadOnly] public NativeParallelHashMap<Entity, BoidSnapshot> snapshotMap;
            [ReadOnly] public float cellSize;
            [ReadOnly] public float deltaTime;

            public void Execute(Entity entity, ref LocalTransform transform, ref BoidVelocity velocity, in FactionData faction, in UnitRadius radius, in MoveTarget moveTarget, in CombatTarget combatTarget)
            {
                int3 myCell = SpatialHashUtility.ComputeCellCoord(transform.Position, cellSize);

                int cellSpan = (int)math.ceil((radius.Radius * 2f) / cellSize);
                cellSpan = math.max(cellSpan, 1); // 最低でも隣接1セルは見る

                float3 separationForce = float3.zero;
                float3 alignmentForce = float3.zero;
                float3 cohesionCenter = float3.zero;
                int neighborCount = 0;

                for (int dx = -cellSpan; dx <= cellSpan; dx++)
                    for (int dy = -cellSpan; dy <= cellSpan; dy++)
                        for (int dz = -cellSpan; dz <= cellSpan; dz++)
                        {
                            int neighborHash = SpatialHashUtility.ComputeHash(myCell + new int3(dx, dy, dz));

                            foreach (var other in spatialMap.GetValuesForKey(neighborHash))
                            {
                                if (other == entity) continue;
                                if (!snapshotMap.TryGetValue(other, out var otherSnap)) continue;
                                if (faction.Team != otherSnap.Team) continue;
                                float3 otherPosition = otherSnap.Position;
                                // 分離
                                float3 separationVector = transform.Position - otherPosition;
                                float distanceSq = math.lengthsq(separationVector);
                                float reciprocalDistanceSq = distanceSq > 1e-6f ? 1f / distanceSq : 0f; // 0除算防止
                                separationForce += separationVector * reciprocalDistanceSq;

                                // 整列
                                float3 otherVelocity = otherSnap.Velocity;
                                alignmentForce += otherVelocity;

                                // 結合
                                cohesionCenter += otherPosition;
                                neighborCount++;
                            }
                        }

                float3 cohesionForce = float3.zero;
                if (neighborCount > 0)
                {
                    alignmentForce /= neighborCount;
                    cohesionCenter /= neighborCount;
                    cohesionForce = cohesionCenter - transform.Position;
                }

                // Weight
                float separationWeight = 5.2f;
                float alignmentWeight = 0.8f;
                float cohesionWeight = 0.8f;
                float targetWeight = 1.5f;

                // ターゲットへの力を計算
                float3 targetForce = float3.zero;
                // ターゲットはCombatTargetが優先されるが、なければMoveTargetを使用する
                Entity targetEntity = combatTarget.Value != Entity.Null
                    ? combatTarget.Value
                    : moveTarget.TargetEntity;
                if (targetEntity != Entity.Null)
                {
                    float3 targetPosition = float3.zero;
                    if (snapshotMap.TryGetValue(targetEntity, out var targetEntitySnap))
                    {
                        targetPosition = targetEntitySnap.Position;
                    }
                    else
                    {
                        targetPosition = moveTarget.TargetPosition;
                    }
                    float3 toTarget = targetPosition - transform.Position;
                    float targetDistance = math.length(toTarget);

                    // 自分とターゲットの半径分は重ならないよう停止距離を設ける
                    float stopDistance = radius.Radius;
                    if (snapshotMap.TryGetValue(targetEntity, out var targetRadiusSnap))
                    {
                        stopDistance += targetRadiusSnap.Radius;
                    }

                    // ターゲットが停止距離より遠ければ、ターゲットに向かう力を加える
                    if (targetDistance > stopDistance && targetDistance > 0.0001f)
                    {
                        float3 desiredVelocity = (toTarget / targetDistance) * moveTarget.Speed;
                        targetForce = desiredVelocity - velocity.Value;
                    }
                    else if (targetDistance > 0.0001f)
                    {
                        // ターゲットが停止距離内に入った場合は、ターゲットから押し返す力を加える
                        float overlap = stopDistance - targetDistance;
                        float overlapRatio = math.saturate(overlap / math.max(stopDistance, 0.0001f));
                        float3 pushBack = -(toTarget / targetDistance) * (moveTarget.Speed * overlapRatio);
                        targetForce = pushBack - velocity.Value;
                    }
                    else
                    {
                        // 完全に同一座標の場合は少なくとも減速して静止へ向かわせる
                        targetForce = -velocity.Value;
                    }
                }

                // 分離、整列、結合、ターゲットへの力を組み合わせる
                float3 boidForce = separationForce * separationWeight + alignmentForce * alignmentWeight + cohesionForce * cohesionWeight + targetForce * targetWeight;

                // 速度制限を適用
                float currentSpeed = math.length(boidForce);
                if (currentSpeed > moveTarget.Speed && currentSpeed > 0.0001f)
                {
                    boidForce = (boidForce / currentSpeed) * moveTarget.Speed;
                    // 速度制限処理後の実速度で再計算
                    currentSpeed = math.length(boidForce);
                }

                // 速度と位置を更新
                velocity.Value = boidForce;
                transform.Position += boidForce * deltaTime;


                // 進行方向に回転させる
                if (currentSpeed > 0.0001f)
                {
                    float3 forward = boidForce / currentSpeed;
                    quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                    float turnSpeed = 3f;
                    // 回転を補間して滑らかにする
                    transform.Rotation = math.slerp(
                        transform.Rotation,
                        targetRotation,
                        math.saturate(turnSpeed * deltaTime)
                    );
                }
            }
        }
    }
}
