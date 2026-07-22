using Unity.Mathematics;

namespace UnityDotsCrowdLab.Core.Spatial
{
    /// <summary>
    /// 空間ハッシュを利用するための汎用ユーティリティクラス
    /// 座標をセル座標に変換し、ハッシュ値を生成する機能を提供
    /// </summary>
    public static class SpatialHashUtility
    {
        /// <summary>
        /// ワールド座標をセルのインデックスに変換
        /// </summary>
        /// <param name="position">ワールド座標</param>
        /// <param name="cellSize">セルサイズ</param>
        /// <returns>セルの3D座標</returns>
        public static int3 ComputeCellCoord(float3 position, float cellSize)
        {
            return new int3(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize),
                (int)math.floor(position.z / cellSize)
            );
        }

        /// <summary>
        /// セルの座標をハッシュ値に変換
        /// </summary>
        /// <param name="cellCoord">セル座標</param>
        /// <returns>ハッシュ値</returns>
        public static int ComputeHash(int3 cellCoord)
        {
            const int p1 = 73856093;
            const int p2 = 19349663;
            const int p3 = 83492791;
            return (cellCoord.x * p1) ^ (cellCoord.y * p2) ^ (cellCoord.z * p3);
        }

        /// <summary>
        /// ワールド座標から直接ハッシュ値を計算
        /// </summary>
        /// <param name="position">ワールド座標</param>
        /// <param name="cellSize">セルサイズ</param>
        /// <returns>ハッシュ値</returns>
        public static int ComputeHashFromPosition(float3 position, float cellSize)
        {
            return ComputeHash(ComputeCellCoord(position, cellSize));
        }
    }
}
