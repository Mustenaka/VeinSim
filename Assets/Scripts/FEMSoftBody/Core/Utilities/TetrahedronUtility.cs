using Unity.Mathematics;
using UnityEngine;

namespace FEMSoftBody
{
    /// <summary>
    /// 四面体几何计算工具
    /// </summary>
    public static class TetrahedronUtility
    {
        /// <summary>
        /// 计算四面体体积
        /// </summary>
        public static float CalculateVolume(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3 a = p1 - p0;
            float3 b = p2 - p0;
            float3 c = p3 - p0;

            return math.abs(math.dot(a, math.cross(b, c))) / 6.0f;
        }

        /// <summary>
        /// 计算静息状态逆矩阵
        /// </summary>
        public static float3x3 CalculateInverseRestMatrix(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3x3 restMatrix = new float3x3(
                p1 - p0,
                p2 - p0,
                p3 - p0
            );

            return math.inverse(restMatrix);
        }

        /// <summary>
        /// 计算四面体质心
        /// </summary>
        public static float3 CalculateCentroid(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            return (p0 + p1 + p2 + p3) * 0.25f;
        }

        /// <summary>
        /// 检查四面体是否有效（非退化）
        /// </summary>
        public static bool IsValidTetrahedron(float3 p0, float3 p1, float3 p2, float3 p3, float minVolume = 1e-6f)
        {
            float volume = CalculateVolume(p0, p1, p2, p3);
            return volume > minVolume;
        }

        /// <summary>
        /// 计算四面体外接球半径
        /// </summary>
        public static float CalculateCircumradius(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3 a = p1 - p0;
            float3 b = p2 - p0;
            float3 c = p3 - p0;

            float3x3 matrix = new float3x3(a, b, c);
            float det = math.determinant(matrix);

            if (math.abs(det) < 1e-10f) return float.MaxValue; // 退化情况

            float3 cross_bc = math.cross(b, c);
            float3 cross_ca = math.cross(c, a);
            float3 cross_ab = math.cross(a, b);

            float3 circumcenter = (math.dot(a, a) * cross_bc + math.dot(b, b) * cross_ca + math.dot(c, c) * cross_ab) / (2.0f * det);

            return math.length(circumcenter);
        }
    }
}