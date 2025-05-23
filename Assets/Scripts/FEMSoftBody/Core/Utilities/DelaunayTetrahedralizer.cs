using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace FEMSoftBody
{
    /// <summary>
    /// Delaunay四面体化器（简化实现）
    /// </summary>
    public class DelaunayTetrahedralizer
    {
        /// <summary>
        /// 对节点集合进行四面体化
        /// </summary>
        public List<int4> Tetrahedralize(FEMNodeData[] nodes)
        {
            if (nodes.Length < 4)
            {
                UnityEngine.Debug.LogWarning("节点数量少于4个，无法生成四面体");
                return new List<int4>();
            }

            // 简化版：使用凸包 + 内部点插入
            // 这里实现一个基础版本，实际项目中建议使用专业的四面体化库

            var tetrahedra = new List<int4>();

            // 1. 找到凸包
            var convexHull = ComputeConvexHull3D(nodes);

            // 2. 对凸包进行初始三角剖分
            var initialTets = TriangulateConvexHull(convexHull, nodes);
            tetrahedra.AddRange(initialTets);

            // 3. 插入内部点（简化版本）
            var interiorPoints = new List<int>();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!convexHull.Contains(i))
                {
                    interiorPoints.Add(i);
                }
            }

            // 简单插入策略：为每个内部点找到包含它的四面体并分割
            foreach (int pointIndex in interiorPoints)
            {
                InsertPointIntoTetrahedralization(pointIndex, nodes, ref tetrahedra);
            }

            return tetrahedra;
        }

        #region 辅助方法

        private List<int> ComputeConvexHull3D(FEMNodeData[] nodes)
        {
            // 简化版凸包算法 - 实际应用中建议使用QuickHull等专业算法
            var hull = new List<int>();

            // 找到极值点
            int minX = 0, maxX = 0, minY = 0, maxY = 0, minZ = 0, maxZ = 0;

            for (int i = 1; i < nodes.Length; i++)
            {
                if (nodes[i].position.x < nodes[minX].position.x) minX = i;
                if (nodes[i].position.x > nodes[maxX].position.x) maxX = i;
                if (nodes[i].position.y < nodes[minY].position.y) minY = i;
                if (nodes[i].position.y > nodes[maxY].position.y) maxY = i;
                if (nodes[i].position.z < nodes[minZ].position.z) minZ = i;
                if (nodes[i].position.z > nodes[maxZ].position.z) maxZ = i;
            }

            hull.Add(minX);
            if (!hull.Contains(maxX)) hull.Add(maxX);
            if (!hull.Contains(minY)) hull.Add(minY);
            if (!hull.Contains(maxY)) hull.Add(maxY);
            if (!hull.Contains(minZ)) hull.Add(minZ);
            if (!hull.Contains(maxZ)) hull.Add(maxZ);

            // 添加更多的表面点
            for (int i = 0; i < nodes.Length && hull.Count < 12; i++)
            {
                if (nodes[i].isSurface && !hull.Contains(i))
                {
                    hull.Add(i);
                }
            }

            return hull;
        }

        private List<int4> TriangulateConvexHull(List<int> hullIndices, FEMNodeData[] nodes)
        {
            var tetrahedra = new List<int4>();

            if (hullIndices.Count < 4) return tetrahedra;

            // 简单的扇形三角剖分
            for (int i = 1; i < hullIndices.Count - 2; i++)
            {
                for (int j = i + 1; j < hullIndices.Count - 1; j++)
                {
                    for (int k = j + 1; k < hullIndices.Count; k++)
                    {
                        int4 tet = new int4(hullIndices[0], hullIndices[i], hullIndices[j], hullIndices[k]);

                        // 检查四面体是否有效
                        if (IsValidTetrahedron(tet, nodes))
                        {
                            tetrahedra.Add(tet);
                        }
                    }
                }
            }

            return tetrahedra;
        }

        private void InsertPointIntoTetrahedralization(int pointIndex, FEMNodeData[] nodes, ref List<int4> tetrahedra)
        {
            float3 point = nodes[pointIndex].position;

            // 找到包含该点的四面体
            for (int i = 0; i < tetrahedra.Count; i++)
            {
                if (IsPointInsideTetrahedron(point, tetrahedra[i], nodes))
                {
                    // 移除该四面体并创建新的四面体
                    int4 oldTet = tetrahedra[i];
                    tetrahedra.RemoveAt(i);

                    // 创建4个新四面体
                    var newTets = new int4[]
                    {
                        new int4(pointIndex, oldTet.x, oldTet.y, oldTet.z),
                        new int4(pointIndex, oldTet.x, oldTet.y, oldTet.w),
                        new int4(pointIndex, oldTet.x, oldTet.z, oldTet.w),
                        new int4(pointIndex, oldTet.y, oldTet.z, oldTet.w)
                    };

                    foreach (var newTet in newTets)
                    {
                        if (IsValidTetrahedron(newTet, nodes))
                        {
                            tetrahedra.Add(newTet);
                        }
                    }

                    break;
                }
            }
        }

        private bool IsValidTetrahedron(int4 tet, FEMNodeData[] nodes)
        {
            float3 p0 = nodes[tet.x].position;
            float3 p1 = nodes[tet.y].position;
            float3 p2 = nodes[tet.z].position;
            float3 p3 = nodes[tet.w].position;

            return TetrahedronUtility.IsValidTetrahedron(p0, p1, p2, p3);
        }

        private bool IsPointInsideTetrahedron(float3 point, int4 tet, FEMNodeData[] nodes)
        {
            float3 p0 = nodes[tet.x].position;
            float3 p1 = nodes[tet.y].position;
            float3 p2 = nodes[tet.z].position;
            float3 p3 = nodes[tet.w].position;

            // 使用重心坐标判断
            return IsPointInsideTetrahedronBarycentric(point, p0, p1, p2, p3);
        }

        private bool IsPointInsideTetrahedronBarycentric(float3 point, float3 p0, float3 p1, float3 p2, float3 p3)
        {
            float3x3 matrix = new float3x3(
                p1 - p0,
                p2 - p0,
                p3 - p0
            );

            float det = math.determinant(matrix);
            if (math.abs(det) < 1e-10f) return false;

            float3 v = point - p0;
            float3 barycentric = math.mul(math.inverse(matrix), v);

            return barycentric.x >= 0 && barycentric.y >= 0 && barycentric.z >= 0 &&
                   (barycentric.x + barycentric.y + barycentric.z) <= 1.0f;
        }

        #endregion
    }
}