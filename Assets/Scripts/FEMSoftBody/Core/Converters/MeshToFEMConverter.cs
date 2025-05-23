using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace FEMSoftBody
{
    /// <summary>
    /// ����ת������
    /// </summary>
    [System.Serializable]
    public class FEMConversionSettings
    {
        [Header("�����廯����")]
        [Tooltip("���ػ��ֱ���")]
        public int voxelResolution = 16;

        [Tooltip("��������ܶ�")]
        public float surfaceSamplingDensity = 1.0f;

        [Tooltip("�ڲ��������ܶ�")]
        public float interiorPointDensity = 0.5f;

        [Header("��������")]
        [Tooltip("����ģ��")]
        public float youngModulus = 1000000f;

        [Tooltip("���ɱ�")]
        public float poissonRatio = 0.3f;

        [Tooltip("�ܶ�")]
        public float density = 1000f;

        [Header("�����ֲ�")]
        [Tooltip("�ڵ��������㷽ʽ")]
        public MassDistributionMode massDistribution = MassDistributionMode.Uniform;

        [Tooltip("�̶��߽�ڵ�")]
        public bool fixBoundaryNodes = false;

        [Tooltip("�߽�̶���ֵ")]
        public float boundaryFixThreshold = 0.1f;
    }

    public enum MassDistributionMode
    {
        Uniform,        // ���ȷֲ�
        VolumeWeighted, // �������Ȩ
        SurfaceWeighted // ���������Ȩ
    }

    /// <summary>
    /// Mesh��FEM���ݵ�ת����
    /// </summary>
    public static class MeshToFEMConverter
    {
        /// <summary>
        /// ת��MeshΪFEM����
        /// </summary>
        public static FEMGeometryData ConvertMesh(Mesh mesh, FEMConversionSettings settings)
        {
            if (mesh == null)
            {
                Debug.LogError("MeshΪ�գ��޷�ת��");
                return null;
            }

            Debug.Log($"��ʼת��Mesh: {mesh.name}, ������: {mesh.vertexCount}, ��������: {mesh.triangles.Length / 3}");

            // ��ȡ��������
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // ת��ΪFEM����
            return ConvertMeshData(vertices, triangles, settings);
        }

        /// <summary>
        /// ת��SkinnedMeshRendererΪFEM����
        /// </summary>
        public static FEMGeometryData ConvertSkinnedMesh(SkinnedMeshRenderer skinnedMesh, FEMConversionSettings settings)
        {
            if (skinnedMesh == null)
            {
                Debug.LogError("SkinnedMeshRendererΪ�գ��޷�ת��");
                return null;
            }

            // �決��ǰ״̬������
            Mesh bakedMesh = new Mesh();
            skinnedMesh.BakeMesh(bakedMesh);

            Debug.Log($"��ʼת��SkinnedMesh: {skinnedMesh.name}, �決�󶥵���: {bakedMesh.vertexCount}");

            var result = ConvertMesh(bakedMesh, settings);

            // ������ʱ����
            Object.DestroyImmediate(bakedMesh);

            return result;
        }

        /// <summary>
        /// ����ת������
        /// </summary>
        private static FEMGeometryData ConvertMeshData(Vector3[] vertices, int[] triangles, FEMConversionSettings settings)
        {
            var geometryData = new FEMGeometryData();

            // 1. ���ػ��������廯
            Debug.Log("����1: ���ػ�����...");
            var voxelData = VoxelizeMesh(vertices, triangles, settings.voxelResolution);

            // 2. ����FEM�ڵ�
            Debug.Log("����2: ����FEM�ڵ�...");
            var nodes = GenerateFEMNodes(voxelData, vertices, settings);

            // 3. ����������
            Debug.Log("����3: ���������嵥Ԫ...");
            var tetrahedra = GenerateTetrahedra(nodes, settings);

            // 4. ʶ�����������
            Debug.Log("����4: ʶ�����������...");
            var surfaceTriangles = GenerateSurfaceTriangles(nodes, tetrahedra, vertices, triangles);

            // 5. ���������ֲ�
            Debug.Log("����5: ���������ֲ�...");
            CalculateMassDistribution(ref nodes, tetrahedra, settings);

            // 6. ���ñ߽�����
            Debug.Log("����6: ���ñ߽�����...");
            ApplyBoundaryConditions(ref nodes, settings);

            // 7. ��װ��������
            geometryData.nodes = nodes;
            geometryData.tetrahedra = tetrahedra;
            geometryData.surfaceTriangles = surfaceTriangles;
            geometryData.defaultYoungModulus = settings.youngModulus;
            geometryData.defaultPoissonRatio = settings.poissonRatio;
            geometryData.defaultDensity = settings.density;

            // 8. ����ͳ����Ϣ
            geometryData.UpdateStatistics();

            Debug.Log($"ת�����! �ڵ�: {geometryData.nodeCount}, ������: {geometryData.tetrahedronCount}, ����������: {geometryData.surfaceTriangleCount}");
            Debug.Log($"�����: {geometryData.totalVolume:F3}, ������: {geometryData.totalMass:F3}");

            return geometryData;
        }

        /// <summary>
        /// ���ػ�����
        /// </summary>
        private static VoxelGrid VoxelizeMesh(Vector3[] vertices, int[] triangles, int resolution)
        {
            // �����Χ��
            Bounds bounds = GeometryUtility.CalculateBounds(vertices, Matrix4x4.identity);
            bounds.Expand(0.1f); // ��΢��չ�߽�

            var voxelGrid = new VoxelGrid(bounds, resolution);

            // ��ÿ�������ν������ػ�
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                voxelGrid.VoxelizeTriangle(v0, v1, v2);
            }

            // ����ڲ�����
            voxelGrid.FloodFillInterior();

            return voxelGrid;
        }

        /// <summary>
        /// ����FEM�ڵ�
        /// </summary>
        private static FEMNodeData[] GenerateFEMNodes(VoxelGrid voxelGrid, Vector3[] originalVertices, FEMConversionSettings settings)
        {
            List<FEMNodeData> nodes = new List<FEMNodeData>();

            // �������������ɽڵ�
            var voxelNodes = voxelGrid.GenerateNodes(settings.interiorPointDensity);

            foreach (var voxelNode in voxelNodes)
            {
                var node = new FEMNodeData(voxelNode.position, 1.0f, false);
                node.isSurface = voxelNode.isSurface;
                nodes.Add(node);
            }

            // ȷ��ԭʼ���涥�㱻����
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
        /// ���������嵥Ԫ
        /// </summary>
        private static FEMTetrahedronData[] GenerateTetrahedra(FEMNodeData[] nodes, FEMConversionSettings settings)
        {
            // ʹ��Delaunay�����廯
            var tetrahedralizer = new DelaunayTetrahedralizer();
            var tetrahedra = tetrahedralizer.Tetrahedralize(nodes);

            List<FEMTetrahedronData> femTetrahedra = new List<FEMTetrahedronData>();

            foreach (var tet in tetrahedra)
            {
                // ��������������
                float3 p0 = nodes[tet.x].position;
                float3 p1 = nodes[tet.y].position;
                float3 p2 = nodes[tet.z].position;
                float3 p3 = nodes[tet.w].position;

                float volume = TetrahedronUtility.CalculateVolume(p0, p1, p2, p3);

                if (volume > 1e-6f) // ���˵��˻���������
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
        /// ���ɱ���������
        /// </summary>
        private static FEMSurfaceTriangle[] GenerateSurfaceTriangles(FEMNodeData[] nodes, FEMTetrahedronData[] tetrahedra, Vector3[] originalVertices, int[] originalTriangles)
        {
            List<FEMSurfaceTriangle> surfaceTriangles = new List<FEMSurfaceTriangle>();

            // ������������ȡ����
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
        /// ���������ֲ�
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
                    // ������������������ڵ�
                    float[] nodeMasses = new float[nodes.Length];

                    foreach (var tet in tetrahedra)
                    {
                        float tetMass = tet.volume * tet.density;
                        float nodeMass = tetMass / 4.0f; // ƽ�������4���ڵ�

                        nodeMasses[tet.nodeIndices.x] += nodeMass;
                        nodeMasses[tet.nodeIndices.y] += nodeMass;
                        nodeMasses[tet.nodeIndices.z] += nodeMass;
                        nodeMasses[tet.nodeIndices.w] += nodeMass;
                    }

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].mass = math.max(nodeMasses[i], 0.001f); // ȷ����С����
                    }
                    break;
            }
        }

        /// <summary>
        /// Ӧ�ñ߽�����
        /// </summary>
        private static void ApplyBoundaryConditions(ref FEMNodeData[] nodes, FEMConversionSettings settings)
        {
            if (!settings.fixBoundaryNodes) return;

            // �����Χ��
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
                // �̶��ײ��ڵ�
                if (nodes[i].position.y < minPos.y + threshold)
                {
                    nodes[i].isFixed = true;
                }
            }
        }
    }
}