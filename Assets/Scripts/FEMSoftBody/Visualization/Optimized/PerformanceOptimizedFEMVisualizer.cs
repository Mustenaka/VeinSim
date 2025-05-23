using UnityEngine;
using FEMSoftBody;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PerformanceOptimizedFEMVisualizer : MonoBehaviour
{ 
    [Header("��������")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private FEMConversionSettings settings;

    [Header("��������")]
    [SerializeField] private FEMPerformanceSettings performanceSettings = new FEMPerformanceSettings();

    [Header("���ӻ�����")]
    [SerializeField] private OptimizedVisualizationSettings visualSettings = new OptimizedVisualizationSettings();

    [Header("ת������")]
    [SerializeField] private bool convertOnStart = true;
    [SerializeField] private bool showDebugInfo = true;

    // ����
    private FEMGeometryData femData;
    private LODManager lodManager;
    private PerformanceMonitor performanceMonitor;
    private VisualElementCache visualCache;

    // ��Ⱦ״̬
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
        // ��ʼ��LOD������
        lodManager.Initialize(femData, performanceSettings);

        // Ԥ������ӻ�����
        visualCache.PrecomputeVisualizationData(femData, lodManager);

        // �������ӻ�����Э��
        StartVisualizationUpdate();

        isVisualizationReady = true;
        Debug.Log($"���ӻ���ʼ����ɣ�LOD����: {lodManager.GetCurrentLODLevel()}");
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
            // �������ܼ��
            performanceMonitor.Update();

            // ���������Զ�����LOD
            lodManager.UpdateLOD(performanceMonitor.GetCurrentFPS(), GetCameraDistance());

            // ���¿��ӻ�����
            if (lodManager.HasLODChanged())
            {
                visualCache.UpdateLODCache(lodManager.GetCurrentLODLevel());
            }

            // ÿ�����һ��LOD
            yield return new WaitForSeconds(0.1f);
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
        Debug.Log($"��ʼLOD����: {lodManager.GetCurrentLODLevel()}");
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

        // ���ư�Χ�У�������ʾ��
        if (visualSettings.showBounds)
        {
            DrawBounds();
        }

        // ����������Ϣ
        if (visualSettings.showPerformanceInfo)
        {
            DrawPerformanceInfo();
        }

        // ���ƽڵ㣨LOD�Ż���
        if (visualSettings.showNodes)
        {
            DrawOptimizedNodes(visibleElements.nodes);
        }

        // ���������壨LOD�Ż���
        if (visualSettings.showTetrahedra && currentLOD <= 2)
        {
            DrawOptimizedTetrahedra(visibleElements.tetrahedra);
        }

        // ���Ʊ��������Σ�LOD�Ż���
        if (visualSettings.showSurfaceTriangles && currentLOD <= 1)
        {
            DrawOptimizedSurfaceTriangles(visibleElements.surfaceTriangles);
        }

        // ����LOD��Ϣ
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

            // �����޳�
            float distance = Vector3.Distance(node.position, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance) continue;

            // LOD-based��С����
            float lodScale = lodManager.GetSizeScale();
            float nodeSize = GetNodeSize(node) * lodScale;

            // ���ݾ��������С
            float distanceScale = Mathf.Clamp(10f / distance, 0.1f, 2f);
            nodeSize *= distanceScale;

            // ������ɫ
            Gizmos.color = GetNodeColor(node);

            // ���ƽڵ�
            if (visualSettings.useWireframeSphere)
            {
                Gizmos.DrawWireSphere(node.position, nodeSize);
            }
            else
            {
                Gizmos.DrawSphere(node.position, nodeSize);
            }

            // �̶��ڵ���
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

            // ��ȡ���������Ľ��о����޳�
            Vector3 center = GetTetrahedronCenter(tet);
            float distance = Vector3.Distance(center, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance * 0.8f) continue;

            // �򻯻��ƣ�����Զʱֻ���Ʋ��ֱ�
            bool drawSimplified = distance > visualSettings.maxDrawDistance * 0.4f;

            var p0 = femData.nodes[tet.nodeIndices.x].position;
            var p1 = femData.nodes[tet.nodeIndices.y].position;
            var p2 = femData.nodes[tet.nodeIndices.z].position;
            var p3 = femData.nodes[tet.nodeIndices.w].position;

            if (drawSimplified)
            {
                // �򻯻��ƣ�ֻ����3����Ҫ��
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
            }
            else
            {
                // ��������
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

            // �����޳�
            Vector3 center = (p0 + p1 + p2) / 3f;
            float distance = Vector3.Distance(center, GetCameraPosition());
            if (distance > visualSettings.maxDrawDistance * 0.6f) continue;

            // ���������α߿�
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p0);

            // ��������ֻ�ڽ�������ʾ��
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
            $"LOD����: {lodManager.GetCurrentLODLevel()}", Color.cyan);
        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"���ƽڵ�: {visualCache.GetVisibleNodeCount()}", Color.white);
        DrawTextGizmo(basePos + Vector3.up * (line++ * lineHeight),
            $"����������: {visualCache.GetVisibleTetrahedronCount()}", Color.white);
    }

    private void DrawLODInfo(int currentLOD, VisibleElements visibleElements)
    {
        Vector3 infoPos = transform.position + Vector3.right * 3f;
        float lineHeight = 0.2f;
        int line = 0;

        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"=== LOD {currentLOD} ===", Color.yellow);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"�ɼ��ڵ�: {visibleElements.nodes.Count}", Color.green);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"�ɼ�������: {visibleElements.tetrahedra.Count}", Color.cyan);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"�ɼ�������: {visibleElements.surfaceTriangles.Count}", Color.magenta);
        DrawTextGizmo(infoPos + Vector3.up * (line++ * lineHeight),
            $"�������: {GetCameraDistance():F1}m", Color.white);
    }

    #region ��������

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
        // ����������
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p0);

        // �������������
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
