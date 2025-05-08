using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 八叉树中的单个节点
/// </summary>
public class OctreeNode
{
    // 节点的边界
    public Bounds Bounds { get; private set; }

    // 节点深度
    public int Depth { get; private set; }

    // 父节点
    public OctreeNode Parent { get; private set; }

    // 子节点（如果已分割）
    public OctreeNode[] Children { get; private set; }

    // 当前节点是否包含体素
    public bool ContainsVoxel { get; set; }

    // 体素颜色
    public Color VoxelColor { get; set; }

    // 最大深度
    private readonly int _maxDepth;

    // 最小大小
    private readonly float _minSize;

    /// <summary>
    /// 创建一个新的八叉树节点
    /// </summary>
    public OctreeNode(Bounds bounds, int depth, OctreeNode parent, int maxDepth, float minSize)
    {
        Bounds = bounds;
        Depth = depth;
        Parent = parent;
        _maxDepth = maxDepth;
        _minSize = minSize;
        ContainsVoxel = false;
        Children = null;
    }

    /// <summary>
    /// 检查点是否在节点边界内
    /// </summary>
    public bool ContainsPoint(Vector3 point)
    {
        return Bounds.Contains(point);
    }

    /// <summary>
    /// 节点是否为叶子节点
    /// </summary>
    public bool IsLeaf()
    {
        return Children == null;
    }

    /// <summary>
    /// 将节点分割为8个子节点
    /// </summary>
    public void Split()
    {
        if (!IsLeaf() || Depth >= _maxDepth || Bounds.size.x <= _minSize)
            return;

        Children = new OctreeNode[8];
        Vector3 center = Bounds.center;
        Vector3 extents = Bounds.extents * 0.5f;

        // 创建8个子节点，基于它们在3D空间中的八个象限
        for (int i = 0; i < 8; i++)
        {
            Vector3 childCenter = center + new Vector3(
                ((i & 1) == 0) ? -extents.x : extents.x,
                ((i & 2) == 0) ? -extents.y : extents.y,
                ((i & 4) == 0) ? -extents.z : extents.z
            );

            Bounds childBounds = new Bounds(childCenter, Bounds.size * 0.5f);
            Children[i] = new OctreeNode(childBounds, Depth + 1, this, _maxDepth, _minSize);
        }
    }

    /// <summary>
    /// 获取所有叶子节点
    /// </summary>
    public List<OctreeNode> GetLeafNodes()
    {
        List<OctreeNode> leafNodes = new List<OctreeNode>();
        GetLeafNodesRecursive(leafNodes);
        return leafNodes;
    }

    private void GetLeafNodesRecursive(List<OctreeNode> leafNodes)
    {
        if (IsLeaf())
        {
            leafNodes.Add(this);
            return;
        }

        foreach (OctreeNode child in Children)
        {
            child.GetLeafNodesRecursive(leafNodes);
        }
    }

    /// <summary>
    /// 获取包含体素的叶子节点
    /// </summary>
    public List<OctreeNode> GetVoxelNodes()
    {
        List<OctreeNode> voxelNodes = new List<OctreeNode>();
        GetVoxelNodesRecursive(voxelNodes);
        return voxelNodes;
    }

    private void GetVoxelNodesRecursive(List<OctreeNode> voxelNodes)
    {
        if (IsLeaf() && ContainsVoxel)
        {
            voxelNodes.Add(this);
            return;
        }

        if (!IsLeaf())
        {
            foreach (OctreeNode child in Children)
            {
                child.GetVoxelNodesRecursive(voxelNodes);
            }
        }
    }
}