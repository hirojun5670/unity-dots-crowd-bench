using Unity.Entities;
using UnityEngine;

namespace UnityDotsCrowdLab.Features.Targeting
{

    public class TargetingConfigAuthoring : MonoBehaviour
    {
        public TargetingMode Mode = TargetingMode.BruteForce;
        public float CellSize = 3f; // AttackPower.Rangeの目安値をデフォルトに

        class Baker : Baker<TargetingConfigAuthoring>
        {
            public override void Bake(TargetingConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None); // 位置情報不要なのでNone
                AddComponent(entity, new TargetingConfig
                {
                    Mode = authoring.Mode,
                    CellSize = authoring.CellSize
                });
            }
        }
    }
}
