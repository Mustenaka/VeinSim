using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 主管理器类，负责管理场景中需要体素化的对象
/// </summary>
public class VoxelizationManager : MonoBehaviour
{
    // 单例实例
    private static VoxelizationManager _instance;
    public static VoxelizationManager Instance
    {
        get
        {
            // 如果实例为null，尝试在场景中找到它
            if (_instance == null)
            {
                _instance = FindObjectOfType<VoxelizationManager>();

                // 如果场景中没有，在编辑器模式下创建一个新的实例
#if UNITY_EDITOR
                if (_instance == null && !Application.isPlaying)
                {
                    GameObject managerObject = new GameObject("Voxelization Manager");
                    _instance = managerObject.AddComponent<VoxelizationManager>();
                    Debug.Log("自动创建了Voxelization Manager，因为它不存在");

                    // 初始化实例
                    _instance.InitializeIfNeeded();
                }
#endif
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("体素化设置")]
    [Tooltip("默认分辨率")]
    public int defaultResolution = 32;

    [Tooltip("最大八叉树深度")]
    public int maxOctreeDepth = 8;

    [Tooltip("最小体素大小")]
    public float minVoxelSize = 0.01f;

    [Header("渲染设置")]
    [Tooltip("体素材质")]
    public Material voxelMaterial;

    // 体素渲染器
    private VoxelRenderer _voxelRenderer;

    // 可体素化对象列表
    private List<VoxelizableObject> _voxelizableObjects = new List<VoxelizableObject>();

    // 每个对象的八叉树
    private Dictionary<VoxelizableObject, Octree> _octrees = new Dictionary<VoxelizableObject, Octree>();

    private void Awake()
    {
        InitializeIfNeeded();
    }

    /// <summary>
    /// 确保管理器正确初始化
    /// </summary>
    private void InitializeIfNeeded()
    {
        // 实现单例模式
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        _instance = this;

        // 确保场景中有体素渲染器
        _voxelRenderer = GetComponent<VoxelRenderer>();
        if (_voxelRenderer == null)
        {
            _voxelRenderer = gameObject.AddComponent<VoxelRenderer>();
        }

        // 设置体素材质
        if (voxelMaterial != null)
        {
            _voxelRenderer.voxelMaterial = voxelMaterial;
        }
    }

    /// <summary>
    /// 注册可体素化对象
    /// </summary>
    public void RegisterVoxelizableObject(VoxelizableObject obj)
    {
        if (!_voxelizableObjects.Contains(obj))
        {
            _voxelizableObjects.Add(obj);
            CreateOctreeForObject(obj);
            UpdateVoxelization(obj);
        }
    }

    /// <summary>
    /// 注销可体素化对象
    /// </summary>
    public void UnregisterVoxelizableObject(VoxelizableObject obj)
    {
        if (_voxelizableObjects.Contains(obj))
        {
            _voxelizableObjects.Remove(obj);

            if (_octrees.ContainsKey(obj))
            {
                _octrees.Remove(obj);
            }

            UpdateVoxelVisualization();
        }
    }

    /// <summary>
    /// 为对象创建八叉树
    /// </summary>
    private void CreateOctreeForObject(VoxelizableObject obj)
    {
        // 获取对象的边界
        Mesh bakedMesh = obj.GetBakedMesh();
        Bounds bounds = bakedMesh.bounds;

        // 转换到世界空间并添加一些填充
        bounds.center = obj.transform.TransformPoint(bounds.center);
        bounds.size = Vector3.Scale(bounds.size, obj.transform.lossyScale) * 1.1f;

        // 创建八叉树
        Octree octree = new Octree(bounds, maxOctreeDepth, minVoxelSize);
        _octrees[obj] = octree;
    }

    /// <summary>
    /// 更新对象的体素化
    /// </summary>
    public void UpdateVoxelization(VoxelizableObject obj)
    {
        if (!_octrees.ContainsKey(obj))
        {
            CreateOctreeForObject(obj);
        }

        Debug.Log("CreateOctreeForObject");

        Octree octree = _octrees[obj];
        Mesh bakedMesh = obj.GetBakedMesh();

        Debug.Log("GetBakedMesh");

        // 体素化网格
        octree.VoxelizeMesh(bakedMesh, obj.transform, obj.voxelColor, obj.resolution);

        Debug.Log("VoxelizeMesh");

        // 更新可视化
        UpdateVoxelVisualization();

        Debug.Log("UpdateVoxelVisualization");

        // 检查渲染器是否使用GameObject模式
        VoxelRenderer renderer = GetComponent<VoxelRenderer>();
        if (renderer != null && renderer.useGameObjects)
        {
            // 调用RenderWithGameObjects方法
            renderer.RenderWithGameObjects();
        }
    }

    /// <summary>
    /// 更新体素可视化
    /// </summary>
    private void UpdateVoxelVisualization()
    {
        List<Voxel> allVoxels = new List<Voxel>();

        // 收集所有体素
        foreach (var kvp in _octrees)
        {
            List<OctreeNode> voxelNodes = kvp.Value.GetVoxelNodes();
            foreach (OctreeNode node in voxelNodes)
            {
                allVoxels.Add(Voxel.FromOctreeNode(node));
            }
        }

        // 更新渲染
        _voxelRenderer.SetVoxels(allVoxels);
    }

    /// <summary>
    /// 清除所有体素
    /// </summary>
    public void ClearAllVoxels()
    {
        foreach (var kvp in _octrees)
        {
            kvp.Value.ClearVoxels();
        }

        _voxelRenderer.ClearVoxels();
    }

    /// <summary>
    /// 设置全局渲染体素的可见性
    /// </summary>
    public void SetVoxelsVisible(bool visible)
    {
        _voxelRenderer.enabled = visible;
    }

    /// <summary>
    /// 检查管理器实例是否有效，如果不存在则尝试创建
    /// </summary>
    public static bool EnsureInstanceExists()
    {
        if (Instance == null)
        {
            Debug.LogError("无法找到或创建VoxelizationManager实例");
            return false;
        }
        return true;
    }

#if UNITY_EDITOR
    // 获取所有已注册的可体素化对象
    public List<VoxelizableObject> GetRegisteredObjects()
    {
        return new List<VoxelizableObject>(_voxelizableObjects);
    }

    // 获取所有已创建的八叉树
    public Dictionary<VoxelizableObject, Octree> GetOctrees()
    {
        return _octrees;
    }
#endif
}

