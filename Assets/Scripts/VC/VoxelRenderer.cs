using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责渲染体素
/// </summary>
public class VoxelRenderer : MonoBehaviour
{
    [Header("渲染设置")]
    [Tooltip("是否使用GPU实例化（提高性能）")]
    public bool useInstancing = true;

    [Tooltip("体素材质")]
    public Material voxelMaterial;

    // 体素网格（简单的立方体）
    private Mesh _boxMesh;

    // 当前渲染的体素
    private List<Voxel> _voxels = new List<Voxel>();

    // 实例化渲染所需的矩阵
    private List<Matrix4x4> _matrices = new List<Matrix4x4>();

    // 实例化渲染所需的颜色属性
    private List<Vector4> _colors = new List<Vector4>();

    // 材质属性块（用于批量设置属性）
    private MaterialPropertyBlock _propertyBlock;

    // 每批实例的最大数量
    private const int MaxInstancesPerBatch = 1023;

    [Header("GameObject渲染设置")]
    [Tooltip("是否使用GameObject盒子进行渲染")]
    public bool useGameObjects = false;

    [Tooltip("GameObject盒子的父对象")]
    private Transform _voxelContainer;

    // 当前的GameObject体素列表
    private List<GameObject> _voxelGameObjects = new List<GameObject>();

    private void Awake()
    {
        // 创建立方体网格
        _boxMesh = CreateBoxMesh();

        // 如果没有提供材质，创建一个URP默认材质
        if (voxelMaterial == null)
        {
            // 尝试获取URP的Lit Shader
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpShader != null)
            {
                voxelMaterial = new Material(urpShader);
                // 设置基本属性
                voxelMaterial.SetFloat("_Smoothness", 0.5f);
                voxelMaterial.SetFloat("_Metallic", 0.0f);
            }
            else
            {
                // 如果找不到URP着色器，尝试使用内置着色器作为备选
                Debug.LogWarning("找不到URP Lit着色器，使用备选着色器", this);
                voxelMaterial = new Material(Shader.Find("Standard"));
                voxelMaterial.EnableKeyword("_EMISSION");
            }
        }

        // 确保启用GPU实例化，不论使用哪种着色器
        if (voxelMaterial != null)
        {
            voxelMaterial.enableInstancing = true;
        }

        _propertyBlock = new MaterialPropertyBlock();
    }

    // 新增方法: 使用GameObject渲染体素
    public void RenderWithGameObjects()
    {
        // 清除现有的GameObject体素
        ClearVoxelGameObjects();

        // 如果没有体素或不使用GameObject渲染，直接返回
        if (_voxels.Count == 0 || !useGameObjects)
            return;

        // 创建容器对象（如果不存在）
        if (_voxelContainer == null)
        {
            GameObject container = new GameObject("Voxel Container");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            _voxelContainer = container.transform;
        }

        // 为每个体素创建一个GameObject
        for (int i = 0; i < _voxels.Count; i++)
        {
            Voxel voxel = _voxels[i];

            // 创建立方体
            GameObject voxelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            voxelObj.name = "Voxel_" + i;
            voxelObj.transform.SetParent(_voxelContainer);
            voxelObj.transform.position = voxel.Position;
            voxelObj.transform.localScale = voxel.Size;

            // 设置材质和颜色
            Renderer renderer = voxelObj.GetComponent<Renderer>();

            // 创建材质实例
            Material mat = new Material(voxelMaterial);

            // 设置颜色（URP或标准渲染管线）
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", voxel.Color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", voxel.Color);
            }

            renderer.material = mat;

            // 添加到列表
            _voxelGameObjects.Add(voxelObj);
        }
    }

    // 清除所有GameObject体素
    private void ClearVoxelGameObjects()
    {
        foreach (GameObject obj in _voxelGameObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }

        _voxelGameObjects.Clear();
    }

    /// <summary>
    /// 创建简单的立方体网格
    /// </summary>
    private Mesh CreateBoxMesh()
    {
        Mesh mesh = new Mesh();

        // 顶点
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };

        // 三角形
        int[] triangles = new int[36]
        {
            // 前
            0, 1, 2, 0, 2, 3,
            // 右
            1, 5, 6, 1, 6, 2,
            // 后
            5, 4, 7, 5, 7, 6,
            // 左
            4, 0, 3, 4, 3, 7,
            // 上
            3, 2, 6, 3, 6, 7,
            // 下
            4, 5, 1, 4, 1, 0
        };

        // UV
        Vector2[] uv = new Vector2[8]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        // 法线
        Vector3[] normals = new Vector3[8]
        {
            Vector3.Normalize(new Vector3(-1, -1, -1)),
            Vector3.Normalize(new Vector3(1, -1, -1)),
            Vector3.Normalize(new Vector3(1, 1, -1)),
            Vector3.Normalize(new Vector3(-1, 1, -1)),
            Vector3.Normalize(new Vector3(-1, -1, 1)),
            Vector3.Normalize(new Vector3(1, -1, 1)),
            Vector3.Normalize(new Vector3(1, 1, 1)),
            Vector3.Normalize(new Vector3(-1, 1, 1))
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;

        return mesh;
    }

    private void Update()
    {
        if (useGameObjects)
        {
            // 使用GameObject渲染不需要每帧更新
        }
        else
        {
            RenderVoxels();
        }
    }

    /// <summary>
    /// 渲染所有体素
    /// </summary>
    private void RenderVoxels()
    {
        if (_voxels.Count == 0 || _boxMesh == null || voxelMaterial == null)
            return;

        if (useInstancing)
        {
            RenderWithInstancing();
        }
        else
        {
            RenderWithoutInstancing();
        }
    }

    /// <summary>
    /// 使用GPU实例化渲染体素
    /// </summary>
    private void RenderWithInstancing()
    {
        // 检查材质和网格
        if (voxelMaterial == null || _boxMesh == null)
        {
            Debug.LogError("无法进行实例化渲染：材质或网格为空", this);
            return;
        }

        // 确保材质启用了实例化
        if (!voxelMaterial.enableInstancing)
        {
            try
            {
                voxelMaterial.enableInstancing = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("无法启用材质的实例化，切换到非实例化渲染: " + e.Message, this);
                useInstancing = false;
                RenderWithoutInstancing();
                return;
            }
        }

        _matrices.Clear();
        _colors.Clear();

        // 准备实例数据
        foreach (Voxel voxel in _voxels)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(voxel.Position, Quaternion.identity, voxel.Size);
            _matrices.Add(matrix);
            _colors.Add(voxel.Color);
        }

        try
        {
            // 分批渲染（GPU实例化有每批次最大数量限制）
            for (int i = 0; i < _matrices.Count; i += MaxInstancesPerBatch)
            {
                int batchCount = Mathf.Min(MaxInstancesPerBatch, _matrices.Count - i);

                // 设置当前批次的颜色
                Vector4[] batchColors = new Vector4[batchCount];
                for (int j = 0; j < batchCount; j++)
                {
                    batchColors[j] = _colors[i + j];
                }

                _propertyBlock.SetVectorArray("_BaseColor", batchColors); // URP使用_BaseColor而非_Color
                _propertyBlock.SetVectorArray("_EmissionColor", batchColors);

                // 计算当前批次的矩阵
                Matrix4x4[] batchMatrices = new Matrix4x4[batchCount];
                for (int j = 0; j < batchCount; j++)
                {
                    batchMatrices[j] = _matrices[i + j];
                }

                // 绘制实例
                Graphics.DrawMeshInstanced(_boxMesh, 0, voxelMaterial, batchMatrices, batchCount, _propertyBlock);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("实例化渲染失败，切换到非实例化渲染: " + e.Message, this);
            useInstancing = false;
            RenderWithoutInstancing();
        }
    }

    /// <summary>
    /// 不使用GPU实例化渲染体素
    /// </summary>
    private void RenderWithoutInstancing()
    {
        // Default shader
        //foreach (Voxel voxel in _voxels)
        //{
        //    _propertyBlock.SetColor("_Color", voxel.Color);
        //    _propertyBlock.SetColor("_EmissionColor", voxel.Color);

        //    Matrix4x4 matrix = Matrix4x4.TRS(voxel.Position, Quaternion.identity, voxel.Size);
        //    Graphics.DrawMesh(_boxMesh, matrix, voxelMaterial, 0, null, 0, _propertyBlock);
        //}

        // URP
        foreach (Voxel voxel in _voxels)
        {
            _propertyBlock.Clear();
            // URP使用_BaseColor而非_Color
            _propertyBlock.SetColor("_BaseColor", voxel.Color);
            _propertyBlock.SetColor("_EmissionColor", voxel.Color);

            Matrix4x4 matrix = Matrix4x4.TRS(voxel.Position, Quaternion.identity, voxel.Size);
            Graphics.DrawMesh(_boxMesh, matrix, voxelMaterial, 0, null, 0, _propertyBlock);
        }
    }

    /// <summary>
    /// 设置要渲染的体素
    /// </summary>
    public void SetVoxels(List<Voxel> voxels)
    {
        _voxels.Clear();
        _voxels.AddRange(voxels);

        // 如果使用GameObject渲染，立即更新
        if (useGameObjects)
        {
            RenderWithGameObjects();
        }
    }

    /// <summary>
    /// 从八叉树节点设置体素
    /// </summary>
    public void SetVoxelsFromOctree(Octree octree)
    {
        _voxels.Clear();

        List<OctreeNode> voxelNodes = octree.GetVoxelNodes();
        foreach (OctreeNode node in voxelNodes)
        {
            _voxels.Add(Voxel.FromOctreeNode(node));
        }
    }

    /// <summary>
    /// 清除所有体素
    /// </summary>
    public void ClearVoxels()
    {
        _voxels.Clear();

        // 清除GameObject体素
        ClearVoxelGameObjects();
    }

    // 在对象销毁时清理资源
    private void OnDestroy()
    {
        ClearVoxelGameObjects();

        if (_voxelContainer != null)
        {
            DestroyImmediate(_voxelContainer.gameObject);
            _voxelContainer = null;
        }
    }
}