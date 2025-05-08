using UnityEngine;

/// <summary>
/// 标记可体素化对象并存储特定设置的组件
/// </summary>
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class VoxelizableObject : MonoBehaviour
{
    [Header("体素化设置")]
    [Tooltip("体素化分辨率（每个轴向的体素数量）")]
    public int resolution = 32;

    [Tooltip("是否自动更新体素（对于动画模型）")]
    public bool autoUpdate = false;

    [Tooltip("自动更新的帧率（每秒更新次数）")]
    public float updateRate = 1.0f;

    [Tooltip("体素的颜色")]
    public Color voxelColor = Color.white;

    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private Mesh _bakedMesh;
    private float _updateTimer;

    private void Awake()
    {
        _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        _bakedMesh = new Mesh();
    }

    private void OnEnable()
    {
        // 在启用时注册到管理器
        if (VoxelizationManager.EnsureInstanceExists())
        {
            VoxelizationManager.Instance.RegisterVoxelizableObject(this);
        }
    }

    private void OnDisable()
    {
        // 在禁用时从管理器中注销
        if (VoxelizationManager.Instance != null)
        {
            VoxelizationManager.Instance.UnregisterVoxelizableObject(this);
        }
    }

    private void Update()
    {
        if (autoUpdate)
        {
            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = 1f / updateRate;
                VoxelizationManager.Instance.UpdateVoxelization(this);
            }
        }
    }

    /// <summary>
    /// 获取当前的烘焙网格
    /// </summary>
    public Mesh GetBakedMesh()
    {
        _skinnedMeshRenderer.BakeMesh(_bakedMesh);
        return _bakedMesh;
    }

    /// <summary>
    /// 手动触发体素化更新
    /// </summary>
    public void TriggerVoxelizationUpdate()
    {
        // 确保管理器实例存在
        if (VoxelizationManager.Instance != null)
        {
            VoxelizationManager.Instance.UpdateVoxelization(this);
        }
        else
        {
            Debug.LogError("无法找到VoxelizationManager实例。请确保场景中存在VoxelizationManager对象。", this);
        }
    }
}