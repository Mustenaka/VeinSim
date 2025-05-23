using Unity.Mathematics;
using UnityEngine;

namespace FEMSoftBody
{
    /// <summary>
    /// FEM节点数据
    /// </summary>
    [System.Serializable]
    public struct FEMNodeData
    {
        public float3 position; // 当前位置
        public float3 restPosition; // 静息位置
        public float3 velocity; // 速度
        public float3 force; // 受力
        public float mass; // 质量
        public bool isFixed; // 是否固定
        public bool isSurface; // 是否为表面节点
        public int originalVertexIndex; // 原始顶点索引

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
    /// FEM四面体单元数据
    /// </summary>
    [System.Serializable]
    public struct FEMTetrahedronData
    {
        public int4 nodeIndices; // 四个节点的索引
        public float volume; // 静息体积
        public float3x3 invRestMatrix; // 静息状态逆矩阵
        public float youngModulus; // 杨氏模量
        public float poissonRatio; // 泊松比
        public float density; // 密度

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
    /// FEM表面三角形数据（用于渲染和碰撞）
    /// </summary>
    [System.Serializable]
    public struct FEMSurfaceTriangle
    {
        public int3 nodeIndices; // 三个表面节点索引
        public float3 normal; // 法向量
        public float area; // 面积
        public int2 neighborTets; // 相邻的两个四面体索引

        public FEMSurfaceTriangle(int3 indices, float3 norm, float area)
        {
            nodeIndices = indices;
            normal = norm;
            this.area = area;
            neighborTets = new int2(-1, -1);
        }
    }

    /// <summary>
    /// 完整的FEM几何数据
    /// </summary>
    [System.Serializable]
    public class FEMGeometryData
    {
        [Header("节点数据")] public FEMNodeData[] nodes;

        [Header("四面体数据")] public FEMTetrahedronData[] tetrahedra;

        [Header("表面数据")] public FEMSurfaceTriangle[] surfaceTriangles;
        public int[] surfaceVertexMap; // 表面顶点到FEM节点的映射

        [Header("统计信息")] public int nodeCount;
        public int tetrahedronCount;
        public int surfaceTriangleCount;
        public float totalVolume;
        public float totalMass;
        public Bounds bounds;

        [Header("材料属性")] public float defaultYoungModulus = 1000000f;
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
        /// 获取四面体的四个节点位置
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
        /// 更新统计信息
        /// </summary>
        public void UpdateStatistics()
        {
            nodeCount = nodes?.Length ?? 0;
            tetrahedronCount = tetrahedra?.Length ?? 0;
            surfaceTriangleCount = surfaceTriangles?.Length ?? 0;

            // 计算总体积和质量
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

                // 计算包围盒
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