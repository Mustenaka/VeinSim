using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 八叉树数据结构实现
/// </summary>
public class Octree
{
    // 根节点
    public OctreeNode Root { get; private set; }

    // 最大深度
    public int MaxDepth { get; private set; }

    // 最小节点大小
    public float MinSize { get; private set; }

    // 包含体素的节点
    private List<OctreeNode> _voxelNodes;

    /// <summary>
    /// 创建一个新的八叉树
    /// </summary>
    public Octree(Bounds bounds, int maxDepth, float minSize)
    {
        MaxDepth = maxDepth;
        MinSize = minSize;
        Root = new OctreeNode(bounds, 0, null, MaxDepth, MinSize);
        _voxelNodes = new List<OctreeNode>();
    }

    /// <summary>
    /// 根据网格体素化
    /// </summary>
    public void VoxelizeMesh(Mesh mesh, Transform transform, Color voxelColor, int resolution)
    {
        // 清除之前的体素
        ClearVoxels();

        // 获取网格信息
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // 遍历所有三角形
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 获取三角形的三个顶点
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v3 = transform.TransformPoint(vertices[triangles[i + 2]]);

            // 体素化三角形
            VoxelizeTriangle(v1, v2, v3, resolution, voxelColor);
        }
    }

    /// <summary>
    /// 体素化三角形
    /// </summary>
    private void VoxelizeTriangle(Vector3 v1, Vector3 v2, Vector3 v3, int resolution, Color voxelColor)
    {
        // 计算三角形的包围盒
        Bounds triangleBounds = new Bounds(v1, Vector3.zero);
        triangleBounds.Encapsulate(v2);
        triangleBounds.Encapsulate(v3);

        // 计算体素大小
        float voxelSize = Mathf.Min(Root.Bounds.size.x, Root.Bounds.size.y, Root.Bounds.size.z) / resolution;

        // 计算体素采样点
        for (float x = triangleBounds.min.x; x <= triangleBounds.max.x; x += voxelSize)
        {
            for (float y = triangleBounds.min.y; y <= triangleBounds.max.y; y += voxelSize)
            {
                for (float z = triangleBounds.min.z; z <= triangleBounds.max.z; z += voxelSize)
                {
                    Vector3 point = new Vector3(x, y, z);

                    // 检查点是否在三角形内或足够接近三角形
                    if (IsPointNearTriangle(point, v1, v2, v3, voxelSize * 0.5f))
                    {
                        InsertVoxel(point, voxelColor);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查点是否在三角形内或足够接近三角形
    /// </summary>
    private bool IsPointNearTriangle(Vector3 point, Vector3 v1, Vector3 v2, Vector3 v3, float maxDistance)
    {
        // 计算点到三角形的最近距离
        // 首先计算点到三角形平面的距离
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
        float distanceToPlane = Mathf.Abs(Vector3.Dot(point - v1, normal));

        // 如果距离平面太远，则不在三角形附近
        if (distanceToPlane > maxDistance)
            return false;

        // 投影点到三角形平面
        Vector3 projectedPoint = point - normal * distanceToPlane;

        // 检查投影点是否在三角形内
        // 使用重心坐标计算
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;
        Vector3 edge3 = projectedPoint - v1;

        float dot11 = Vector3.Dot(edge1, edge1);
        float dot12 = Vector3.Dot(edge1, edge2);
        float dot13 = Vector3.Dot(edge1, edge3);
        float dot22 = Vector3.Dot(edge2, edge2);
        float dot23 = Vector3.Dot(edge2, edge3);

        float invDenom = 1.0f / (dot11 * dot22 - dot12 * dot12);
        float u = (dot22 * dot13 - dot12 * dot23) * invDenom;
        float v = (dot11 * dot23 - dot12 * dot13) * invDenom;

        // 检查重心坐标是否在有效范围内
        if (u >= 0 && v >= 0 && u + v <= 1)
        {
            return true;
        }

        // 如果投影不在三角形内，检查点是否足够接近三角形的边或顶点
        float distanceToEdge1 = PointToLineDistance(point, v1, v2);
        float distanceToEdge2 = PointToLineDistance(point, v2, v3);
        float distanceToEdge3 = PointToLineDistance(point, v3, v1);

        return Mathf.Min(distanceToEdge1, distanceToEdge2, distanceToEdge3) <= maxDistance;
    }

    /// <summary>
    /// 计算点到线段的距离
    /// </summary>
    private float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float lineLengthSqr = line.sqrMagnitude;

        if (lineLengthSqr == 0)
            return Vector3.Distance(point, lineStart);

        // 计算投影比例
        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, line) / lineLengthSqr);

        // 计算投影点
        Vector3 projection = lineStart + t * line;

        // 返回点到投影点的距离
        return Vector3.Distance(point, projection);
    }

    /// <summary>
    /// 在指定点插入体素
    /// </summary>
    public void InsertVoxel(Vector3 point, Color color)
    {
        if (!Root.Bounds.Contains(point))
            return;

        // 从根节点开始查找、分割并插入
        InsertVoxelRecursive(Root, point, color);
    }

    private void InsertVoxelRecursive(OctreeNode node, Vector3 point, Color color)
    {
        // 如果点不在节点范围内，返回
        if (!node.ContainsPoint(point))
            return;

        // 如果达到最大深度或最小大小，设置节点包含体素
        if (node.Depth >= MaxDepth || node.Bounds.size.x <= MinSize)
        {
            if (!node.ContainsVoxel)
            {
                node.ContainsVoxel = true;
                node.VoxelColor = color;
                _voxelNodes.Add(node);
            }
            return;
        }

        // 如果节点是叶子节点，分割它
        if (node.IsLeaf())
        {
            node.Split();
        }

        // 在适当的子节点中递归插入
        foreach (OctreeNode child in node.Children)
        {
            if (child.ContainsPoint(point))
            {
                InsertVoxelRecursive(child, point, color);
                break;
            }
        }
    }

    /// <summary>
    /// 清除所有体素
    /// </summary>
    public void ClearVoxels()
    {
        ClearVoxelsRecursive(Root);
        _voxelNodes.Clear();
    }

    private void ClearVoxelsRecursive(OctreeNode node)
    {
        node.ContainsVoxel = false;

        if (!node.IsLeaf())
        {
            foreach (OctreeNode child in node.Children)
            {
                ClearVoxelsRecursive(child);
            }
        }
    }

    /// <summary>
    /// 获取所有包含体素的节点
    /// </summary>
    public List<OctreeNode> GetVoxelNodes()
    {
        return _voxelNodes;
    }
}