using UnityEngine;

[System.Serializable]
public class FEMPerformanceSettings
{
    [Header("性能目标")]
    [Tooltip("目标帧率")]
    public float targetFPS = 30f;

    [Tooltip("最低可接受帧率")]
    public float minimumFPS = 15f;

    [Tooltip("性能监控窗口大小")]
    public int performanceWindowSize = 30;

    [Header("LOD设置")]
    [Tooltip("LOD级别数量")]
    public int lodLevels = 4;

    [Tooltip("自动LOD调整")]
    public bool autoLODAdjustment = true;

    [Tooltip("LOD调整灵敏度")]
    public float lodAdjustmentSensitivity = 1f;

    [Header("渲染限制")]
    [Tooltip("最大节点数（LOD 0）")]
    public int maxNodesLOD0 = 200;

    [Tooltip("最大四面体数（LOD 0）")]
    public int maxTetrahedraLOD0 = 50;

    [Tooltip("最大表面三角形数（LOD 0）")]
    public int maxSurfaceTrianglesLOD0 = 100;

    [Header("距离设置")]
    [Tooltip("近距离阈值")]
    public float nearDistance = 5f;

    [Tooltip("远距离阈值")]
    public float farDistance = 20f;

    [Tooltip("最大渲染距离")]
    public float maxRenderDistance = 50f;
}