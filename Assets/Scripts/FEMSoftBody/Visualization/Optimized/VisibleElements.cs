using System.Collections.Generic;

public class VisibleElements
{
    public List<int> nodes = new List<int>();
    public List<int> tetrahedra = new List<int>();
    public List<int> surfaceTriangles = new List<int>();

    public void Clear()
    {
        nodes.Clear();
        tetrahedra.Clear();
        surfaceTriangles.Clear();
    }
}