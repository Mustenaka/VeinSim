using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace FEMSoftBody
{
    /// <summary>
    /// 从四面体网格中提取表面三角形
    /// </summary>
    public class SurfaceExtractor
    {
        /// <summary>
        /// 从四面体数组中提取表面
        /// </summary>
        public List<int3> ExtractSurface(FEMTetrahedronData[] tetrahedra)
        {
            var faceCount = new Dictionary<string, int>();
            var faceToTriangle = new Dictionary<string, int3>();

            // 遍历所有四面体的面
            foreach (var tet in tetrahedra)
            {
                var faces = new int3[]
                {
                    new int3(tet.nodeIndices.x, tet.nodeIndices.y, tet.nodeIndices.z),
                    new int3(tet.nodeIndices.x, tet.nodeIndices.y, tet.nodeIndices.w),
                    new int3(tet.nodeIndices.x, tet.nodeIndices.z, tet.nodeIndices.w),
                    new int3(tet.nodeIndices.y, tet.nodeIndices.z, tet.nodeIndices.w)
                };

                foreach (var face in faces)
                {
                    // 标准化面的顶点顺序
                    var sortedFace = SortTriangle(face);
                    string key = $"{sortedFace.x}_{sortedFace.y}_{sortedFace.z}";

                    if (faceCount.ContainsKey(key))
                    {
                        faceCount[key]++;
                    }
                    else
                    {
                        faceCount[key] = 1;
                        faceToTriangle[key] = face;
                    }
                }
            }

            // 只有被一个四面体共享的面才是表面
            var surfaceFaces = new List<int3>();
            foreach (var kvp in faceCount)
            {
                if (kvp.Value == 1)
                {
                    surfaceFaces.Add(faceToTriangle[kvp.Key]);
                }
            }

            return surfaceFaces;
        }

        private int3 SortTriangle(int3 triangle)
        {
            int[] sorted = { triangle.x, triangle.y, triangle.z };
            System.Array.Sort(sorted);
            return new int3(sorted[0], sorted[1], sorted[2]);
        }
    }
}