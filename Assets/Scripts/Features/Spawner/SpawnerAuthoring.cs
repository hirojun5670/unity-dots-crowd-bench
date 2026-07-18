using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnityDotsCrowdLab.Features.Spawner
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float SpawnInterval = 1f; // 何秒ごとにスポーンするか
        public int FactionID = 0;// チームID
        public Transform StartPoint;
        public Transform TargetPoint;

        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnerData
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    FactionID = authoring.FactionID,
                    StartPoint = GetEntity(authoring.StartPoint, TransformUsageFlags.Dynamic),
                    TargetPoint = GetEntity(authoring.TargetPoint, TransformUsageFlags.Dynamic),
                    Interval = authoring.SpawnInterval,
                    Timer = 0f
                });
            }
        }
    }
}
