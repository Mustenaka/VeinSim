using Unity.Mathematics;
using UnityEngine;

namespace FEMSoftBody
{
    /// <summary>
    /// FEM�ڵ�����
    /// </summary>
    [System.Serializable]
    public struct FEMNodeData
    {
        public float3 position; // ��ǰλ��
        public float3 restPosition; // ��Ϣλ��
        public float3 velocity; // �ٶ�
        public float3 force; // ����
        public float mass; // ����
        public bool isFixed; // �Ƿ�̶�
        public bool isSurface; // �Ƿ�Ϊ����ڵ�
        public int originalVertexIndex; // ԭʼ��������

        public FEMNodeData(float3 pos, float mass = 1.0f, bool isFixed = false)
        {
            position = pos;
            restPosition = pos;
            velocity = float3.zero;
            force = float3.zero;
            this.mass = mass;
            this.isFixed = isFixed;
            isSurface = false;
            originalVertexIndex = -1;
        }
    }

    /// <summary>
    /// FEM�����嵥Ԫ����
    /// </summary>
    [System.Serializable]
    public struct FEMTetrahedronData
    {
        public int4 nodeIndices; // �ĸ��ڵ������
        public float volume; // ��Ϣ���
        public float3x3 invRestMatrix; // ��Ϣ״̬�����
        public float youngModulus; // ����ģ��
        public float poissonRatio; // ���ɱ�
        public float density; // �ܶ�

        public FEMTetrahedronData(int4 indices, float3x3 invMatrix, float vol,
            float young = 1000000f, float poisson = 0.3f, float dens = 1000f)
        {
            nodeIndices = indices;
            invRestMatrix = invMatrix;
            volume = vol;
            youngModulus = young;
            poissonRatio = poisson;
            density = dens;
        }
    }

    /// <summary>
    /// FEM�������������ݣ�������Ⱦ����ײ��
    /// </summary>
    [System.Serializable]
    public struct FEMSurfaceTriangle
    {
        public int3 nodeIndices; // ��������ڵ�����
        public float3 normal; // ������
        public float area; // ���
        public int2 neighborTets; // ���ڵ���������������

        public FEMSurfaceTriangle(int3 indices, float3 norm, float area)
        {
            nodeIndices = indices;
            normal = norm;
            this.area = area;
            neighborTets = new int2(-1, -1);
        }
    }

    /// <summary>
    /// ������FEM��������
    /// </summary>
    [System.Serializable]
    public class FEMGeometryData
    {
        [Header("�ڵ�����")] public FEMNodeData[] nodes;

        [Header("����������")] public FEMTetrahedronData[] tetrahedra;

        [Header("��������")] public FEMSurfaceTriangle[] surfaceTriangles;
        public int[] surfaceVertexMap; // ���涥�㵽FEM�ڵ��ӳ��

        [Header("ͳ����Ϣ")] public int nodeCount;
        public int tetrahedronCount;
        public int surfaceTriangleCount;
        public float totalVolume;
        public float totalMass;
        public Bounds bounds;

        [Header("��������")] public float defaultYoungModulus = 1000000f;
        public float defaultPoissonRatio = 0.3f;
        public float defaultDensity = 1000f;

        public FEMGeometryData()
        {
            nodes = new FEMNodeData[0];
            tetrahedra = new FEMTetrahedronData[0];
            surfaceTriangles = new FEMSurfaceTriangle[0];
            surfaceVertexMap = new int[0];
        }

        /// <summary>
        /// ��ȡ��������ĸ��ڵ�λ��
        /// </summary>
        public void GetTetrahedronPositions(int tetIndex, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var tet = tetrahedra[tetIndex];
            p0 = nodes[tet.nodeIndices.x].position;
            p1 = nodes[tet.nodeIndices.y].position;
            p2 = nodes[tet.nodeIndices.z].position;
            p3 = nodes[tet.nodeIndices.w].position;
        }

        /// <summary>
        /// ����ͳ����Ϣ
        /// </summary>
        public void UpdateStatistics()
        {
            nodeCount = nodes?.Length ?? 0;
            tetrahedronCount = tetrahedra?.Length ?? 0;
            surfaceTriangleCount = surfaceTriangles?.Length ?? 0;

            // ���������������
            totalVolume = 0f;
            totalMass = 0f;

            if (tetrahedra != null)
            {
                foreach (var tet in tetrahedra)
                {
                    totalVolume += tet.volume;
                }
            }

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    totalMass += node.mass;
                }

                // �����Χ��
                if (nodes.Length > 0)
                {
                    float3 min = nodes[0].position;
                    float3 max = nodes[0].position;
                    foreach (var node in nodes)
                    {
                        min = math.min(min, node.position);
                        max = math.max(max, node.position);
                    }

                    bounds = new Bounds((max + min) * 0.5f, max - min);
                }
            }
        }
    }
}