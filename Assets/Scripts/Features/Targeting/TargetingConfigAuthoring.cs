using Unity.Entities;
using UnityEngine;

namespace UnityDotsCrowdLab.Features.Targeting
{

    public class TargetingConfigAuthoring : MonoBehaviour
    {
        public TargetingMode Mode = TargetingMode.BruteForce;
        public float CellSize = 3f; // AttackPower.Rangeの目安値をデフォルトに

        [Header("Gizmo設定")]
        public bool ShowGrid = true;
        public float GridRange = 30f; // 原点からどれだけの範囲を描画するか
        public Color GridColor = new Color(0f, 1f, 1f, 0.3f);

        // CellSizeのgizmo表示
        void OnDrawGizmosSelected()
        {
            if (!ShowGrid || CellSize <= 0f) return;

            Gizmos.color = GridColor;

            int lineCount = Mathf.CeilToInt(GridRange / CellSize);

            for (int i = -lineCount; i <= lineCount; i++)
            {
                float x = i * CellSize;
                Gizmos.DrawLine(
                    new Vector3(x, 0, -GridRange),
                    new Vector3(x, 0, GridRange)
                );
            }

            for (int i = -lineCount; i <= lineCount; i++)
            {
                float z = i * CellSize;
                Gizmos.DrawLine(
                    new Vector3(-GridRange, 0, z),
                    new Vector3(GridRange, 0, z)
                );
            }
        }

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
