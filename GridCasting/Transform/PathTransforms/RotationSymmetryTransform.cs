using GridCasting.Models.GridGraph;
using Path = GridCasting.Models.Path;
#if NET8_0_OR_GREATER
using Mathf = System.MathF;
#else
using Mathf = UnityEngine.Mathf;
#endif

namespace GridCasting.Transform.PathTransforms;

public class RotationSymmetryTransform(int sectors = 16) : IPathTransform
{
    public void Initialize(GridGraph graph)
    {
        // Sort edges by angle for rotation consistency
        foreach (var node in graph.Nodes) 
            node.Edges.Sort((a, b) => a.Angle.CompareTo(b.Angle));
    }

    public Path Transform(GridGraph graph, Path path)
    {
        var node = graph[path.StartNode];
        var directions = new int[path.Directions.Length];
        var angle = 0f;
        for (var i = 0; i < directions.Length; i++)
        {
            var direction = path.Directions[i];
            var edge = node.Edges[direction];
            float newAngle;
            if (node == edge.NodeA)
            {
                node = edge.NodeB;
                newAngle = edge.Angle;
            }
            else
            {
                node = edge.NodeA;
                newAngle = edge.Angle + Mathf.PI;
            }
            if (i == 0) angle = newAngle;
            var delta = newAngle - angle;
            while (delta > Mathf.PI) delta -= Mathf.PI * 2;
            while (delta < -Mathf.PI) delta += Mathf.PI * 2;
            directions[i] = (int)Mathf.Round((delta * sectors - 0.5f) / Mathf.PI);
            angle = newAngle;
        }

        return new Path(path.StartNode, directions);
    }

    public bool IsRequired => true;
}