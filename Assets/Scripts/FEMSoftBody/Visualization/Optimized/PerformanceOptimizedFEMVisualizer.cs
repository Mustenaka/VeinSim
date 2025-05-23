using UnityEngine;
using FEMSoftBody;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PerformanceOptimizedFEMVisualizer : MonoBehaviour
{ 
    [Header("输入设置")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private FEMConversionSettings settings;

    [Header("性能设置")]
    [SerializeField] private FEMPerformanceSettings performanceSettings = new FEMPerformanceSettings();

    [Header("可视化设置")]
    [SerializeField] private OptimizedVisualizationSettings visualSettings = new OptimizedVisualizationSettings();

    [Header("转换控制")]
    [SerializeField] private bool convertOnStart = true;
    [SerializeField] private bool showDebugInfo = true;

    // 数据
    private FEMGeometryData femData;
    private LODManager lodManager;
    private PerformanceMonitor performanceMonitor;
    private VisualElementCache visualCache;

    // 渲染状态
    private bool isVisualizationReady = false;
    private Coroutine visualizationUpdateCoroutine;

    void Start()
    {
        InitializePerformanceComponents();

        if (convertOnStart)
        {
            ConvertMesh();
        }
    }

    void OnDestroy()
    {
        StopVisualizationUpdate();
        visualCache?.Dispose();
    }

    private void InitializePerformanceComponents()
    {
        performanceMonitor = new PerformanceMonitor(performanceSettings);
        visualCache = new VisualElementCache();
        lodManager = new LODManager(performanceSettings);
    }

    [ContextMenu("Convert Mesh")]
    public void ConvertMesh()
    {
        StopVisualizationUpdate();

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

        if (femData != null)
        {
            InitializeVisualization();

            if (showDebugInfo)
            {
                ShowConversionResults();
            }
        }
    }

    private void InitializeVisualization()
    {
        // 初始化LOD管理器
        lodManager.Initialize(femData, performanceSettings);

        // 预计算可视化数据
        visualCache.PrecomputeVisualizationData(femData, lodManager);

        // 启动可视化更新协程
        StartVisualizationUpdate();

        isVisualizationReady = true;
        Debug.Log($"可视化初始化完成，LOD级别: {lodManager.GetCurrentLODLevel()}");
    }

    private void StartVisualizationUpdate()
    {
        if (visualizationUpdateCoroutine != null)
        {
            StopCoroutine(visualizationUpdateCoroutine);
        }

        visualizationUpdateCoroutine = StartCoroutine(VisualizationUpdateCoroutine());
    }

    private void StopVisualizationUpdate()
    {
        if (visualizationUpdateCoroutine != null)
        {
            StopCoroutine(visualizationUpdateCoroutine);
            visualizationUpdateCoroutine = null;
        }
    }

    private IEnumerator VisualizationUpdateCoroutine()
    {
        while (isVisualizationReady)
        {
            // 更新性能监控
            performanceMonitor.Update();

            // 根据性能自动调整LOD
            lodManager.UpdateLOD(performanceMonitor.GetCurrentFPS(), GetCameraDistance());

            // 更新可视化缓存
            if (lodManager.HasLODChanged())
            {
                visualCache.UpdateLODCache(lodManager.GetCurrentLODLevel());
            }

            // 每秒更新一次LOD
            yield return new WaitForSeconds(0.1f);
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
        Debug.Log($"初始LOD级别: {lodManager.GetCurrentLODLevel()}");
    }

    void OnDrawGizmos()
    {
        if (!isVisualizationReady || femData?.nodes == null) return;

        performanceMonitor.BeginFrame();

        DrawOptimizedVisualization();

        performanceMonitor.EndFrame();
    }

    private void DrawOptimizedVisualization()
    {
        var currentLOD = lodManager.GetCurrentLODLevel();
        var visibleElements = visualCache.GetVisibleElements(currentLOD, GetCameraPosition(), GetCameraDirection());

        // 绘制包围盒（总是显示）
        if (visualSettings.showBounds)
        {
            DrawBounds();
        }

        // 绘制性能信息
        if (visualSettings.showPerformanceInfo)
        {
            DrawPerformanceInfo();
        }

        // 绘制节点（LOD优化）
        if (visualSettings.showNodes)
        {
            DrawOptimizedNodes(visibleElements.nodes);
        }

        // 绘制四面体（LOD优化）
        if (visualSettings.showTetrahedra && currentLOD <= 2)
        {
            DrawOptimizedTetrahedra(visibleElements.tetrahedra);
        }

        // 绘制表面三角形（LOD优化）
        if (visualSettings.showSurfaceTriangles && currentLOD <= 1)
        {
            DrawOptimizedSurfaceTriangles(visibleElements.surfaceTriangles);
        }

        // 绘制LOD信息
        if (visualSettings.showLODInfo)
        {
            DrawLODInfo(currentLOD, visibleElements);
        }
    }

    private void DrawOptimizedNodes(List<int> visibleNodeIndices)
    {
        int drawnNodes = 0;
        int maxNodes = lodManager.GetMaxNodesForCurrentLOD();

        foreach (int nodeIndex in visibleNodeIndices)
        {
            if (drawnNodes >= maxNodes) break;

            var node = femData.nodes[nodeIndex];

            // 距离剔除
            float distance = Vector3.Distance(node.position, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance) continue;

            // LOD-based大小调整
            float lodScale = lodManager.GetSizeScale();
            float nodeSize = GetNodeSize(node) * lodScale;

            // 根据距离调整大小
            float distanceScale = Mathf.Clamp(10f / distance, 0.1f, 2f);
            nodeSize *= distanceScale;

            // 设置颜色
            Gizmos.color = GetNodeColor(node);

            // 绘制节点
            if (visualSettings.useWireframeSphere)
            {
                Gizmos.DrawWireSphere(node.position, nodeSize);
            }
            else
            {
                Gizmos.DrawSphere(node.position, nodeSize);
            }

            // 固定节点标记
            if (node.isFixed && visualSettings.showFixedNodeMarkers)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(node.position, Vector3.one * nodeSize * 1.5f);
            }

            drawnNodes++;
        }
    }

    private void DrawOptimizedTetrahedra(List<int> visibleTetrahedraIndices)
    {
        Gizmos.color = visualSettings.tetrahedraColor;
        int drawnTets = 0;
        int maxTets = lodManager.GetMaxTetrahedraForCurrentLOD();

        foreach (int tetIndex in visibleTetrahedraIndices)
        {
            if (drawnTets >= maxTets) break;

            var tet = femData.tetrahedra[tetIndex];

            // 获取四面体中心进行距离剔除
            Vector3 center = GetTetrahedronCenter(tet);
            float distance = Vector3.Distance(center, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance * 0.8f) continue;

            // 简化绘制：距离远时只绘制部分边
            bool drawSimplified = distance > visualSettings.maxDrawDistance * 0.4f;

            var p0 = femData.nodes[tet.nodeIndices.x].position;
            var p1 = femData.nodes[tet.nodeIndices.y].position;
            var p2 = femData.nodes[tet.nodeIndices.z].position;
            var p3 = femData.nodes[tet.nodeIndices.w].position;

            if (drawSimplified)
            {
                // 简化绘制：只绘制3条主要边
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
            }
            else
            {
                // 完整绘制
                DrawTetrahedronWireframe(p0, p1, p2, p3);
            }

            drawnTets++;
        }
    }

    private void DrawOptimizedSurfaceTriangles(List<int> visibleTriangleIndices)
    {
        Gizmos.color = visualSettings.surfaceTriangleColor;
        int drawnTris = 0;
        int maxTris = lodManager.GetMaxSurfaceTrianglesForCurrentLOD();

        foreach (int triIndex in visibleTriangleIndices)
        {
            if (drawnTris >= maxTris) break;

            var tri = femData.surfaceTriangles[triIndex];

            var p0 = femData.nodes[tri.nodeIndices.x].position;
            var p1 = femData.nodes[tri.nodeIndices.y].position;
            var p2 = femData.nodes[tri.nodeIndices.z].position;

            // 距离剔除
            Vector3 center = (p0 + p1 + p2) / 3f;
            float distance = Vector3.Distance(center, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance * 0.6f) continue;

            // 绘制三角形边框
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p0);

            // 法向量（只在近距离显示）
            if (visualSettings.showSurfaceNormals && distance < visualSettings.maxDrawDistance * 0.3f)
            {
                Gizmos.color = visualSettings.surfaceNormalColor;
                Gizmos.DrawRay(center, tri.normal * visualSettings.surfaceNormalLength);
                Gizmos.color = visualSettings.surfaceTriangleColor;
            }

            drawnTris++;
        }
    }

    private void DrawBounds()
    {
        Gizmos.color = visualSettings.boundsColor;
        Gizmos.DrawWireCube(femData.bounds.center, femData.bounds.size);
    }

    private void DrawPerformanceInfo()
    {
        Vector3 basePos = transform.position + Vector3.up * 3f;
        float lineHeight = 0.3f;
        int line = 0;

        var fps = performanceMonitor.GetCurrentFPS();
        Color fpsColor = fps > 30 ? Color.green : fps > 15 ? Color.yellow : Color.red;

        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"FPS: {fps:F1}", fpsColor);
        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"LOD级别: {lodManager.GetCurrentLODLevel()}", Color.cyan);
        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"绘制节点: {visualCache.GetVisibleNodeCount()}", Color.white);
        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"绘制四面体: {visualCache.GetVisibleTetrahedronCount()}", Color.white);
    }

    private void DrawLODInfo(int currentLOD, VisibleElements visibleElements)
    {
        Vector3 infoPos = transform.position + Vector3.right * 3f;
        float lineHeight = 0.2f;
        int line = 0;

        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"=== LOD {currentLOD} ===", Color.yellow);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"可见节点: {visibleElements.nodes.Count}", Color.green);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"可见四面体: {visibleElements.tetrahedra.Count}", Color.cyan);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"可见三角形: {visibleElements.surfaceTriangles.Count}", Color.magenta);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"相机距离: {GetCameraDistance():F1}m", Color.white);
    }

    #region 辅助方法

    private Vector3 GetCameraPosition()
    {
        Camera cam = Camera.current ?? Camera.main;
        return cam != null ? cam.transform.position : transform.position + Vector3.back * 5f;
    }

    private Vector3 GetCameraDirection()
    {
        Camera cam = Camera.current ?? Camera.main;
        return cam != null ? cam.transform.forward : Vector3.forward;
    }

    private float GetCameraDistance()
    {
        return Vector3.Distance(GetCameraPosition(), transform.position);
    }

    private float GetNodeSize(FEMNodeData node)
    {
        if (node.isFixed) return visualSettings.fixedNodeSize;
        return node.isSurface ? visualSettings.surfaceNodeSize : visualSettings.interiorNodeSize;
    }

    private Color GetNodeColor(FEMNodeData node)
    {
        if (node.isFixed) return visualSettings.fixedNodeColor;
        return node.isSurface ? visualSettings.surfaceNodeColor : visualSettings.interiorNodeColor;
    }

    private Vector3 GetTetrahedronCenter(FEMTetrahedronData tet)
    {
        var p0 = femData.nodes[tet.nodeIndices.x].position;
        var p1 = femData.nodes[tet.nodeIndices.y].position;
        var p2 = femData.nodes[tet.nodeIndices.z].position;
        var p3 = femData.nodes[tet.nodeIndices.w].position;
        return (p0 + p1 + p2 + p3) * 0.25f;
    }

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

    private void DrawTextGizmo(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position, text);
#endif
    }

    #endregion
}
