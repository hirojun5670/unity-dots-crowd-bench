using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace UnityDotsCrowdLab.Features.SpatialHash
{

    /// <summary>
    /// 空間ハッシュマップを保持するシングルトンコンポーネント
    /// </summary>
    public struct SpatialHashMapSingleton : IComponentData
    {
        public int CurrentReadBufferIndex;

        public JobHandle Buffer0JobHandle;
        public JobHandle Buffer1JobHandle;

        public NativeParallelMultiHashMap<int, Entity> SpatialMapBuffer0;
        public NativeParallelHashMap<Entity, BoidSnapshot> SnapshotMapBuffer0;

        public NativeParallelMultiHashMap<int, Entity> SpatialMapBuffer1;
        public NativeParallelHashMap<Entity, BoidSnapshot> SnapshotMapBuffer1;


        public void SwapBuffers()
        {
            CurrentReadBufferIndex = 1 - CurrentReadBufferIndex;
        }

        public void CompleteNextWriteBufferDependency()
        {
            if (CurrentReadBufferIndex == 0)
                Buffer1JobHandle.Complete();
            else
                Buffer0JobHandle.Complete();
        }

        public void SetReadBufferCompleteHandle(JobHandle handle)
        {
            if (CurrentReadBufferIndex == 0)
                Buffer0JobHandle = handle;
            else
                Buffer1JobHandle = handle;
        }

        public NativeParallelMultiHashMap<int, Entity> SpatialMap
        {
            get
            {
                return CurrentReadBufferIndex == 0 ? SpatialMapBuffer0 : SpatialMapBuffer1;
            }
        }
        public NativeParallelHashMap<Entity, BoidSnapshot> SnapshotMap
        {
            get
            {
                return CurrentReadBufferIndex == 0 ? SnapshotMapBuffer0 : SnapshotMapBuffer1;
            }
        }

        public NativeParallelMultiHashMap<int, Entity> WriteSpatialMap
        {
            get
            {
                return CurrentReadBufferIndex == 0 ? SpatialMapBuffer1 : SpatialMapBuffer0;
            }
        }
        public NativeParallelHashMap<Entity, BoidSnapshot> WriteSnapshotMap
        {
            get
            {
                return CurrentReadBufferIndex == 0 ? SnapshotMapBuffer1 : SnapshotMapBuffer0;
            }
        }

        public void SetWriteBufferCapacity(int capacity)
        {
            if (CurrentReadBufferIndex == 0)
            {
                if (SpatialMapBuffer1.Capacity < capacity)
                    SpatialMapBuffer1.Capacity = capacity;
                if (SnapshotMapBuffer1.Capacity < capacity)
                    SnapshotMapBuffer1.Capacity = capacity;
            }
            else
            {
                if (SpatialMapBuffer0.Capacity < capacity)
                    SpatialMapBuffer0.Capacity = capacity;
                if (SnapshotMapBuffer0.Capacity < capacity)
                    SnapshotMapBuffer0.Capacity = capacity;
            }
        }

    }
}