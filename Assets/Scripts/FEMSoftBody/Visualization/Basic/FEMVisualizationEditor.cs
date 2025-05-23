namespace FEMSoftBody
{
    // FEMVisualizationEditor.cs - 自定义编辑器
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ConversionTest))]
    public class FEMVisualizationEditor : Editor
    {
        private ConversionTest script;
        private bool showVisualizationFoldout = true;
        private bool showNodesFoldout = true;
        private bool showTetrahedraFoldout = false;
        private bool showSurfaceFoldout = false;
        private bool showDetailsFoldout = false;

        void OnEnable()
        {
            script = (ConversionTest)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // 转换控制按钮
            if (GUILayout.Button("转换网格", GUILayout.Height(30)))
            {
                script.ConvertMesh();
            }

            EditorGUILayout.Space(10);

            // 可视化控制面板
            showVisualizationFoldout = EditorGUILayout.Foldout(showVisualizationFoldout, "可视化控制面板", true);
            if (showVisualizationFoldout)
            {
                DrawVisualizationControls();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }
        }

        private void DrawVisualizationControls()
        {
            var vis = script.visualSettings;

            EditorGUI.indentLevel++;

            // 通用控制
            EditorGUILayout.LabelField("通用显示", EditorStyles.boldLabel);
            vis.showBounds = EditorGUILayout.Toggle("显示包围盒", vis.showBounds);
            vis.showStatistics = EditorGUILayout.Toggle("显示统计信息", vis.showStatistics);
            vis.showDetailedInfo = EditorGUILayout.Toggle("显示详细信息", vis.showDetailedInfo);

            EditorGUILayout.Space(5);

            // 节点控制
            showNodesFoldout = EditorGUILayout.Foldout(showNodesFoldout, "节点显示");
            if (showNodesFoldout)
            {
                EditorGUI.indentLevel++;
                vis.showNodes = EditorGUILayout.Toggle("显示节点", vis.showNodes);
                if (vis.showNodes)
                {
                    vis.maxNodesToShow = EditorGUILayout.IntSlider("最大显示节点数", vis.maxNodesToShow, 10, 1000);
                    vis.surfaceNodeSize = EditorGUILayout.Slider("表面节点大小", vis.surfaceNodeSize, 0.01f, 0.2f);
                    vis.interiorNodeSize = EditorGUILayout.Slider("内部节点大小", vis.interiorNodeSize, 0.01f, 0.2f);
                    vis.useWireframeSphere = EditorGUILayout.Toggle("使用线框球体", vis.useWireframeSphere);
                    vis.showNodeIndices = EditorGUILayout.Toggle("显示节点索引", vis.showNodeIndices);
                    vis.showMassAsAlpha = EditorGUILayout.Toggle("质量影响透明度", vis.showMassAsAlpha);
                }
                EditorGUI.indentLevel--;
            }

            // 四面体控制
            showTetrahedraFoldout = EditorGUILayout.Foldout(showTetrahedraFoldout, "四面体显示");
            if (showTetrahedraFoldout)
            {
                EditorGUI.indentLevel++;
                vis.showTetrahedra = EditorGUILayout.Toggle("显示四面体", vis.showTetrahedra);
                if (vis.showTetrahedra)
                {
                    vis.maxTetrahedraToShow = EditorGUILayout.IntSlider("最大显示四面体数", vis.maxTetrahedraToShow, 10, 500);
                    vis.showTetrahedraAsWireframe = EditorGUILayout.Toggle("线框模式", vis.showTetrahedraAsWireframe);
                    vis.showTetrahedraCenters = EditorGUILayout.Toggle("显示中心点", vis.showTetrahedraCenters);
                }
                EditorGUI.indentLevel--;
            }

            // 表面三角形控制
            showSurfaceFoldout = EditorGUILayout.Foldout(showSurfaceFoldout, "表面显示");
            if (showSurfaceFoldout)
            {
                EditorGUI.indentLevel++;
                vis.showSurfaceTriangles = EditorGUILayout.Toggle("显示表面三角形", vis.showSurfaceTriangles);
                if (vis.showSurfaceTriangles)
                {
                    vis.maxSurfaceTrianglesToShow = EditorGUILayout.IntSlider("最大显示三角形数", vis.maxSurfaceTrianglesToShow, 10, 1000);
                    vis.showSurfaceNormals = EditorGUILayout.Toggle("显示法向量", vis.showSurfaceNormals);
                    if (vis.showSurfaceNormals)
                    {
                        vis.surfaceNormalLength = EditorGUILayout.Slider("法向量长度", vis.surfaceNormalLength, 0.1f, 1f);
                    }
                }
                EditorGUI.indentLevel--;
            }

            // 详细信息控制
            if (vis.showDetailedInfo)
            {
                showDetailsFoldout = EditorGUILayout.Foldout(showDetailsFoldout, "详细信息显示");
                if (showDetailsFoldout)
                {
                    EditorGUI.indentLevel++;
                    vis.showNodeConnections = EditorGUILayout.Toggle("显示节点连接", vis.showNodeConnections);
                    vis.showForceVectors = EditorGUILayout.Toggle("显示力向量", vis.showForceVectors);
                    vis.showMassDistribution = EditorGUILayout.Toggle("显示质量分布", vis.showMassDistribution);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // 快速预设按钮
            EditorGUILayout.LabelField("快速预设", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("仅节点"))
            {
                ResetVisualization();
                vis.showNodes = true;
                vis.showBounds = true;
            }

            if (GUILayout.Button("节点+四面体"))
            {
                ResetVisualization();
                vis.showNodes = true;
                vis.showTetrahedra = true;
                vis.showBounds = true;
            }

            if (GUILayout.Button("完整显示"))
            {
                vis.showNodes = true;
                vis.showTetrahedra = true;
                vis.showSurfaceTriangles = true;
                vis.showBounds = true;
                vis.showStatistics = true;
            }

            if (GUILayout.Button("重置"))
            {
                ResetVisualization();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ResetVisualization()
        {
            var vis = script.visualSettings;
            vis.showNodes = false;
            vis.showTetrahedra = false;
            vis.showSurfaceTriangles = false;
            vis.showBounds = true;
            vis.showStatistics = true;
            vis.showDetailedInfo = false;
        }
    }
#endif
}