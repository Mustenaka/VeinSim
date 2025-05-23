using FEMSoftBody;
using UnityEngine;

public class LODManager
{
    private FEMPerformanceSettings settings;
    private int currentLODLevel;
    private bool lodChanged;
    private float lastLODChangeTime;
    private float lodChangeDebounceTime = 0.5f;

    public LODManager(FEMPerformanceSettings settings)
    {
        this.settings = settings;
        currentLODLevel = 0;
        lodChanged = false;
    }

    public void Initialize(FEMGeometryData femData, FEMPerformanceSettings settings)
    {
        // 根据数据量自动设置初始LOD
        int dataComplexity = GetDataComplexity(femData);
        currentLODLevel = Mathf.Clamp(dataComplexity, 0, settings.lodLevels - 1);

        Debug.Log($"初始LOD级别设置为: {currentLODLevel} (基于数据复杂度: {dataComplexity})");
    }

    public void UpdateLOD(float currentFPS, float cameraDistance)
    {
        if (!settings.autoLODAdjustment) return;
        if (Time.time - lastLODChangeTime < lodChangeDebounceTime) return;

        int newLODLevel = CalculateOptimalLOD(currentFPS, cameraDistance);

        if (newLODLevel != currentLODLevel)
        {
            currentLODLevel = newLODLevel;
            lodChanged = true;
            lastLODChangeTime = Time.time;

            Debug.Log($"LOD级别调整为: {currentLODLevel} (FPS: {currentFPS:F1}, 距离: {cameraDistance:F1})");
        }
    }

    private int CalculateOptimalLOD(float currentFPS, float cameraDistance)
    {
        int lodLevel = currentLODLevel;

        // 根据FPS调整
        if (currentFPS < settings.minimumFPS)
        {
            lodLevel = Mathf.Min(lodLevel + 1, settings.lodLevels - 1);
        }
        else if (currentFPS > settings.targetFPS * 1.2f)
        {
            lodLevel = Mathf.Max(lodLevel - 1, 0);
        }

        // 根据距离调整
        if (cameraDistance > settings.farDistance)
        {
            lodLevel = Mathf.Max(lodLevel, 2);
        }
        else if (cameraDistance < settings.nearDistance)
        {
            lodLevel = Mathf.Max(lodLevel - 1, 0);
        }

        return Mathf.Clamp(lodLevel, 0, settings.lodLevels - 1);
    }

    private int GetDataComplexity(FEMGeometryData femData)
    {
        int nodeCount = femData.nodeCount;
        int tetCount = femData.tetrahedronCount;

        // 根据数据量评估复杂度
        if (nodeCount > 5000 || tetCount > 2000) return 3; // 最高LOD
        if (nodeCount > 2000 || tetCount > 800) return 2;
        if (nodeCount > 800 || tetCount > 300) return 1;
        return 0; // 最低LOD
    }

    public int GetCurrentLODLevel()
    {
        return currentLODLevel;
    }

    public bool HasLODChanged()
    {
        bool changed = lodChanged;
        lodChanged = false;
        return changed;
    }

    public float GetSizeScale()
    {
        return 1f / (1f + currentLODLevel * 0.3f);
    }

    public int GetMaxNodesForCurrentLOD()
    {
        int baseMax = settings.maxNodesLOD0;
        return Mathf.Max(baseMax >> currentLODLevel, 10);
    }

    public int GetMaxTetrahedraForCurrentLOD()
    {
        int baseMax = settings.maxTetrahedraLOD0;
        return Mathf.Max(baseMax >> currentLODLevel, 5);
    }

    public int GetMaxSurfaceTrianglesForCurrentLOD()
    {
        int baseMax = settings.maxSurfaceTrianglesLOD0;
        return Mathf.Max(baseMax >> currentLODLevel, 10);
    }
}