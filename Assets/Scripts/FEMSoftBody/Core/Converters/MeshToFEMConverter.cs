using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace FEMSoftBody
{
    /// <summary>
    /// 网格转换配置
    /// </summary>
    [System.Serializable]
    public class FEMConversionSettings
    {
        [Header("四面体化设置")]
        [Tooltip("体素化分辨率")]
        public int voxelResolution = 16;

        [Tooltip("表面采样密度")]
        public float surfaceSamplingDensity = 1.0f;

        [Tooltip("内部点生成密度")]
        public float interiorPointDensity = 0.5f;

        [Header("材料属性")]
        [Tooltip("杨氏模量")]
        public float youngModulus = 1000000f;

        [Tooltip("泊松比")]
        public float poissonRatio = 0.3f;

        [Tooltip("密度")]
        public float density = 1000f;

        [Header("质量分布")]
        [Tooltip("节点质量计算方式")]
        public MassDistributionMode massDistribution = MassDistributionMode.Uniform;

        [Tooltip("固定边界节点")]
        public bool fixBoundaryNodes = false;

        [Tooltip("边界固定阈值")]
        public float boundaryFixThreshold = 0.1f;
    }

    public enum MassDistributionMode
    {
        Uniform,        // 均匀分布
        VolumeWeighted, // 按体积加权
        SurfaceWeighted // 按表面积加权
    }

    /// <summary>
    /// Mesh到FEM数据的转换器
    /// </summary>
    public static class MeshToFEMConverter
    {
        /// <summary>
        /// 转换Mesh为FEM数据
        /// </summary>
        public static FEMGeometryData ConvertMesh(Mesh mesh, FEMConversionSettings settings)
        {
            if (mesh == null)
            {
                Debug.LogError("Mesh为空，无法转换");
                return null;
            }

            Debug.Log($"开始转换Mesh: {mesh.name}, 顶点数: {mesh.vertexCount}, 三角形数: {mesh.triangles.Length / 3}");

            // 获取网格数据
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // 转换为FEM数据
            return ConvertMeshData(vertices, triangles, settings);
        }

        /// <summary>
        /// 转换SkinnedMeshRenderer为FEM数据
        /// </summary>
        public static FEMGeometryData ConvertSkinnedMesh(SkinnedMeshRenderer skinnedMesh, FEMConversionSettings settings)
        {
            if (skinnedMesh == null)
            {
                Debug.LogError("SkinnedMeshRenderer为空，无法转换");
                return null;
            }

            // 烘焙当前状态的网格
            Mesh bakedMesh = new Mesh();
            skinnedMesh.BakeMesh(bakedMesh);

            Debug.Log($"开始转换SkinnedMesh: {skinnedMesh.name}, 烘焙后顶点数: {bakedMesh.vertexCount}");

            var result = ConvertMesh(bakedMesh, settings);

            // 清理临时网格
            Object.DestroyImmediate(bakedMesh);

            return result;
        }

        /// <summary>
        /// 核心转换方法
        /// </summary>
        private static FEMGeometryData ConvertMeshData(Vector3[] vertices, int[] triangles, FEMConversionSettings settings)
        {
            var geometryData = new FEMGeometryData();

            // 1. 体素化和四面体化
            Debug.Log("步骤1: 体素化网格...");
            var voxelData = VoxelizeMesh(vertices, triangles, settings.voxelResolution);

            // 2. 生成FEM节点
            Debug.Log("步骤2: 生成FEM节点...");
            var nodes = GenerateFEMNodes(voxelData, vertices, settings);

            // 3. 生成四面体
            Debug.Log("步骤3: 生成四面体单元...");
            var tetrahedra = GenerateTetrahedra(nodes, settings);

            // 4. 识别表面三角形
            Debug.Log("步骤4: 识别表面三角形...");
            var surfaceTriangles = GenerateSurfaceTriangles(nodes, tetrahedra, vertices, triangles);

            // 5. 计算质量分布
            Debug.Log("步骤5: 计算质量分布...");
            CalculateMassDistribution(ref nodes, tetrahedra, settings);

            // 6. 设置边界条件
            Debug.Log("步骤6: 设置边界条件...");
            ApplyBoundaryConditions(ref nodes, settings);

            // 7. 组装最终数据
            geometryData.nodes = nodes;
            geometryData.tetrahedra = tetrahedra;
            geometryData.surfaceTriangles = surfaceTriangles;
            geometryData.defaultYoungModulus = settings.youngModulus;
            geometryData.defaultPoissonRatio = settings.poissonRatio;
            geometryData.defaultDensity = settings.density;

            // 8. 更新统计信息
            geometryData.UpdateStatistics();

            Debug.Log($"转换完成! 节点: {geometryData.nodeCount}, 四面体: {geometryData.tetrahedronCount}, 表面三角形: {geometryData.surfaceTriangleCount}");
            Debug.Log($"总体积: {geometryData.totalVolume:F3}, 总质量: {geometryData.totalMass:F3}");

            return geometryData;
        }

        /// <summary>
        /// 体素化网格
        /// </summary>
        private static VoxelGrid VoxelizeMesh(Vector3[] vertices, int[] triangles, int resolution)
        {
            // 计算包围盒
            Bounds bounds = GeometryUtility.CalculateBounds(vertices, Matrix4x4.identity);
            bounds.Expand(0.1f); // 稍微扩展边界

            var voxelGrid = new VoxelGrid(bounds, resolution);

            // 对每个三角形进行体素化
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                voxelGrid.VoxelizeTriangle(v0, v1, v2);
            }

            // 填充内部体素
            voxelGrid.FloodFillInterior();

            return voxelGrid;
        }

        /// <summary>
        /// 生成FEM节点
        /// </summary>
        private static FEMNodeData[] GenerateFEMNodes(VoxelGrid voxelGrid, Vector3[] originalVertices, FEMConversionSettings settings)
        {
            List<FEMNodeData> nodes = new List<FEMNodeData>();

            // 从体素网格生成节点
            var voxelNodes = voxelGrid.GenerateNodes(settings.interiorPointDensity);

            foreach (var voxelNode in voxelNodes)
            {
                var node = new FEMNodeData(voxelNode.position, 1.0f, false);
                node.isSurface = voxelNode.isSurface;
                nodes.Add(node);
            }

            // 确保原始表面顶点被包含
            foreach (var vertex in originalVertices)
            {
                float3 pos = vertex;
                bool exists = nodes.Any(n => math.distancesq(n.position, pos) < 0.001f);

                if (!exists)
                {
                    var node = new FEMNodeData(pos, 1.0f, false);
                    node.isSurface = true;
                    nodes.Add(node);
                }
            }

            return nodes.ToArray();
        }

        /// <summary>
        /// 生成四面体单元
        /// </summary>
        private static FEMTetrahedronData[] GenerateTetrahedra(FEMNodeData[] nodes, FEMConversionSettings settings)
        {
            // 使用Delaunay四面体化
            var tetrahedralizer = new DelaunayTetrahedralizer();
            var tetrahedra = tetrahedralizer.Tetrahedralize(nodes);

            List<FEMTetrahedronData> femTetrahedra = new List<FEMTetrahedronData>();

            foreach (var tet in tetrahedra)
            {
                // 计算四面体属性
                float3 p0 = nodes[tet.x].position;
                float3 p1 = nodes[tet.y].position;
                float3 p2 = nodes[tet.z].position;
                float3 p3 = nodes[tet.w].position;

                float volume = TetrahedronUtility.CalculateVolume(p0, p1, p2, p3);

                if (volume > 1e-6f) // 过滤掉退化的四面体
                {
                    var invRestMatrix = TetrahedronUtility.CalculateInverseRestMatrix(p0, p1, p2, p3);

                    var femTet = new FEMTetrahedronData(
                        tet, invRestMatrix, volume,
                        settings.youngModulus, settings.poissonRatio, settings.density
                    );

                    femTetrahedra.Add(femTet);
                }
            }

            return femTetrahedra.ToArray();
        }

        /// <summary>
        /// 生成表面三角形
        /// </summary>
        private static FEMSurfaceTriangle[] GenerateSurfaceTriangles(FEMNodeData[] nodes, FEMTetrahedronData[] tetrahedra, Vector3[] originalVertices, int[] originalTriangles)
        {
            List<FEMSurfaceTriangle> surfaceTriangles = new List<FEMSurfaceTriangle>();

            // 从四面体中提取表面
            var surfaceExtractor = new SurfaceExtractor();
            var surfaceFaces = surfaceExtractor.ExtractSurface(tetrahedra);

            foreach (var face in surfaceFaces)
            {
                float3 p0 = nodes[face.x].position;
                float3 p1 = nodes[face.y].position;
                float3 p2 = nodes[face.z].position;

                float3 normal = math.normalize(math.cross(p1 - p0, p2 - p0));
                float area = 0.5f * math.length(math.cross(p1 - p0, p2 - p0));

                var surfaceTriangle = new FEMSurfaceTriangle(face, normal, area);
                surfaceTriangles.Add(surfaceTriangle);
            }

            return surfaceTriangles.ToArray();
        }

        /// <summary>
        /// 计算质量分布
        /// </summary>
        private static void CalculateMassDistribution(ref FEMNodeData[] nodes, FEMTetrahedronData[] tetrahedra, FEMConversionSettings settings)
        {
            switch (settings.massDistribution)
            {
                case MassDistributionMode.Uniform:
                    float uniformMass = 1.0f;
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].mass = uniformMass;
                    }
                    break;

                case MassDistributionMode.VolumeWeighted:
                    // 将四面体质量分配给节点
                    float[] nodeMasses = new float[nodes.Length];

                    foreach (var tet in tetrahedra)
                    {
                        float tetMass = tet.volume * tet.density;
                        float nodeMass = tetMass / 4.0f; // 平均分配给4个节点

                        nodeMasses[tet.nodeIndices.x] += nodeMass;
                        nodeMasses[tet.nodeIndices.y] += nodeMass;
                        nodeMasses[tet.nodeIndices.z] += nodeMass;
                        nodeMasses[tet.nodeIndices.w] += nodeMass;
                    }

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].mass = math.max(nodeMasses[i], 0.001f); // 确保最小质量
                    }
                    break;
            }
        }

        /// <summary>
        /// 应用边界条件
        /// </summary>
        private static void ApplyBoundaryConditions(ref FEMNodeData[] nodes, FEMConversionSettings settings)
        {
            if (!settings.fixBoundaryNodes) return;

            // 计算包围盒
            float3 minPos = nodes[0].position;
            float3 maxPos = nodes[0].position;

            foreach (var node in nodes)
            {
                minPos = math.min(minPos, node.position);
                maxPos = math.max(maxPos, node.position);
            }

            float threshold = settings.boundaryFixThreshold;

            for (int i = 0; i < nodes.Length; i++)
            {
                // 固定底部节点
                if (nodes[i].position.y < minPos.y + threshold)
                {
                    nodes[i].isFixed = true;
                }
            }
        }
    }
}