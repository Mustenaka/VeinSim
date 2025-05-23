// VoxelGrid.cs - ���ػ�����
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

namespace FEMSoftBody
{
    /// <summary>
    /// ���ؽڵ�����
    /// </summary>
    public struct VoxelNode
    {
        public float3 position;
        public bool isSurface;
        public bool isInterior;

        public VoxelNode(float3 pos, bool surface = false)
        {
            position = pos;
            isSurface = surface;
            isInterior = !surface;
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    public class VoxelGrid
    {
        private bool[,,] voxels;
        private Bounds bounds;
        private int resolution;
        private float voxelSize;

        public VoxelGrid(Bounds bounds, int resolution)
        {
            this.bounds = bounds;
            this.resolution = resolution;
            this.voxelSize = math.max(bounds.size.x, math.max(bounds.size.y, bounds.size.z)) / resolution;

            voxels = new bool[resolution, resolution, resolution];
        }

        /// <summary>
        /// ���ػ�������
        /// </summary>
        public void VoxelizeTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            // ���������ΰ�Χ��
            Vector3 min = Vector3.Min(Vector3.Min(v0, v1), v2);
            Vector3 max = Vector3.Max(Vector3.Max(v0, v1), v2);

            // ת��Ϊ��������
            int3 voxelMin = WorldToVoxel(min);
            int3 voxelMax = WorldToVoxel(max);

            // ������Χ���ڵ�����
            for (int x = voxelMin.x; x <= voxelMax.x; x++)
            {
                for (int y = voxelMin.y; y <= voxelMax.y; y++)
                {
                    for (int z = voxelMin.z; z <= voxelMax.z; z++)
                    {
                        if (IsValidVoxelIndex(x, y, z))
                        {
                            Vector3 voxelCenter = VoxelToWorld(new int3(x, y, z));

                            // ������������Ƿ����������ڻ򸽽�
                            if (IsPointNearTriangle(voxelCenter, v0, v1, v2, voxelSize * 0.5f))
                            {
                                voxels[x, y, z] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ��ˮ����㷨����ڲ�
        /// </summary>
        public void FloodFillInterior()
        {
            bool[,,] interior = new bool[resolution, resolution, resolution];
            bool[,,] visited = new bool[resolution, resolution, resolution];

            // �ӱ߽翪ʼ��ˮ����ⲿ����
            Queue<int3> queue = new Queue<int3>();

            // ������б߽�����
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        if (x == 0 || x == resolution - 1 ||
                            y == 0 || y == resolution - 1 ||
                            z == 0 || z == resolution - 1)
                        {
                            if (!voxels[x, y, z])
                            {
                                queue.Enqueue(new int3(x, y, z));
                                visited[x, y, z] = true;
                            }
                        }
                    }
                }
            }

            // ��ˮ���
            int3[] neighbors = {
                new int3(1, 0, 0), new int3(-1, 0, 0),
                new int3(0, 1, 0), new int3(0, -1, 0),
                new int3(0, 0, 1), new int3(0, 0, -1)
            };

            while (queue.Count > 0)
            {
                int3 current = queue.Dequeue();

                foreach (var neighbor in neighbors)
                {
                    int3 next = current + neighbor;

                    if (IsValidVoxelIndex(next.x, next.y, next.z) &&
                        !visited[next.x, next.y, next.z] &&
                        !voxels[next.x, next.y, next.z])
                    {
                        visited[next.x, next.y, next.z] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            // δ�������Ҳ��Ǳ�������ؾ����ڲ�
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        if (!visited[x, y, z] && !voxels[x, y, z])
                        {
                            voxels[x, y, z] = true; // ���Ϊ�ڲ�
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ����FEM�ڵ�
        /// </summary>
        public List<VoxelNode> GenerateNodes(float interiorDensity)
        {
            List<VoxelNode> nodes = new List<VoxelNode>();

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        if (voxels[x, y, z])
                        {
                            Vector3 worldPos = VoxelToWorld(new int3(x, y, z));
                            bool isSurface = IsSurfaceVoxel(x, y, z);

                            // ����ڵ��������
                            if (isSurface)
                            {
                                nodes.Add(new VoxelNode(worldPos, true));
                            }
                            // �ڲ��ڵ�����ܶȿ���
                            else if (UnityEngine.Random.value < interiorDensity)
                            {
                                nodes.Add(new VoxelNode(worldPos, false));
                            }
                        }
                    }
                }
            }

            return nodes;
        }

        #region ��������

        private int3 WorldToVoxel(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - bounds.min;
            int3 voxelPos = new int3(
                Mathf.FloorToInt(localPos.x / voxelSize),
                Mathf.FloorToInt(localPos.y / voxelSize),
                Mathf.FloorToInt(localPos.z / voxelSize)
            );

            return math.clamp(voxelPos, int3.zero, new int3(resolution - 1));
        }

        private Vector3 VoxelToWorld(int3 voxelPos)
        {
            return bounds.min + new Vector3(
                (voxelPos.x + 0.5f) * voxelSize,
                (voxelPos.y + 0.5f) * voxelSize,
                (voxelPos.z + 0.5f) * voxelSize
            );
        }

        private bool IsValidVoxelIndex(int x, int y, int z)
        {
            return x >= 0 && x < resolution &&
                   y >= 0 && y < resolution &&
                   z >= 0 && z < resolution;
        }

        private bool IsSurfaceVoxel(int x, int y, int z)
        {
            if (!voxels[x, y, z]) return false;

            // ���6���ھ�
            int[] dx = { -1, 1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, -1, 1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, -1, 1 };

            for (int i = 0; i < 6; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                int nz = z + dz[i];

                if (!IsValidVoxelIndex(nx, ny, nz) || !voxels[nx, ny, nz])
                {
                    return true; // ���ھ��ǿյģ�˵���Ǳ���
                }
            }

            return false;
        }

        private bool IsPointNearTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2, float threshold)
        {
            // ����㵽�����εľ���
            Vector3 closest = ClosestPointOnTriangle(point, v0, v1, v2);
            float distance = Vector3.Distance(point, closest);

            return distance <= threshold;
        }

        private Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 edge0 = v1 - v0;
            Vector3 edge1 = v2 - v0;
            Vector3 v0ToPoint = point - v0;

            float a = Vector3.Dot(edge0, edge0);
            float b = Vector3.Dot(edge0, edge1);
            float c = Vector3.Dot(edge1, edge1);
            float d = Vector3.Dot(edge0, v0ToPoint);
            float e = Vector3.Dot(edge1, v0ToPoint);

            float det = a * c - b * b;
            float s = b * e - c * d;
            float t = b * d - a * e;

            if (s + t < det)
            {
                if (s < 0.0f)
                {
                    if (t < 0.0f)
                    {
                        s = Mathf.Clamp01(-d / a);
                        t = 0.0f;
                    }
                    else
                    {
                        s = 0.0f;
                        t = Mathf.Clamp01(-e / c);
                    }
                }
                else if (t < 0.0f)
                {
                    s = Mathf.Clamp01(-d / a);
                    t = 0.0f;
                }
                else
                {
                    float invDet = 1.0f / det;
                    s *= invDet;
                    t *= invDet;
                }
            }
            else
            {
                if (s < 0.0f)
                {
                    float tmp0 = b + d;
                    float tmp1 = c + e;
                    if (tmp1 > tmp0)
                    {
                        float numer = tmp1 - tmp0;
                        float denom = a - 2 * b + c;
                        s = Mathf.Clamp01(numer / denom);
                        t = 1 - s;
                    }
                    else
                    {
                        t = Mathf.Clamp01(-e / c);
                        s = 0.0f;
                    }
                }
                else if (t < 0.0f)
                {
                    s = Mathf.Clamp01(-d / a);
                    t = 0.0f;
                }
                else
                {
                    float numer = c + e - b - d;
                    if (numer <= 0.0f)
                    {
                        s = 0.0f;
                    }
                    else
                    {
                        float denom = a - 2 * b + c;
                        s = Mathf.Clamp01(numer / denom);
                    }
                    t = 1.0f - s;
                }
            }

            return v0 + s * edge0 + t * edge1;
        }

        #endregion
    }
}
