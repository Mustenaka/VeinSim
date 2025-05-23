using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace FEMSoftBody
{
    /// <summary>
    /// Delaunay�����廯������ʵ�֣�
    /// </summary>
    public class DelaunayTetrahedralizer
    {
        /// <summary>
        /// �Խڵ㼯�Ͻ��������廯
        /// </summary>
        public List<int4> Tetrahedralize(FEMNodeData[] nodes)
        {
            if (nodes.Length < 4)
            {
                UnityEngine.Debug.LogWarning("�ڵ���������4�����޷�����������");
                return new List<int4>();
            }

            // �򻯰棺ʹ��͹�� + �ڲ������
            // ����ʵ��һ�������汾��ʵ����Ŀ�н���ʹ��רҵ�������廯��

            var tetrahedra = new List<int4>();

            // 1. �ҵ�͹��
            var convexHull = ComputeConvexHull3D(nodes);

            // 2. ��͹�����г�ʼ�����ʷ�
            var initialTets = TriangulateConvexHull(convexHull, nodes);
            tetrahedra.AddRange(initialTets);

            // 3. �����ڲ��㣨�򻯰汾��
            var interiorPoints = new List<int>();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!convexHull.Contains(i))
                {
                    interiorPoints.Add(i);
                }
            }

            // �򵥲�����ԣ�Ϊÿ���ڲ����ҵ��������������岢�ָ�
            foreach (int pointIndex in interiorPoints)
            {
                InsertPointIntoTetrahedralization(pointIndex, nodes, ref tetrahedra);
            }

            return tetrahedra;
        }

        #region ��������

        private List<int> ComputeConvexHull3D(FEMNodeData[] nodes)
        {
            // �򻯰�͹���㷨 - ʵ��Ӧ���н���ʹ��QuickHull��רҵ�㷨
            var hull = new List<int>();

            // �ҵ���ֵ��
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

            // ��Ӹ���ı����
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

            // �򵥵����������ʷ�
            for (int i = 1; i < hullIndices.Count - 2; i++)
            {
                for (int j = i + 1; j < hullIndices.Count - 1; j++)
                {
                    for (int k = j + 1; k < hullIndices.Count; k++)
                    {
                        int4 tet = new int4(hullIndices[0], hullIndices[i], hullIndices[j], hullIndices[k]);

                        // ����������Ƿ���Ч
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

            // �ҵ������õ��������
            for (int i = 0; i < tetrahedra.Count; i++)
            {
                if (IsPointInsideTetrahedron(point, tetrahedra[i], nodes))
                {
                    // �Ƴ��������岢�����µ�������
                    int4 oldTet = tetrahedra[i];
                    tetrahedra.RemoveAt(i);

                    // ����4����������
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

            // ʹ�����������ж�
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