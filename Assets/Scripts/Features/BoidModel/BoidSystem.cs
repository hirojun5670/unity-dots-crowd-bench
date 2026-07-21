using Unity.Entities;
using Unity.Transforms;
using UnityDotsCrowdLab.Features.CombatUnit;
using Unity.Mathematics;
using Unity.Collections;
using UnityDotsCrowdLab.Features.Targeting;
using UnityDotsCrowdLab.Core.Spatial;
using UnityDotsCrowdLab.Features.Spawner;
using UnityDotsCrowdLab.Features.Movement;

namespace UnityDotsCrowdLab.Features.BoidModel
{
    public partial struct BoidSystem : ISystem
    {
        NativeParallelMultiHashMap<int, Entity> spatialMap;

        ComponentLookup<FactionData> factionLookup;
        ComponentLookup<LocalTransform> transformLookup;
        ComponentLookup<BoidVelocity> velocityLookup;

        public void OnCreate(ref SystemState state)
        {
            spatialMap = new NativeParallelMultiHashMap<int, Entity>(1000, Allocator.Persistent);
            factionLookup = state.GetComponentLookup<FactionData>(isReadOnly: true);
            transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            velocityLookup = state.GetComponentLookup<BoidVelocity>(isReadOnly: true);
        }

        public void OnDestroy(ref SystemState state)
        {
            spatialMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TargetingConfig>()) return;
            var config = SystemAPI.GetSingleton<TargetingConfig>();

            spatialMap.Clear();
            factionLookup.Update(ref state);
            transformLookup.Update(ref state);
            velocityLookup.Update(ref state);

            foreach (var (transform, faction, radius, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>>().WithAll<BoidVelocity>().WithEntityAccess())
            {
                int3 cellCoord = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, config.CellSize);
                spatialMap.Add(SpatialHashUtility.ComputeHash(cellCoord), entity);
            }

            foreach (var (transform, faction, radius, moveTarget, velocity, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<FactionData>, RefRO<UnitRadius>, RefRO<MoveTarget>, RefRW<BoidVelocity>>().WithEntityAccess())
            {
                int3 myCell = SpatialHashUtility.ComputeCellCoord(transform.ValueRO.Position, config.CellSize);

                int cellSpan = (int)math.ceil((radius.ValueRO.Radius * 2f) / config.CellSize);
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
                                if (faction.ValueRO.Team != factionLookup[other].Team) continue;

                                // 分離
                                float3 separationVector = transform.ValueRO.Position - transformLookup[other].Position;
                                float distance = math.length(separationVector);
                                float reciprocalDistance = distance > 0f ? 1f / distance : 0f;
                                separationForce += math.normalize(separationVector) * reciprocalDistance;

                                // 整列
                                float3 otherVelocity = velocityLookup[other].Value;
                                alignmentForce += otherVelocity;

                                // 結合
                                float3 otherPosition = transformLookup[other].Position;
                                cohesionCenter += otherPosition;
                                neighborCount++;
                            }
                        }

                float3 cohesionForce = float3.zero;
                if (neighborCount > 0)
                {
                    alignmentForce /= neighborCount;
                    cohesionCenter /= neighborCount;
                    cohesionForce = cohesionCenter - transform.ValueRO.Position;
                }

                // Weight
                float separationWeight = 2.0f;
                float alignmentWeight = 0.8f;
                float cohesionWeight = 0.8f;
                float targetWeight = 1.5f;

                float3 targetForce = float3.zero;
                Entity targetEntity = moveTarget.ValueRO.TargetEntity;
                if (targetEntity != Entity.Null && transformLookup.HasComponent(targetEntity))
                {
                    float3 toTarget = transformLookup[targetEntity].Position - transform.ValueRO.Position;
                    float targetDistance = math.length(toTarget);
                    if (targetDistance > 0.0001f)
                    {
                        float3 desiredVelocity = (toTarget / targetDistance) * moveTarget.ValueRO.Speed;
                        targetForce = desiredVelocity - velocity.ValueRO.Value;
                    }
                }

                float3 boidForce = separationForce * separationWeight + alignmentForce * alignmentWeight + cohesionForce * cohesionWeight + targetForce * targetWeight;

                float currentSpeed = math.length(boidForce);
                if (currentSpeed > moveTarget.ValueRO.Speed && currentSpeed > 0.0001f)
                {
                    boidForce = (boidForce / currentSpeed) * moveTarget.ValueRO.Speed;
                }

                // Update velocity
                velocity.ValueRW.Value = boidForce;
                transform.ValueRW.Position += boidForce * state.WorldUnmanaged.Time.DeltaTime;


                if (currentSpeed > 0.0001f)
                {
                    float3 forward = boidForce / currentSpeed;
                    quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                    float turnSpeed = 12f;
                    transform.ValueRW.Rotation = math.slerp(
                        transform.ValueRO.Rotation,
                        targetRotation,
                        math.saturate(turnSpeed * state.WorldUnmanaged.Time.DeltaTime)
                    );
                }
            }
        }
    }
}
