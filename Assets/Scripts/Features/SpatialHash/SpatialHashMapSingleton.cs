using System;
using Unity.Collections;
using Unity.Entities;

namespace UnityDotsCrowdLab.Features.SpatialHash
{
    /// <summary>
    /// 空間ハッシュマップを保持するシングルトンコンポーネント
    /// </summary>
    public struct SpatialHashMapSingleton : IComponentData
    {
        public NativeParallelMultiHashMap<int, Entity> SpatialMap;
        public NativeParallelHashMap<Entity, BoidSnapshot> SnapshotMap;
    }
}