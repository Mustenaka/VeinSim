using UnityEngine;

[System.Serializable]
public class OptimizedVisualizationSettings
{
    [Header("基础显示")]
    public bool showBounds = true;
    public bool showNodes = true;
    public bool showTetrahedra = false;
    public bool showSurfaceTriangles = false;

    [Header("性能显示")]
    public bool showPerformanceInfo = true;
    public bool showLODInfo = true;

    [Header("节点设置")]
    public Color surfaceNodeColor = Color.red;
    public Color interiorNodeColor = Color.blue;
    public Color fixedNodeColor = Color.yellow;
    public float surfaceNodeSize = 0.08f;
    public float interiorNodeSize = 0.06f;
    public float fixedNodeSize = 0.1f;
    public bool useWireframeSphere = true;
    public bool showFixedNodeMarkers = true;

    [Header("其他元素")]
    public Color boundsColor = Color.green;
    public Color tetrahedraColor = new Color(0f, 1f, 1f, 0.3f);
    public Color surfaceTriangleColor = Color.magenta;
    public bool showSurfaceNormals = false;
    public Color surfaceNormalColor = Color.yellow;
    public float surfaceNormalLength = 0.2f;

    [Header("距离控制")]
    public float maxDrawDistance = 30f;
}