using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Core.Spatial;
using UnityDotsCrowdLab.Features.CombatUnit;
using UnityDotsCrowdLab.Features.Spawner;
using UnityDotsCrowdLab.Features.Targeting;

namespace UnityDotsCrowdLab.Features.SpatialHash
{
    /// <summary>
    /// 空間ハッシュを構築するシステム
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnerSystem))]
    [BurstCompile]
    public partial struct SpatialHashBuildSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // シングルトンEntityを作成し、コンポーネントとして登録
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new SpatialHashMapSingleton
            {
                CurrentReadBufferIndex = 0,
                SpatialMapBuffer0 = new NativeParallelMultiHashMap<long, Entity>(1000, Allocator.Persistent),
                SnapshotMapBuffer0 = new NativeParallelHashMap<Entity, BoidSnapshot>(1000, Allocator.Persistent),
                SpatialMapBuffer1 = new NativeParallelMultiHashMap<long, Entity>(1000, Allocator.Persistent),
                SnapshotMapBuffer1 = new NativeParallelHashMap<Entity, BoidSnapshot>(1000, Allocator.Persistent),
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // 全てのジョブの終了を待つ
            state.Dependency.Complete();

            var singleton = SystemAPI.GetSingleton<SpatialHashMapSingleton>();
            singleton.SpatialMapBuffer0.Dispose();
            singleton.SnapshotMapBuffer0.Dispose();
            singleton.SpatialMapBuffer1.Dispose();
            singleton.SnapshotMapBuffer1.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TargetingConfig>())
                return;

            var config = SystemAPI.GetSingleton<TargetingConfig>();

            var singleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();

            singleton.ValueRW.CompleteNextWriteBufferDependency();

            singleton.ValueRW.WriteSpatialMap.Clear();
            singleton.ValueRW.WriteSnapshotMap.Clear();

            // entity数に応じて容量を調整する
            int count = SystemAPI.QueryBuilder().WithAll<LocalTransform, BoidVelocity>().Build().CalculateEntityCount();
            int required = math.ceilpow2(math.max(1, count));
            singleton.ValueRW.SetWriteBufferCapacity(required);

            var handle = new BuildSpatialHashJob
            {
                SpatialMap = singleton.ValueRW.WriteSpatialMap.AsParallelWriter(),
                SnapshotMap = singleton.ValueRW.WriteSnapshotMap.AsParallelWriter(),
                CellSize = config.CellSize,
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }
    }

    [BurstCompile]
    public struct BoidSnapshot
    {
        public float3 Position;
        public float3 Velocity;
        public float Radius;
        public int Team;
    }

    // 空間ハッシュを構築するジョブ
    [BurstCompile]
    public partial struct BuildSpatialHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<long, Entity>.ParallelWriter SpatialMap;
        public NativeParallelHashMap<Entity, BoidSnapshot>.ParallelWriter SnapshotMap;
        [ReadOnly] public float CellSize;

        public void Execute(Entity entity, in LocalTransform transform, in BoidVelocity velocity, in UnitRadius unitRadius, in FactionData faction)
        {
            long cellHash = SpatialHashUtility.ComputeHashFromPositionAndTeam(
                transform.Position, CellSize, faction.Team);

            SpatialMap.Add(cellHash, entity);

            SnapshotMap.TryAdd(entity, new BoidSnapshot
            {
                Position = transform.Position,
                Velocity = velocity.Value,
                Radius = unitRadius.Radius,
                Team = faction.Team
            });
        }
    }
}