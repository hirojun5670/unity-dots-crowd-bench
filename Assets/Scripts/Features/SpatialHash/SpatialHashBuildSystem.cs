using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityDotsCrowdLab.Core.Job;
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
        private JobHandle sharedJobhandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // シングルトンEntityを作成し、コンポーネントとして登録
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new SpatialHashMapSingleton
            {
                SpatialMap = new NativeParallelMultiHashMap<int, Entity>(1000, Allocator.Persistent),
                SnapshotMap = new NativeParallelHashMap<Entity, BoidSnapshot>(1000, Allocator.Persistent),
            });

            // 依存関係を管理するシングルトンEntityを作成
            var sharedJobDependencyEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(sharedJobDependencyEntity, new SharedJobDependency { Handle = sharedJobhandle });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // 全てのジョブの終了を待つ
            if (SystemAPI.HasSingleton<SharedJobDependency>())
                SystemAPI.GetSingleton<SharedJobDependency>().Handle.Complete();
            state.Dependency.Complete();

            var singleton = SystemAPI.GetSingleton<SpatialHashMapSingleton>();
            singleton.SpatialMap.Dispose();
            singleton.SnapshotMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TargetingConfig>())
            {
                return;
            }

            var config = SystemAPI.GetSingleton<TargetingConfig>();

            var sharedJobhandle = SystemAPI.GetSingleton<SharedJobDependency>().Handle;
            var combined = JobHandle.CombineDependencies(state.Dependency, sharedJobhandle);

            var singleton = SystemAPI.GetSingletonRW<SpatialHashMapSingleton>();
            singleton.ValueRW.SpatialMap.Clear();
            singleton.ValueRW.SnapshotMap.Clear();
            int count = SystemAPI.QueryBuilder().WithAll<LocalTransform, BoidVelocity>().Build().CalculateEntityCount();
            // entity数に応じて容量を調整する
            int required = math.ceilpow2(math.max(1, count));
            if (singleton.ValueRW.SnapshotMap.Capacity < required)
                singleton.ValueRW.SnapshotMap.Capacity = required;

            if (singleton.ValueRW.SpatialMap.Capacity < required)
                singleton.ValueRW.SpatialMap.Capacity = required;

            var handle = new BuildSpatialHashJob
            {
                SpatialMap = singleton.ValueRW.SpatialMap.AsParallelWriter(),
                SnapshotMap = singleton.ValueRW.SnapshotMap.AsParallelWriter(),
                CellSize = config.CellSize,
            }.ScheduleParallel(combined);

            state.Dependency = handle;
            SystemAPI.SetSingleton(new SharedJobDependency { Handle = handle });
        }
    }

    [BurstCompile]
    public struct BoidSnapshot
    {
        public float3 Position;
        public float3 Velocity;
        public int Team;
    }

    // 空間ハッシュを構築するジョブ
    [BurstCompile]
    public partial struct BuildSpatialHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, Entity>.ParallelWriter SpatialMap;
        public NativeParallelHashMap<Entity, BoidSnapshot>.ParallelWriter SnapshotMap;
        [ReadOnly] public float CellSize;

        public void Execute(Entity entity, in LocalTransform transform, in BoidVelocity velocity, in FactionData faction)
        {
            int cellHash = SpatialHashUtility.ComputeHash(
                SpatialHashUtility.ComputeCellCoord(transform.Position, CellSize));

            SpatialMap.Add(cellHash, entity);

            SnapshotMap.TryAdd(entity, new BoidSnapshot
            {
                Position = transform.Position,
                Velocity = velocity.Value,
                Team = faction.Team
            });
        }
    }
}