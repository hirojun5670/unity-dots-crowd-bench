using Unity.Entities;

namespace UnityDotsCrowdLab.Features.Spawner
{
  public struct SpawnerData : IComponentData
  {
    public Entity Prefab;
    public Entity StartPoint;
    public Entity TargetPoint;
    public float Interval;
    public float Timer;
  }
}