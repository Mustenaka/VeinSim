using UnityEngine;

[System.Serializable]
public class OptimizedVisualizationSettings
{
    [Header("������ʾ")]
    public bool showBounds = true;
    public bool showNodes = true;
    public bool showTetrahedra = false;
    public bool showSurfaceTriangles = false;

    [Header("������ʾ")]
    public bool showPerformanceInfo = true;
    public bool showLODInfo = true;

    [Header("�ڵ�����")]
    public Color surfaceNodeColor = Color.red;
    public Color interiorNodeColor = Color.blue;
    public Color fixedNodeColor = Color.yellow;
    public float surfaceNodeSize = 0.08f;
    public float interiorNodeSize = 0.06f;
    public float fixedNodeSize = 0.1f;
    public bool useWireframeSphere = true;
    public bool showFixedNodeMarkers = true;

    [Header("����Ԫ��")]
    public Color boundsColor = Color.green;
    public Color tetrahedraColor = new Color(0f, 1f, 1f, 0.3f);
    public Color surfaceTriangleColor = Color.magenta;
    public bool showSurfaceNormals = false;
    public Color surfaceNormalColor = Color.yellow;
    public float surfaceNormalLength = 0.2f;

    [Header("�������")]
    public float maxDrawDistance = 30f;
}