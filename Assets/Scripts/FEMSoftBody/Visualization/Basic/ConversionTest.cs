using UnityEngine;
using FEMSoftBody;
using Unity.Mathematics;
using System.Linq;

namespace FEMSoftBody
{
    public class ConversionTest : MonoBehaviour
    {
        [Header("输入设置")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private FEMConversionSettings settings;

        [Header("转换控制")]
        [SerializeField] private bool convertOnStart = true;
        [SerializeField] private bool showDebugInfo = true;

        [Header("可视化控制")]
        [SerializeField] public FEMVisualizationSettings visualSettings = new FEMVisualizationSettings();

        // FEM柔体几何数据
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
                Debug.Log("转换MeshFilter...");
                femData = MeshToFEMConverter.ConvertMesh(meshFilter.sharedMesh, settings);
            }
            else if (skinnedMeshRenderer != null)
            {
                Debug.Log("转换SkinnedMeshRenderer...");
                femData = MeshToFEMConverter.ConvertSkinnedMesh(skinnedMeshRenderer, settings);
            }
            else
            {
                Debug.LogError("未找到可转换的网格组件！");
                return;
            }

            if (femData != null && showDebugInfo)
            {
                ShowConversionResults();
            }
        }

        private void ShowConversionResults()
        {
            Debug.Log($"=== FEM转换结果 ===");
            Debug.Log($"节点数量: {femData.nodeCount}");
            Debug.Log($"  - 表面节点: {femData.nodes.Count(n => n.isSurface)}");
            Debug.Log($"  - 内部节点: {femData.nodes.Count(n => !n.isSurface)}");
            Debug.Log($"  - 固定节点: {femData.nodes.Count(n => n.isFixed)}");
            Debug.Log($"四面体数量: {femData.tetrahedronCount}");
            Debug.Log($"表面三角形数量: {femData.surfaceTriangleCount}");
            Debug.Log($"总体积: {femData.totalVolume:F3}");
            Debug.Log($"总质量: {femData.totalMass:F3}");
            Debug.Log($"包围盒: {femData.bounds}");
            Debug.Log($"平均节点质量: {(femData.totalMass / femData.nodeCount):F3}");
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

            // 绘制包围盒
            if (vis.showBounds)
            {
                DrawBounds();
            }

            // 绘制节点
            if (vis.showNodes)
            {
                DrawFEMNodes();
            }

            // 绘制四面体
            if (vis.showTetrahedra)
            {
                DrawTetrahedra();
            }

            // 绘制表面三角形
            if (vis.showSurfaceTriangles)
            {
                DrawSurfaceTriangles();
            }

            // 绘制统计信息
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

                // 根据节点类型设置颜色和大小
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

                // 根据质量调整透明度
                if (vis.showMassAsAlpha)
                {
                    float maxMass = GetMaxNodeMass();
                    float alpha = Mathf.Clamp01(node.mass / maxMass);
                    nodeColor.a = alpha;
                }

                Gizmos.color = nodeColor;

                // 绘制节点
                if (vis.useWireframeSphere)
                {
                    Gizmos.DrawWireSphere(node.position, nodeSize);
                }
                else
                {
                    Gizmos.DrawSphere(node.position, nodeSize);
                }

                // 绘制固定节点的特殊标记
                if (node.isFixed && vis.showFixedNodeMarkers)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(node.position, Vector3.one * nodeSize * 2f);
                }

                // 绘制节点索引
                if (vis.showNodeIndices && nodeCount < vis.maxIndicesToShow)
                {
                    DrawTextGizmo((Vector3)node.position + Vector3.up * (nodeSize + 0.1f), i.ToString(), Color.white);
                }

                // 绘制节点信息
                if (vis.showNodeInfo && i == vis.selectedNodeIndex)
                {
                    DrawSelectedNodeInfo(node, i);
                }

                nodeCount++;
            }

            // 如果节点数量超过限制，显示警告
            if (femData.nodes.Length > maxNodes)
            {
                DrawTextGizmo(transform.position + Vector3.up * 2f,
                    $"显示 {maxNodes}/{femData.nodes.Length} 个节点", Color.yellow);
            }
        }

        private void DrawBounds()
        {
            Gizmos.color = visualSettings.boundsColor;
            Gizmos.DrawWireCube(femData.bounds.center, femData.bounds.size);

            // 绘制包围盒信息
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

                // 获取四面体的四个顶点
                var p0 = femData.nodes[tet.nodeIndices.x].position;
                var p1 = femData.nodes[tet.nodeIndices.y].position;
                var p2 = femData.nodes[tet.nodeIndices.z].position;
                var p3 = femData.nodes[tet.nodeIndices.w].position;

                if (vis.showTetrahedraAsWireframe)
                {
                    // 绘制四面体的12条边
                    DrawTetrahedronWireframe(p0, p1, p2, p3);
                }

                if (vis.showTetrahedraCenters)
                {
                    Vector3 center = (p0 + p1 + p2 + p3) * 0.25f;
                    Gizmos.color = vis.tetrahedraCenterColor;
                    Gizmos.DrawSphere(center, vis.tetrahedraCenterSize);
                }

                // 显示选中的四面体信息
                if (tetCount == vis.selectedTetrahedronIndex && vis.showTetrahedronInfo)
                {
                    DrawSelectedTetrahedronInfo(tet, tetCount, p0, p1, p2, p3);
                }

                tetCount++;
            }

            if (femData.tetrahedra.Length > maxTets)
            {
                DrawTextGizmo(transform.position + Vector3.up * 1.5f,
                    $"显示 {maxTets}/{femData.tetrahedra.Length} 个四面体", Color.cyan);
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
                    // 绘制三角形边框
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

            // 绘制连接线
            if (visualSettings.showNodeConnections)
            {
                DrawNodeConnections();
            }

            // 绘制力向量（如果有的话）
            if (visualSettings.showForceVectors)
            {
                DrawForceVectors();
            }

            // 绘制质量分布
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
                $"=== FEM 统计信息 ===", Color.white);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"节点总数: {femData.nodeCount}", Color.green);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"表面节点: {femData.nodes.Count(n => n.isSurface)}", Color.red);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"内部节点: {femData.nodes.Count(n => !n.isSurface)}", Color.blue);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"固定节点: {femData.nodes.Count(n => n.isFixed)}", Color.yellow);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"四面体数: {femData.tetrahedronCount}", Color.cyan);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"表面三角形数: {femData.surfaceTriangleCount}", Color.magenta);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"总体积: {femData.totalVolume:F3}", Color.white);
            DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
                $"总质量: {femData.totalMass:F3}", Color.white);
        }

        #region 辅助绘制方法

        private void DrawTetrahedronWireframe(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // 底面三角形
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p0);

            // 到顶点的连接线
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
                $"节点 #{index}", Color.yellow);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"位置: {node.position:F2}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"质量: {node.mass:F3}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"表面: {(node.isSurface ? "是" : "否")}", node.isSurface ? Color.red : Color.blue);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"固定: {(node.isFixed ? "是" : "否")}", node.isFixed ? Color.yellow : Color.gray);
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
                $"四面体 #{index}", Color.yellow);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"节点: [{tet.nodeIndices.x}, {tet.nodeIndices.y}, {tet.nodeIndices.z}, {tet.nodeIndices.w}]", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"体积: {tet.volume:F6}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"杨氏模量: {tet.youngModulus:F0}", Color.white);
            DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
                $"泊松比: {tet.poissonRatio:F3}", Color.white);
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

                // 绘制四面体内的连接（简化版，只绘制部分连接避免过于密集）
                if (connectionCount % 3 == 0) // 每3个四面体绘制一次连接
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

                    // 绘制箭头头部
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