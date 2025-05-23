using UnityEngine;

[System.Serializable]
public class FEMPerformanceSettings
{
    [Header("����Ŀ��")]
    [Tooltip("Ŀ��֡��")]
    public float targetFPS = 30f;

    [Tooltip("��Ϳɽ���֡��")]
    public float minimumFPS = 15f;

    [Tooltip("���ܼ�ش��ڴ�С")]
    public int performanceWindowSize = 30;

    [Header("LOD����")]
    [Tooltip("LOD��������")]
    public int lodLevels = 4;

    [Tooltip("�Զ�LOD����")]
    public bool autoLODAdjustment = true;

    [Tooltip("LOD����������")]
    public float lodAdjustmentSensitivity = 1f;

    [Header("��Ⱦ����")]
    [Tooltip("���ڵ�����LOD 0��")]
    public int maxNodesLOD0 = 200;

    [Tooltip("�������������LOD 0��")]
    public int maxTetrahedraLOD0 = 50;

    [Tooltip("����������������LOD 0��")]
    public int maxSurfaceTrianglesLOD0 = 100;

    [Header("��������")]
    [Tooltip("��������ֵ")]
    public float nearDistance = 5f;

    [Tooltip("Զ������ֵ")]
    public float farDistance = 20f;

    [Tooltip("�����Ⱦ����")]
    public float maxRenderDistance = 50f;
}