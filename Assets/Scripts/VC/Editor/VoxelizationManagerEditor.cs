using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// VoxelizationManager的编辑器扩展
/// </summary>
[CustomEditor(typeof(VoxelizationManager))]
public class VoxelizationManagerEditor : Editor
{
    // 折叠区域状态
    private bool _showSettings = true;
    private bool _showObjects = true;
    private bool _showOperations = true;
    private bool _showVisibilityControls = true;

    // 自动添加的对象列表
    private List<SkinnedMeshRenderer> _foundRenderers = new List<SkinnedMeshRenderer>();

    // 过滤选项
    private bool _excludeDisabledRenderers = true;
    private string _nameFilter = "";

    // 是否已添加组件
    private bool[] _hasComponent;

    // 分辨率设置
    private int _batchResolution = 32;

    // 透明度设置
    private float _transparencyLevel = 0.5f;

    // 保存原始材质和设置
    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, bool> _originalVisibility = new Dictionary<Renderer, bool>();

    // 透明材质缓存
    private Material _transparentMaterial;

    // 渲染 or GameObject
    private bool _showRenderingOptions = true;

    public override void OnInspectorGUI()
    {
        // 获取目标组件
        VoxelizationManager manager = (VoxelizationManager)target;

        // 使用Unity的撤销功能
        Undo.RecordObject(manager, "Modified VoxelizationManager");

        // 样式定义
        GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
        headerStyle.fontStyle = FontStyle.Bold;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        boxStyle.margin = new RectOffset(0, 0, 10, 10);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(15, 15, 5, 5);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("体素化管理器", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 设置部分
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "体素化设置", true, headerStyle);

            if (_showSettings)
            {
                EditorGUI.indentLevel++;

                manager.defaultResolution = EditorGUILayout.IntSlider(new GUIContent("默认分辨率", "每个轴向的体素数量"),
                    manager.defaultResolution, 8, 128);

                manager.maxOctreeDepth = EditorGUILayout.IntSlider(new GUIContent("最大八叉树深度", "八叉树的最大深度，影响细节和性能"),
                    manager.maxOctreeDepth, 4, 12);

                manager.minVoxelSize = EditorGUILayout.FloatField(new GUIContent("最小体素大小", "体素的最小尺寸"),
                    manager.minVoxelSize);

                manager.voxelMaterial = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("体素材质", "用于渲染体素的材质"),
                    manager.voxelMaterial, typeof(Material), false);

                EditorGUI.indentLevel--;
            }
        }

        // 对象搜索部分
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            _showObjects = EditorGUILayout.Foldout(_showObjects, "场景对象", true, headerStyle);

            if (_showObjects)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                _nameFilter = EditorGUILayout.TextField(new GUIContent("名称过滤", "仅包含名称包含此文本的对象"), _nameFilter);
                if (GUILayout.Button("清除", GUILayout.Width(60)))
                {
                    _nameFilter = "";
                }
                EditorGUILayout.EndHorizontal();

                _excludeDisabledRenderers = EditorGUILayout.Toggle(
                    new GUIContent("排除禁用的渲染器", "不包括禁用的SkinnedMeshRenderer"),
                    _excludeDisabledRenderers);

                EditorGUILayout.Space(5);

                if (GUILayout.Button("搜索场景中的蒙皮网格渲染器", buttonStyle))
                {
                    SearchForSkinnedMeshRenderers();
                }

                DisplayFoundRenderers(manager);

                EditorGUI.indentLevel--;
            }
        }

        // 操作部分
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            _showOperations = EditorGUILayout.Foldout(_showOperations, "体素化操作", true, headerStyle);

            if (_showOperations)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("批量操作分辨率");
                _batchResolution = EditorGUILayout.IntSlider(_batchResolution, 8, 128);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("体素化所有对象", buttonStyle))
                {
                    VoxelizeAllObjects(manager);
                }

                if (GUILayout.Button("清除所有体素", buttonStyle))
                {
                    manager.ClearAllVoxels();
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("显示体素", buttonStyle))
                {
                    manager.SetVoxelsVisible(true);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("隐藏体素", buttonStyle))
                {
                    manager.SetVoxelsVisible(false);
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }

        // 原始对象可见性控制部分
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            _showVisibilityControls = EditorGUILayout.Foldout(_showVisibilityControls, "原始对象可见性控制", true, headerStyle);

            if (_showVisibilityControls)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5);

                // 透明度滑块
                EditorGUILayout.LabelField("透明度控制", EditorStyles.boldLabel);
                _transparencyLevel = EditorGUILayout.Slider("透明度级别", _transparencyLevel, 0f, 1f);

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("显示原始对象", buttonStyle))
                {
                    ShowOriginalObjects();
                }

                if (GUILayout.Button("隐藏原始对象", buttonStyle))
                {
                    HideOriginalObjects();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("使对象透明", buttonStyle))
                {
                    MakeObjectsTransparent();
                }

                if (GUILayout.Button("恢复原始材质", buttonStyle))
                {
                    RestoreOriginalMaterials();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }

        // 在OnInspectorGUI方法中添加新的UI部分
        using (new EditorGUILayout.VerticalScope(boxStyle))
        {
            _showRenderingOptions = EditorGUILayout.Foldout(_showRenderingOptions, "渲染选项", true, headerStyle);

            if (_showRenderingOptions)
            {
                EditorGUI.indentLevel++;

                // 获取VoxelRenderer组件
                VoxelRenderer renderer = manager.gameObject.GetComponent<VoxelRenderer>();
                if (renderer != null)
                {
                    // 记录撤销
                    Undo.RecordObject(renderer, "Modified VoxelRenderer");

                    // GameObject渲染切换
                    bool useGameObjects = EditorGUILayout.Toggle(
                        new GUIContent("使用GameObject盒子", "使用实际的GameObject盒子而不是实例化渲染"),
                        renderer.useGameObjects);

                    // 如果值发生变化
                    if (useGameObjects != renderer.useGameObjects)
                    {
                        renderer.useGameObjects = useGameObjects;

                        EditorUtility.SetDirty(renderer);

                        if (useGameObjects)
                        {
                            renderer.RenderWithGameObjects();
                            EditorGUILayout.HelpBox("警告：使用大量GameObject盒子可能会影响性能。建议仅在低分辨率下使用此选项。", MessageType.Warning);
                        }
                        else
                        {
                            // 调用私有方法清除GameObject体素
                            System.Reflection.MethodInfo clearMethod = typeof(VoxelRenderer).GetMethod(
                                "ClearVoxelGameObjects",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(renderer, null);
                            }
                        }

                        SceneView.RepaintAll();
                    }

                    // 如果使用GameObject渲染，添加一些操作按钮
                    if (renderer.useGameObjects)
                    {
                        EditorGUILayout.Space(5);

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("重新生成GameObject盒子", buttonStyle))
                        {
                            renderer.RenderWithGameObjects();
                            SceneView.RepaintAll();
                        }

                        if (GUILayout.Button("导出为预制体", buttonStyle))
                        {
                            ExportVoxelsAsPrefab(renderer);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("未找到VoxelRenderer组件", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
            }
        }


        // 应用更改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    // 添加导出为预制体的方法
    private void ExportVoxelsAsPrefab(VoxelRenderer renderer)
    {
        // 获取voxel容器的引用（通过反射）
        System.Reflection.FieldInfo containerField = typeof(VoxelRenderer).GetField(
            "_voxelContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (containerField != null)
        {
            Transform voxelContainer = containerField.GetValue(renderer) as Transform;

            if (voxelContainer != null && voxelContainer.childCount > 0)
            {
                // 选择保存路径
                string path = EditorUtility.SaveFilePanelInProject(
                    "保存体素模型预制体",
                    "VoxelModel",
                    "prefab",
                    "请选择保存体素模型预制体的位置");

                if (!string.IsNullOrEmpty(path))
                {
                    // 创建一个临时GameObject
                    GameObject prefabRoot = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));

                    // 复制所有体素子对象
                    foreach (Transform child in voxelContainer)
                    {
                        GameObject copy = Instantiate(child.gameObject);
                        copy.transform.SetParent(prefabRoot.transform, false);
                        copy.transform.localPosition = child.localPosition;
                        copy.transform.localRotation = child.localRotation;
                        copy.transform.localScale = child.localScale;
                    }

                    // 创建预制体
#if UNITY_2018_3_OR_NEWER
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
#else
                GameObject prefab = PrefabUtility.CreatePrefab(path, prefabRoot);
#endif

                    // 清理
                    DestroyImmediate(prefabRoot);

                    // 选择新创建的预制体
                    if (prefab != null)
                    {
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }

                    Debug.Log("体素模型预制体已保存到: " + path);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "没有找到体素对象，请先生成GameObject盒子", "确定");
            }
        }
    }

    /// <summary>
    /// 搜索场景中的所有SkinnedMeshRenderer
    /// </summary>
    private void SearchForSkinnedMeshRenderers()
    {
        _foundRenderers.Clear();

        // 查找场景中的所有SkinnedMeshRenderer
        SkinnedMeshRenderer[] renderers = FindObjectsOfType<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            // 应用过滤条件
            if (_excludeDisabledRenderers && !renderer.enabled)
                continue;

            if (!string.IsNullOrEmpty(_nameFilter) && !renderer.gameObject.name.Contains(_nameFilter))
                continue;

            _foundRenderers.Add(renderer);

            // 保存原始可见性状态
            _originalVisibility[renderer] = renderer.enabled;
        }

        // 初始化"已添加组件"数组
        _hasComponent = new bool[_foundRenderers.Count];

        // 检查哪些对象已经有VoxelizableObject组件
        for (int i = 0; i < _foundRenderers.Count; i++)
        {
            _hasComponent[i] = _foundRenderers[i].GetComponent<VoxelizableObject>() != null;
        }
    }

    /// <summary>
    /// 显示找到的渲染器列表
    /// </summary>
    private void DisplayFoundRenderers(VoxelizationManager manager)
    {
        if (_foundRenderers.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到符合条件的蒙皮网格渲染器。", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("找到的对象:", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(60)))
        {
            for (int i = 0; i < _hasComponent.Length; i++)
            {
                _hasComponent[i] = true;
            }
        }

        if (GUILayout.Button("全不选", GUILayout.Width(60)))
        {
            for (int i = 0; i < _hasComponent.Length; i++)
            {
                _hasComponent[i] = false;
            }
        }

        if (GUILayout.Button("应用选择", GUILayout.Width(80)))
        {
            ApplySelection(manager);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 滚动视图
        using (var scrollView = new EditorGUILayout.ScrollViewScope(
            new Vector2(0, 0), GUILayout.Height(Mathf.Min(300, _foundRenderers.Count * 20))))
        {
            for (int i = 0; i < _foundRenderers.Count; i++)
            {
                SkinnedMeshRenderer renderer = _foundRenderers[i];

                if (renderer == null)
                    continue;

                EditorGUILayout.BeginHorizontal();

                // 复选框
                _hasComponent[i] = EditorGUILayout.Toggle(_hasComponent[i], GUILayout.Width(20));

                // 对象名称
                EditorGUILayout.ObjectField(renderer.gameObject, typeof(GameObject), true);

                // 状态标签
                VoxelizableObject voxelObj = renderer.GetComponent<VoxelizableObject>();

                if (voxelObj != null)
                {
                    GUILayout.Label("已添加", EditorStyles.miniLabel, GUILayout.Width(60));

                    if (GUILayout.Button("体素化", GUILayout.Width(60)))
                    {
                        voxelObj.TriggerVoxelizationUpdate();
                        SceneView.RepaintAll();
                    }
                }
                else
                {
                    GUILayout.Label("未添加", EditorStyles.miniLabel, GUILayout.Width(60));

                    if (GUILayout.Button("添加", GUILayout.Width(60)))
                    {
                        voxelObj = renderer.gameObject.AddComponent<VoxelizableObject>();
                        voxelObj.resolution = manager.defaultResolution;
                        _hasComponent[i] = true;

                        Undo.RegisterCreatedObjectUndo(voxelObj, "Add VoxelizableObject");
                        EditorUtility.SetDirty(renderer.gameObject);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 应用用户选择
    /// </summary>
    private void ApplySelection(VoxelizationManager manager)
    {
        for (int i = 0; i < _foundRenderers.Count; i++)
        {
            SkinnedMeshRenderer renderer = _foundRenderers[i];

            if (renderer == null)
                continue;

            VoxelizableObject voxelObj = renderer.GetComponent<VoxelizableObject>();

            if (_hasComponent[i])
            {
                // 如果需要添加组件但尚未添加
                if (voxelObj == null)
                {
                    voxelObj = Undo.AddComponent<VoxelizableObject>(renderer.gameObject);
                    voxelObj.resolution = manager.defaultResolution;
                    EditorUtility.SetDirty(renderer.gameObject);
                }
            }
            else
            {
                // 如果需要移除组件但已存在
                if (voxelObj != null)
                {
                    Undo.DestroyObjectImmediate(voxelObj);
                    EditorUtility.SetDirty(renderer.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 体素化所有对象
    /// </summary>
    private void VoxelizeAllObjects(VoxelizationManager manager)
    {
        // 确保管理器有效
        if (manager == null)
        {
            EditorUtility.DisplayDialog("错误", "找不到有效的VoxelizationManager实例。", "确定");
            return;
        }

        VoxelizableObject[] voxelObjects = FindObjectsOfType<VoxelizableObject>();

        if (voxelObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("体素化", "场景中没有可体素化的对象。请先添加VoxelizableObject组件到蒙皮网格对象上。", "确定");
            return;
        }

        // 询问是否更改分辨率
        bool changeResolution = EditorUtility.DisplayDialog("体素化",
            "是否要将所有对象的分辨率设置为 " + _batchResolution + "？", "是", "否");

        try
        {
            for (int i = 0; i < voxelObjects.Length; i++)
            {
                VoxelizableObject voxelObj = voxelObjects[i];

                if (voxelObj == null)
                    continue;

                if (changeResolution)
                {
                    Undo.RecordObject(voxelObj, "Change Resolution");
                    voxelObj.resolution = _batchResolution;
                    EditorUtility.SetDirty(voxelObj);
                }

                // 显示进度条
                EditorUtility.DisplayProgressBar("体素化对象",
                    "正在处理: " + voxelObj.gameObject.name, (float)i / voxelObjects.Length);

                try
                {
                    // 确保VoxelizationManager实例已经创建
                    if (VoxelizationManager.Instance == null)
                    {
                        // 如果管理器为null，尝试使用传入的manager参数
                        if (manager != null)
                        {
                            manager.UpdateVoxelization(voxelObj);
                        }
                        else
                        {
                            Debug.LogError("无法找到VoxelizationManager实例", voxelObj);
                            continue;
                        }
                    }
                    else
                    {
                        voxelObj.TriggerVoxelizationUpdate();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("对象 " + voxelObj.gameObject.name + " 体素化失败: " + e.Message, voxelObj);
                    continue;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            SceneView.RepaintAll();
        }
    }

    /// <summary>
    /// 创建菜单项在场景中添加VoxelizationManager
    /// </summary>
    [MenuItem("GameObject/3D Object/Voxelization Manager", false, 10)]
    static void CreateVoxelizationManager(MenuCommand menuCommand)
    {
        // 创建一个空对象
        GameObject gameObject = new GameObject("Voxelization Manager");

        // 添加VoxelizationManager组件
        VoxelizationManager manager = gameObject.AddComponent<VoxelizationManager>();

        // 添加VoxelRenderer组件
        VoxelRenderer renderer = gameObject.AddComponent<VoxelRenderer>();

        // 创建URP材质
        Material material = null;
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpShader != null)
        {
            material = new Material(urpShader);
            material.SetFloat("_Smoothness", 0.5f);
            material.SetFloat("_Metallic", 0.0f);
            // URP使用_BaseColor而非_Color
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", Color.white);
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            // 备选方案使用标准着色器
            Debug.LogWarning("找不到URP着色器，使用标准着色器作为替代。请确保项目使用了正确的渲染管线。");
            material = new Material(Shader.Find("Standard"));
            material.color = Color.white;
            material.SetColor("_EmissionColor", Color.white);
            material.EnableKeyword("_EMISSION");
        }

        // 启用GPU实例化
        material.enableInstancing = true;

        // 设置默认材质
        manager.voxelMaterial = material;
        renderer.voxelMaterial = material;

        // 注册撤销操作
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Voxelization Manager");

        // 设置为选中项
        Selection.activeObject = gameObject;
    }

    /// <summary>
    /// 隐藏原始对象
    /// </summary>
    private void HideOriginalObjects()
    {
        // 保存原始可见性
        foreach (SkinnedMeshRenderer renderer in _foundRenderers)
        {
            if (renderer != null)
            {
                _originalVisibility[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 显示原始对象
    /// </summary>
    private void ShowOriginalObjects()
    {
        foreach (SkinnedMeshRenderer renderer in _foundRenderers)
        {
            if (renderer != null)
            {
                // 如果有保存的原始状态，恢复它，否则启用
                if (_originalVisibility.ContainsKey(renderer))
                {
                    renderer.enabled = _originalVisibility[renderer];
                }
                else
                {
                    renderer.enabled = true;
                }
            }
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 使对象透明
    /// </summary>
    private void MakeObjectsTransparent()
    {
        // 创建透明材质
        if (_transparentMaterial == null)
        {
            // 尝试创建URP透明材质
            _transparentMaterial = CreateURPTransparentMaterial();

            // 如果URP材质创建失败，回退到标准渲染管线
            if (_transparentMaterial == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    _transparentMaterial = new Material(standardShader);
                    _transparentMaterial.SetFloat("_Mode", 3); // 透明模式
                    _transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _transparentMaterial.SetInt("_ZWrite", 0);
                    _transparentMaterial.DisableKeyword("_ALPHATEST_ON");
                    _transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
                    _transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    _transparentMaterial.renderQueue = 3000;
                }
                else
                {
                    Debug.LogError("无法找到合适的着色器创建透明材质");
                    return;
                }
            }
        }

        // 更新透明度
        Color transparentColor = new Color(1, 1, 1, 1 - _transparencyLevel);

        // 如果是URP材质
        if (_transparentMaterial.HasProperty("_BaseColor"))
        {
            _transparentMaterial.SetColor("_BaseColor", transparentColor);
        }
        // 如果是标准材质
        else if (_transparentMaterial.HasProperty("_Color"))
        {
            _transparentMaterial.SetColor("_Color", transparentColor);
        }

        // 应用透明材质到所有渲染器
        foreach (SkinnedMeshRenderer renderer in _foundRenderers)
        {
            if (renderer != null && renderer.enabled)
            {
                // 保存原始材质
                if (!_originalMaterials.ContainsKey(renderer))
                {
                    _originalMaterials[renderer] = renderer.sharedMaterials;
                }

                // 创建新的材质数组
                Material[] transparentMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < transparentMaterials.Length; i++)
                {
                    transparentMaterials[i] = _transparentMaterial;
                }

                // 应用透明材质
                renderer.sharedMaterials = transparentMaterials;
            }
        }

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 恢复原始材质
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in _originalMaterials)
        {
            Renderer renderer = kvp.Key;
            Material[] originalMats = kvp.Value;

            if (renderer != null)
            {
                renderer.sharedMaterials = originalMats;
            }
        }

        // 清除缓存
        _originalMaterials.Clear();

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 在编辑器退出时清理资源
    /// </summary>
    private void OnDestroy()
    {
        // 恢复所有原始材质
        RestoreOriginalMaterials();

        // 销毁临时材质
        if (_transparentMaterial != null)
        {
            DestroyImmediate(_transparentMaterial);
            _transparentMaterial = null;
        }
    }

    /// <summary>
    /// 创建URP透明材质
    /// </summary>
    private Material CreateURPTransparentMaterial()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpShader == null)
        {
            Debug.LogError("找不到URP Lit着色器，请确保已正确安装URP渲染管线");
            return null;
        }

        Material transparentMaterial = new Material(urpShader);

        // 设置表面类型为透明
        transparentMaterial.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent

        // 设置混合模式为Alpha
        transparentMaterial.SetFloat("_Blend", 0);   // 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply

        // 禁用深度写入
        transparentMaterial.SetFloat("_ZWrite", 0);

        // 设置渲染队列为透明
        transparentMaterial.renderQueue = 3000;

        // 设置默认颜色
        transparentMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.5f));

        // 启用关键字
        transparentMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        transparentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return transparentMaterial;
    }
}