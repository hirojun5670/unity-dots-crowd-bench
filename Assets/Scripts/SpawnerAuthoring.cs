using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpawnerData : IComponentData
{
    public Entity Prefab;
    public float Interval;
    public float3 Destination;
    public float Timer;
}

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnInterval = 1f; // 何秒ごとにスポーンするか
    public Transform TargetPoint;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnerData
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Interval = authoring.SpawnInterval,
                Destination = authoring.TargetPoint.position,
                Timer = 0f
            });
        }
    }
}
