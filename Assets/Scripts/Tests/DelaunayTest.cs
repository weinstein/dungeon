using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DelaunayTest
{
    [Test]
    public void TriangleTriangulation()
    {
        Vector2 a = new(1, 1);
        Vector2 b = new(-1, 1);
        Vector2 c = new(0, -1);

        Graph<Vector2, float> triangulation = Delaunay.Triangulate(a, b, c);
        Assert.True(triangulation.ContainsEdge(a, b));
        Assert.True(triangulation.ContainsEdge(b, c));
        Assert.True(triangulation.ContainsEdge(a, c));
    }
}
