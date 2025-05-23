using UnityEngine;
using FEMSoftBody;
using Unity.Mathematics;
using System.Linq;

namespace FEMSoftBody
{
    public class ConversionTest : MonoBehaviour
    {
        [Header("��������")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private FEMConversionSettings settings;

        [Header("ת������")]
        [SerializeField] private bool convertOnStart = true;
        [SerializeField] private bool showDebugInfo = true;

        [Header("���ӻ�����")]
        [SerializeField] public FEMVisualizationSettings visualSettings = new FEMVisualizationSettings();

        // FEM���弸������
        private FEMGeometryData femData;

        void Start()
        {
            if (convertOnStart)
            {
                ConvertMesh();
            }
        }

        [ContextMenu("Convert Mesh")]
        public void ConvertMesh()
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Debug.Log("ת��MeshFilter...");
                femData = MeshToFEMConverter.ConvertMesh(meshFilter.sharedMesh, settings);
            }
            else if (skinnedMeshRenderer != null)
            {
                Debug.Log("ת��SkinnedMeshRenderer...");
                femData = MeshToFEMConverter.ConvertSkinnedMesh(skinnedMeshRenderer, settings);
            }
            else
            {
                Debug.LogError("δ�ҵ���ת�������������");
                return;
            }

            if (femData != null && showDebugInfo)
            {
                ShowConversionResults();
            }
        }

        private void ShowConversionResults()
        {
            Debug.Log($"=== FEMת����� ===");
            Debug.Log($"�ڵ�����: {femData.nodeCount}");
            Debug.Log($"  - ����ڵ�: {femData.nodes.Count(n => n.isSurface)}");
            Debug.Log($"  - �ڲ��ڵ�: {femData.nodes.Count(n => !n.isSurface)}");
            Debug.Log($"  - �̶��ڵ�: {femData.nodes.Count(n => n.isFixed)}");
            Debug.Log($"����������: {femData.tetrahedronCount}");
            Debug.Log($"��������������: {femData.surfaceTriangleCount}");
            Debug.Log($"�����: {femData.totalVolume:F3}");
            Debug.Log($"������: {femData.totalMass:F3}");
            Debug.Log($"��Χ��: {femData.bounds}");
            Debug.Log($"ƽ���ڵ�����: {(femData.totalMass / femData.nodeCount):F3}");
        }

        void OnDrawGizmos()
        {
            if (femData?.nodes != null)
            {
                DrawFEMVisualization();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (femData?.nodes != null)
            {
                DrawDetailedFEMInfo();
            }
        }

        private void DrawFEMVisualization()
        {
            var vis = visualSettings;

            // ���ư�Χ��
            if (vis.showBounds)
            {
                DrawBounds();
            }

            // ���ƽڵ�
            if (vis.showNodes)
            {
                DrawFEMNodes();
            }

            // ����������
            if (vis.showTetrahedra)
            {
                DrawTetrahedra();
            }

            // ���Ʊ���������
            if (vis.showSurfaceTriangles)
            {
                DrawSurfaceTriangles();
            }

            // ����ͳ����Ϣ
            if (vis.showStatistics)
            {
                DrawStatistics();
            }
        }

        private void DrawFEMNodes()
        {
            var vis = visualSettings;
            int nodeCount = 0;
            int maxNodes = vis.maxNodesToShow;

            for (int i = 0; i < femData.nodes.Length && nodeCount < maxNodes; i++)
            {
                var node = femData.nodes[i];

                // ���ݽڵ�����������ɫ�ʹ�С
                Color nodeColor;
                float nodeSize;

                if (node.isFixed)
                {
                    nodeColor = vis.fixedNodeColor;
                    nodeSize = vis.fixedNodeSize;
                }
                else if (node.isSurface)
                {
                    nodeColor = vis.surfaceNodeColor;
                    nodeSize = vis.surfaceNodeSize;
                }
                else
                {
                    nodeColor = vis.interiorNodeColor;
                    nodeSize = vis.interiorNodeSize;
                }

                // ������������͸����
                if (vis.showMassAsAlpha)
                {
                    float maxMass = GetMaxNodeMass();
                    float alpha = Mathf.Clamp01(node.mass / maxMass);
                    nodeColor.a = alpha;
                }

                Gizmos.color = nodeColor;

                // ���ƽڵ�
                if (vis.useWireframeSphere)
                {
                    Gizmos.DrawWireSphere(node.position, nodeSize);
                }
                else
                {
                    Gizmos.DrawSphere(node.position, nodeSize);
                }

                // ���ƹ̶��ڵ��������
                if (node.isFixed && vis.showFixedNodeMarkers)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(node.position, Vector3.one * nodeSize * 2f);
                }

                // ���ƽڵ�����
                if (vis.showNodeIndices && nodeCount < vis.maxIndicesToShow)
                {
                    DrawTextGizmo((Vector3)node.position + Vector3.up * (nodeSize + 0.1f), i.ToString(), Color.white);
                }

                // ���ƽڵ���Ϣ
                if (vis.showNodeInfo && i == vis.selectedNodeIndex)
                {
                    DrawSelectedNodeInfo(node, i);
                }

                nodeCount++;
            }

            // ����ڵ������������ƣ���ʾ����
            if (femData.nodes.Length > maxNodes)
            {
                DrawTextGizmo(transform.position + Vector3.up * 2f,
                    $"��ʾ {maxNodes}/{femData.nodes.Length} ���ڵ�", Color.yellow);
            }
        }

        private void DrawBounds()
        {
            Gizmos.color = visualSettings.boundsColor;
            Gizmos.DrawWireCube(femData.bounds.center, femData.bounds.size);

            // ���ư�Χ����Ϣ
            if (visualSettings.showBoundsInfo)
            {
                Vector3 textPos = femData.bounds.center + Vector3.up * (femData.bounds.size.y * 0.5f + 0.5f);
                DrawTextGizmo(textPos, $"Bounds: {femData.bounds.size:F2}", visualSettings.boundsColor);
            }
        }

        private void DrawTetrahedra()
        {
            var vis = visualSettings;
            Gizmos.color = vis.tetrahedraColor;

            int tetCount = 0;
            int maxTets = vis.maxTetrahedraToShow;

            foreach (var tet in femData.tetrahedra)
            {
                if (tetCount >= maxTets) break;

                // ��ȡ��������ĸ�����
                var p0 = femData.nodes[tet.nodeIndices.x].position;
                var p1 = femData.nodes[tet.nodeIndices.y].position;
                var p2 = femData.nodes[tet.nodeIndices.z].position;
                var p3 = femData.nodes[tet.nodeIndices.w].position;

                if (vis.showTetrahedraAsWireframe)
                {
                    // �����������12����
                    DrawTetrahedronWireframe(p0, p1, p2, p3);
                }

                if (vis.showTetrahedraCenters)
                {
                    Vector3 center = (p0 + p1 + p2 + p3) * 0.25f;
                    Gizmos.color = vis.tetrahedraCenterColor;
                    Gizmos.DrawSphere(center, vis.tetrahedraCenterSize);
                }

                // ��ʾѡ�е���������Ϣ
                if (tetCount == vis.selectedTetrahedronIndex && vis.showTetrahedronInfo)
                {
                    DrawSelectedTetrahedronInfo(tet, tetCount, p0, p1, p2, p3);
                }

                tetCount++;
            }

            if (femData.tetrahedra.Length > maxTets)
            {
                DrawTextGizmo(transform.position + Vector3.up * 1.5f,
                    $"��ʾ {maxTets}/{femData.tetrahedra.Length} ��������", Color.cyan);
            }
        }

        private void DrawSurfaceTriangles()
        {
            var vis = visualSettings;
            Gizmos.color = vis.surfaceTriangleColor;

            int triCount = 0;
            int maxTris = vis.maxSurfaceTrianglesToShow;

            foreach (var tri in femData.surfaceTriangles)
            {
                if (triCount >= maxTris) break;

                var p0 = femData.nodes[tri.nodeIndices.x].position;
                var p1 = femData.nodes[tri.nodeIndices.y].position;
                var p2 = femData.nodes[tri.nodeIndices.z].position;

                if (vis.showSurfaceTriangleWireframe)
                {
                    // ���������α߿�
                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p0);
                }

                if (vis.showSurfaceNormals)
                {
                    Vector3 center = (p0 + p1 + p2) / 3f;
                    Gizmos.color = vis.surfaceNormalColor;
                    Gizmos.DrawRay(center, tri.normal * vis.surfaceNormalLength);
                }

                triCount++;
            }
        }

        private void DrawDetailedFEMInfo()
        {
            if (!visualSettings.showDetailedInfo) return;

            // ����������
            if (visualSettings.showNodeConnections)
            {
                DrawNodeConnections();
            }

            // ����������������еĻ���
            if (visualSettings.showForceVectors)
            {
                DrawForceVectors();
            }

            // ���������ֲ�
            if (visualSettings.showMassDistribution)
            {
                DrawMassDistribution();
            }
        }

        private void DrawStatistics()
        {
            if (femData == null) return;

            Vector3 basePos = transform.position + Vector3.up * 3f;
            float lineHeight = 0.3f;
            int line = 0;

            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"=== FEM ͳ����Ϣ ===", Color.white);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"�ڵ�����: {femData.nodeCount}", Color.green);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"����ڵ�: {femData.nodes.Count(n => n.isSurface)}", Color.red);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"�ڲ��ڵ�: {femData.nodes.Count(n => !n.isSurface)}", Color.blue);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"�̶��ڵ�: {femData.nodes.Count(n => n.isFixed)}", Color.yellow);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"��������: {femData.tetrahedronCount}", Color.cyan);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"������������: {femData.surfaceTriangleCount}", Color.magenta);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"�����: {femData.totalVolume:F3}", Color.white);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"������: {femData.totalMass:F3}", Color.white);
        }

        #region �������Ʒ���

        private void DrawTetrahedronWireframe(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // ����������
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p0);

            // �������������
            Gizmos.DrawLine(p0, p3);
            Gizmos.DrawLine(p1, p3);
            Gizmos.DrawLine(p2, p3);
        }

        private void DrawSelectedNodeInfo(FEMNodeData node, int index)
        {
            Vector3 infoPos = (Vector3)node.position + Vector3.up * 0.5f;
            float lineHeight = 0.15f;
            int line = 0;

            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"�ڵ� #{index}", Color.yellow);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"λ��: {node.position:F2}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"����: {node.mass:F3}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"����: {(node.isSurface ? "��" : "��")}", node.isSurface ? Color.red : Color.blue);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"�̶�: {(node.isFixed ? "��" : "��")}", node.isFixed ? Color.yellow : Color.gray);
        }

        private void DrawSelectedTetrahedronInfo(FEMTetrahedronData tet, int index, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 center = (p0 + p1 + p2 + p3) * 0.25f;
            Vector3 infoPos = center + Vector3.up * 0.8f;
            float lineHeight = 0.15f;
            int line = 0;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.1f);

            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"������ #{index}", Color.yellow);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"�ڵ�: [{tet.nodeIndices.x}, {tet.nodeIndices.y}, {tet.nodeIndices.z}, {tet.nodeIndices.w}]", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"���: {tet.volume:F6}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"����ģ��: {tet.youngModulus:F0}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"���ɱ�: {tet.poissonRatio:F3}", Color.white);
        }

        private void DrawNodeConnections()
        {
            Gizmos.color = visualSettings.connectionLineColor;

            int connectionCount = 0;
            int maxConnections = visualSettings.maxConnectionsToShow;

            foreach (var tet in femData.tetrahedra)
            {
                if (connectionCount >= maxConnections) break;

                var p0 = femData.nodes[tet.nodeIndices.x].position;
                var p1 = femData.nodes[tet.nodeIndices.y].position;
                var p2 = femData.nodes[tet.nodeIndices.z].position;
                var p3 = femData.nodes[tet.nodeIndices.w].position;

                // �����������ڵ����ӣ��򻯰棬ֻ���Ʋ������ӱ�������ܼ���
                if (connectionCount % 3 == 0) // ÿ3�����������һ������
                {
                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p2, p3);
                }

                connectionCount++;
            }
        }

        private void DrawForceVectors()
        {
            Gizmos.color = visualSettings.forceVectorColor;

            for (int i = 0; i < femData.nodes.Length && i < visualSettings.maxForceVectorsToShow; i++)
            {
                var node = femData.nodes[i];
                if (math.lengthsq(node.force) > 0.001f)
                {
                    Vector3 forceDir = math.normalize(node.force);
                    float forceMag = math.length(node.force);
                    float vectorLength = Mathf.Min(forceMag * visualSettings.forceVectorScale, visualSettings.maxForceVectorLength);

                    Gizmos.DrawRay(node.position, forceDir * vectorLength);

                    // ���Ƽ�ͷͷ��
                    Vector3 arrowHead = (Vector3)node.position + forceDir * vectorLength;
                    Vector3 perpendicular = Vector3.Cross(forceDir, Vector3.up).normalized * 0.05f;
                    Gizmos.DrawLine(arrowHead, arrowHead - forceDir * 0.1f + perpendicular);
                    Gizmos.DrawLine(arrowHead, arrowHead - forceDir * 0.1f - perpendicular);
                }
            }
        }

        private void DrawMassDistribution()
        {
            float maxMass = GetMaxNodeMass();
            float minMass = GetMinNodeMass();

            for (int i = 0; i < femData.nodes.Length && i < visualSettings.maxMassVisualizationNodes; i++)
            {
                var node = femData.nodes[i];
                float massRatio = (node.mass - minMass) / (maxMass - minMass);

                Color massColor = Color.Lerp(Color.blue, Color.red, massRatio);
                Gizmos.color = massColor;

                float size = Mathf.Lerp(0.02f, 0.1f, massRatio);
                Gizmos.DrawSphere(node.position, size);
            }
        }

        private void DrawTextGizmo(Vector3 position, string text, Color color)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(position, text);
#endif
        }

        private float GetMaxNodeMass()
        {
            float max = 0f;
            foreach (var node in femData.nodes)
            {
                if (node.mass > max) max = node.mass;
            }
            return max;
        }

        private float GetMinNodeMass()
        {
            float min = float.MaxValue;
            foreach (var node in femData.nodes)
            {
                if (node.mass < min) min = node.mass;
            }
            return min;
        }

        #endregion
    }
}