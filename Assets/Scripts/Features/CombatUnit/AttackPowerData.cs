using Unity.Entities;

namespace UnityDotsCrowdLab.Features.CombatUnit
{
    public struct AttackPowerData : IComponentData
    {
        public float Damage;
        public float Range;      // 攻撃射程
        public float Cooldown;   // 攻撃間隔
        public float Timer;      // 内部タイマー
    }
}
