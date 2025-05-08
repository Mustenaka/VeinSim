using UnityEngine;

/// <summary>
/// 表示单个体素
/// </summary>
public class Voxel
{
    // 体素位置
    public Vector3 Position { get; private set; }

    // 体素大小
    public Vector3 Size { get; private set; }

    // 体素颜色
    public Color Color { get; set; }

    /// <summary>
    /// 创建一个新的体素
    /// </summary>
    public Voxel(Vector3 position, Vector3 size, Color color)
    {
        Position = position;
        Size = size;
        Color = color;
    }

    /// <summary>
    /// 从八叉树节点创建体素
    /// </summary>
    public static Voxel FromOctreeNode(OctreeNode node)
    {
        return new Voxel(node.Bounds.center, node.Bounds.size, node.VoxelColor);
    }
}