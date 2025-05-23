using UnityEngine;

namespace FEMSoftBody
{
    [System.Serializable]
    public class FEMVisualizationSettings
    {
        [Header("通用设置")] public bool showBounds = true;
        public bool showNodes = true;
        public bool showTetrahedra = false;
        public bool showSurfaceTriangles = false;
        public bool showStatistics = true;
        public bool showDetailedInfo = false;

        [Header("节点显示")] public Color surfaceNodeColor = Color.red;
        public Color interiorNodeColor = Color.blue;
        public Color fixedNodeColor = Color.yellow;
        public float surfaceNodeSize = 0.08f;
        public float interiorNodeSize = 0.06f;
        public float fixedNodeSize = 0.1f;
        public bool useWireframeSphere = false;
        public bool showFixedNodeMarkers = true;
        public bool showNodeIndices = false;
        public bool showNodeInfo = false;
        public bool showMassAsAlpha = false;
        public int selectedNodeIndex = 0;
        public int maxNodesToShow = 500;
        public int maxIndicesToShow = 50;

        [Header("包围盒显示")] public Color boundsColor = Color.green;
        public bool showBoundsInfo = true;

        [Header("四面体显示")] public Color tetrahedraColor = Color.cyan;
        public bool showTetrahedraAsWireframe = true;
        public bool showTetrahedraCenters = false;
        public Color tetrahedraCenterColor = Color.white;
        public float tetrahedraCenterSize = 0.03f;
        public bool showTetrahedronInfo = false;
        public int selectedTetrahedronIndex = 0;
        public int maxTetrahedraToShow = 100;

        [Header("表面三角形显示")] public Color surfaceTriangleColor = Color.magenta;
        public bool showSurfaceTriangleWireframe = true;
        public bool showSurfaceNormals = false;
        public Color surfaceNormalColor = Color.yellow;
        public float surfaceNormalLength = 0.2f;
        public int maxSurfaceTrianglesToShow = 200;

        [Header("详细信息显示")] public bool showNodeConnections = false;
        public Color connectionLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public int maxConnectionsToShow = 50;

        public bool showForceVectors = false;
        public Color forceVectorColor = Color.red;
        public float forceVectorScale = 0.001f;
        public float maxForceVectorLength = 0.5f;
        public int maxForceVectorsToShow = 100;

        public bool showMassDistribution = false;
        public int maxMassVisualizationNodes = 200;
    }
}