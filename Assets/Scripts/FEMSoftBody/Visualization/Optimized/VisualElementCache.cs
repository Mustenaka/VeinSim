using FEMSoftBody;
using System.Collections.Generic;
using UnityEngine;

public class VisualElementCache
{
    private Dictionary<int, VisibleElements> lodCache;
    private FEMGeometryData femData;
    private LODManager lodManager;

    public VisualElementCache()
    {
        lodCache = new Dictionary<int, VisibleElements>();
    }

    public void PrecomputeVisualizationData(FEMGeometryData femData, LODManager lodManager)
    {
        this.femData = femData;
        this.lodManager = lodManager;

        // 预计算不同LOD级别的可见元素
        for (int lod = 0; lod < 4; lod++)
        {
            lodCache[lod] = ComputeVisibleElementsForLOD(lod);
        }
    }

    public void UpdateLODCache(int lodLevel)
    {
        if (!lodCache.ContainsKey(lodLevel))
        {
            lodCache[lodLevel] = ComputeVisibleElementsForLOD(lodLevel);
        }
    }

    public VisibleElements GetVisibleElements(int lodLevel, Vector3 cameraPos, Vector3 cameraDir)
    {
        if (!lodCache.ContainsKey(lodLevel))
        {
            UpdateLODCache(lodLevel);
        }

        var cachedElements = lodCache[lodLevel];

        // 基于相机位置进行进一步剔除
        return PerformCameraBasedCulling(cachedElements, cameraPos, cameraDir);
    }

    private VisibleElements ComputeVisibleElementsForLOD(int lodLevel)
    {
        var elements = new VisibleElements();

        // 计算采样间隔
        int nodeStep = 1 << lodLevel; // 2^lodLevel
        int tetStep = 1 << (lodLevel + 1);
        int triStep = 1 << lodLevel;

        // 选择节点
        for (int i = 0; i < femData.nodes.Length; i += nodeStep)
        {
            elements.nodes.Add(i);
        }

        // 确保所有表面节点都被包含（即使在高LOD下）
        for (int i = 0; i < femData.nodes.Length; i++)
        {
            if (femData.nodes[i].isSurface && !elements.nodes.Contains(i))
            {
                elements.nodes.Add(i);
            }
        }

        // 选择四面体
        for (int i = 0; i < femData.tetrahedra.Length; i += tetStep)
        {
            elements.tetrahedra.Add(i);
        }

        // 选择表面三角形
        for (int i = 0; i < femData.surfaceTriangles.Length; i += triStep)
        {
            elements.surfaceTriangles.Add(i);
        }

        return elements;
    }

    private VisibleElements PerformCameraBasedCulling(VisibleElements elements, Vector3 cameraPos, Vector3 cameraDir)
    {
        var culledElements = new VisibleElements();

        // 节点剔除
        foreach (int nodeIndex in elements.nodes)
        {
            if (IsNodeVisible(nodeIndex, cameraPos, cameraDir))
            {
                culledElements.nodes.Add(nodeIndex);
            }
        }

        // 四面体剔除（简化版）
        foreach (int tetIndex in elements.tetrahedra)
        {
            if (IsTetrahedronVisible(tetIndex, cameraPos))
            {
                culledElements.tetrahedra.Add(tetIndex);
            }
        }

        // 表面三角形剔除
        foreach (int triIndex in elements.surfaceTriangles)
        {
            if (IsSurfaceTriangleVisible(triIndex, cameraPos))
            {
                culledElements.surfaceTriangles.Add(triIndex);
            }
        }

        return culledElements;
    }

    private bool IsNodeVisible(int nodeIndex, Vector3 cameraPos, Vector3 cameraDir)
    {
        Vector3 nodePos = femData.nodes[nodeIndex].position;
        float distance = Vector3.Distance(nodePos, cameraPos);

        // 距离剔除
        if (distance > 30f) return false;

        // 视锥剔除（简化版）
        Vector3 toNode = (nodePos - cameraPos).normalized;
        float dot = Vector3.Dot(toNode, cameraDir);

        return dot > -0.5f; // 允许在相机后方一定角度内的节点
    }

    private bool IsTetrahedronVisible(int tetIndex, Vector3 cameraPos)
    {
        var tet = femData.tetrahedra[tetIndex];
        Vector3 center = GetTetrahedronCenter(tet);
        float distance = Vector3.Distance(center, cameraPos);

        return distance <= 25f; // 四面体更早剔除
    }

    private bool IsSurfaceTriangleVisible(int triIndex, Vector3 cameraPos)
    {
        var tri = femData.surfaceTriangles[triIndex];
        Vector3 p0 = femData.nodes[tri.nodeIndices.x].position;
        Vector3 p1 = femData.nodes[tri.nodeIndices.y].position;
        Vector3 p2 = femData.nodes[tri.nodeIndices.z].position;
        Vector3 center = (p0 + p1 + p2) / 3f;

        float distance = Vector3.Distance(center, cameraPos);
        return distance <= 20f; // 表面三角形最早剔除
    }

    private Vector3 GetTetrahedronCenter(FEMTetrahedronData tet)
    {
        var p0 = femData.nodes[tet.nodeIndices.x].position;
        var p1 = femData.nodes[tet.nodeIndices.y].position;
        var p2 = femData.nodes[tet.nodeIndices.z].position;
        var p3 = femData.nodes[tet.nodeIndices.w].position;
        return (p0 + p1 + p2 + p3) * 0.25f;
    }

    public int GetVisibleNodeCount()
    {
        int currentLOD = lodManager.GetCurrentLODLevel();
        return lodCache.ContainsKey(currentLOD) ? lodCache[currentLOD].nodes.Count : 0;
    }

    public int GetVisibleTetrahedronCount()
    {
        int currentLOD = lodManager.GetCurrentLODLevel();
        return lodCache.ContainsKey(currentLOD) ? lodCache[currentLOD].tetrahedra.Count : 0;
    }

    public void Dispose()
    {
        lodCache?.Clear();
    }
}