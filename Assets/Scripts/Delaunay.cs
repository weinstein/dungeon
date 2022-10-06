using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Delaunay
{
    public class Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 c, float r)
        {
            this.center = c;
            this.radius = r;
        }

        static Vector2 Intersect(Vector2 a, Vector2 dirA, Vector2 b, Vector2 dirB)
        {
            Vector2 dirBNormal = Vector2.Perpendicular(dirB);
            float rA = Vector2.Dot(b - a, dirBNormal) / Vector2.Dot(dirA, dirBNormal);
            return a + rA * dirA;
        }

        public static Circle FromPoints(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 midAB = (a + b) / 2;
            Vector2 abNormal = Vector2.Perpendicular(b - a);
            Vector2 midBC = (b + c) / 2;
            Vector2 bcNormal = Vector2.Perpendicular(c - b);
            Vector2 center = Intersect(midAB, abNormal, midBC, bcNormal);
            float radius = (a - center).magnitude;
            return new Circle(center, radius);
        }

        public bool Contains(Vector2 pt)
        {
            return (pt - center).sqrMagnitude <= radius * radius;
        }
        public bool Contains(float x, float y)
        {
            float dx = x - center.x;
            float dy = y - center.y;
            return dx * dx + dy * dy <= radius * radius;
        }

        public Vector2 PointOnCircumference(float angle)
        {
            Vector2 dir;
            dir.x = Mathf.Cos(angle);
            dir.y = Mathf.Sin(angle);
            return center + radius * dir;
        }

        public Triangle CircumscribedTriangle()
        {
            Vector2 a = PointOnCircumference(0);
            Vector2 b = PointOnCircumference(2 * Mathf.PI / 3);
            Vector2 c = PointOnCircumference(2 * Mathf.PI * 2 / 3);
            Vector2 triAB = Intersect(a, Vector2.Perpendicular(a - center), b, Vector2.Perpendicular(b - center));
            Vector2 triBC = Intersect(b, Vector2.Perpendicular(b - center), c, Vector2.Perpendicular(c - center));
            Vector2 triCA = Intersect(c, Vector2.Perpendicular(c - center), a, Vector2.Perpendicular(a - center));
            return new Triangle(triAB, triBC, triCA);
        }
    }

    public class Triangle
    {
        public Vector2 a, b, c;
        public Circle circ;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.circ = Circle.FromPoints(a, b, c);
        }

        public bool Contains(Vector2 pt)
        {
            Vector2 ab = b - a;
            Vector2 abNormal = Vector2.Perpendicular(ab);
            Vector2 ac = c - a;
            Vector2 acNormal = Vector2.Perpendicular(ac);
            float rAB = Vector2.Dot(pt - a, acNormal) / Vector2.Dot(ab, acNormal);
            float rAC = Vector2.Dot(pt - a, abNormal) / Vector2.Dot(ac, abNormal);
            return rAB >= 0 && rAC >= 0 && rAB + rAC <= 1;
        }

        public readonly int Length = 3;

        public Vector2 this[int idx]
        {
            get { switch (idx)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    default: return Vector2.zero;
                } }
            set {
                switch (idx)
                {
                    case 0: a = value; break;
                    case 1: b = value; break;
                    case 2: c = value; break;
                    default: break;
                }
                circ = Circle.FromPoints(a, b, c);
            }
        }

        public bool TrueForAll(Predicate<Vector2> cond)
        {
            return cond(a) && cond(b) && cond(c);
        }

        public bool ContainsVertex(Vector2 p)
        {
            return a == p || b == p || c == p;
        }

        public bool ContainsEdge(Vector2 x, Vector2 y)
        {
            return ContainsVertex(x) && ContainsVertex(y);
        }

        public Vector2 Find(Predicate<Vector2> cond)
        {
            if (cond(a)) return a;
            else if (cond(b)) return b;
            else return c;
        }
    }

    struct DelaunayEdge
    {
        public Triangle a, b;
        public Vector2 src, dst;
    }

    struct EdgeData
    {
        public Vector2 src, dst;
    }

    private List<Vector2> placeholderPoints = new();
    private Queue<DelaunayEdge> edgesToCheck = new();
    private Graph<Triangle, EdgeData> triGraph = new();

    public Delaunay(IEnumerable<Vector2> pts)
    {
        Vector2 center = Vector2.zero;
        int count = 0;
        foreach (Vector2 p in pts)
        {
            center += p;
            ++count;
        }
        center /= count;
        float radius = 0;
        foreach (Vector2 p in pts)
        {
            radius = Mathf.Max(radius, (p - center).magnitude);
        }
        radius *= 2;
        Triangle containing = new Circle(center, radius).CircumscribedTriangle();
        triGraph.Add(containing);
        for (int i = 0; i < 3; ++i) placeholderPoints.Add(containing[i]);
        foreach (Vector2 p in pts) AddPoint(p);
        while (edgesToCheck.Count > 0) FixEdge(edgesToCheck.Dequeue());
        List<Triangle> placeholderTriangles = new();
        triGraph.TraverseDepthFirst(t =>
        {
            foreach (Vector2 p in placeholderPoints)
            {
                if (t.ContainsVertex(p)) placeholderTriangles.Add(t);
            }
        }, (_, _, _) => { });
        foreach (Triangle t in placeholderTriangles)
        {
            triGraph.Remove(t);
        }
    }

    void FixEdge(DelaunayEdge e)
    {
        if (!triGraph.ContainsEdge(e.a, e.b)) return;
        Vector2 exclA = e.a.Find(p => p != e.src && p != e.dst);
        Vector2 exclB = e.b.Find(p => p != e.src && p != e.dst);
        bool isLegal = !e.a.circ.Contains(exclB);
        if (isLegal) return;
        // Flip it            
        DelaunayEdge newEdge = new();
        newEdge.a = new Triangle(exclA, e.src, exclB);
        newEdge.b = new Triangle(exclA, e.dst, exclB);
        newEdge.src = exclA;
        newEdge.dst = exclB;
        EdgeData newEdgeData = new();
        newEdgeData.src = newEdge.src;
        newEdgeData.dst = newEdge.dst;
        triGraph.AddUndirected(newEdge.a, newEdge.b, newEdgeData);
        // Newly added edge is guaranteed legal and doesn't need to be checked again.
        foreach (KeyValuePair<Triangle, EdgeData> kv in triGraph.FindEdges(e.a))
        {
            Triangle other = kv.Key;
            if (other == e.b) continue;
            if (newEdge.a.ContainsEdge(kv.Value.src, kv.Value.dst))
            {
                triGraph.AddUndirected(newEdge.a, other, kv.Value);
                DelaunayEdge toCheck = new();
                toCheck.a = newEdge.a;
                toCheck.b = other;
                toCheck.src = kv.Value.src;
                toCheck.dst = kv.Value.dst;
                edgesToCheck.Enqueue(toCheck);
            }
            if (newEdge.b.ContainsEdge(kv.Value.src, kv.Value.dst))
            {
                triGraph.AddUndirected(newEdge.b, other, kv.Value);
                DelaunayEdge toCheck = new();
                toCheck.a = newEdge.b;
                toCheck.b = other;
                toCheck.src = kv.Value.src;
                toCheck.dst = kv.Value.dst;
                edgesToCheck.Enqueue(toCheck);
            }
        }
        foreach (KeyValuePair<Triangle, EdgeData> kv in triGraph.FindEdges(e.b))
        {
            Triangle other = kv.Key;
            if (other == e.a) continue;
            if (newEdge.a.ContainsEdge(kv.Value.src, kv.Value.dst))
            {
                triGraph.AddUndirected(newEdge.a, other, kv.Value);
                DelaunayEdge toCheck = new();
                toCheck.a = newEdge.a;
                toCheck.b = other;
                toCheck.src = kv.Value.src;
                toCheck.dst = kv.Value.dst;
                edgesToCheck.Enqueue(toCheck);
            }
            if (newEdge.b.ContainsEdge(kv.Value.src, kv.Value.dst))
            {
                triGraph.AddUndirected(newEdge.b, other, kv.Value);
                DelaunayEdge toCheck = new();
                toCheck.a = newEdge.b;
                toCheck.b = other;
                toCheck.src = kv.Value.src;
                toCheck.dst = kv.Value.dst;
                edgesToCheck.Enqueue(toCheck);
            }
        }
        triGraph.Remove(e.a);
        triGraph.Remove(e.b);
    }

    public void AddPoint(Vector2 p)
    {
        Triangle parent = null;
        triGraph.TraverseDepthFirst(t =>
        {
            if (t.Contains(p)) parent = t;
        }, (_, _, _) => { });
        Triangle abp, acp, bcp;
        abp = new Triangle(parent.a, parent.b, p);
        acp = new Triangle(parent.a, parent.c, p);
        bcp = new Triangle(parent.b, parent.c, p);
        foreach (KeyValuePair<Triangle, EdgeData> kv in triGraph.FindEdges(parent))
        {
            if (abp.ContainsEdge(kv.Value.src, kv.Value.dst)) triGraph.AddUndirected(kv.Key, abp, kv.Value);
            if (acp.ContainsEdge(kv.Value.src, kv.Value.dst)) triGraph.AddUndirected(kv.Key, acp, kv.Value);
            if (bcp.ContainsEdge(kv.Value.src, kv.Value.dst)) triGraph.AddUndirected(kv.Key, bcp, kv.Value);
        }
        EdgeData newEdge = new();
        newEdge.src = p;
        newEdge.dst = parent.a;
        triGraph.AddUndirected(abp, acp, newEdge);
        DelaunayEdge toCheck = new();
        toCheck.a = abp;
        toCheck.b = acp;
        toCheck.src = newEdge.src;
        toCheck.dst = newEdge.dst;
        edgesToCheck.Enqueue(toCheck);

        newEdge.dst = parent.b;
        triGraph.AddUndirected(abp, bcp, newEdge);
        toCheck.a = abp;
        toCheck.b = bcp;
        toCheck.dst = newEdge.dst;

        newEdge.dst = parent.c;
        triGraph.AddUndirected(bcp, acp, newEdge);
        toCheck.a = bcp;
        toCheck.b = acp;
        toCheck.dst = newEdge.dst;
        edgesToCheck.Enqueue(toCheck);
        
        triGraph.Remove(parent);
    }

    public static Graph<Vector2, float> Triangulate(IEnumerable<Vector2> points)
    {
        Graph<Vector2, float> ret = new();
        Delaunay d = new(points);
        d.triGraph.ForEachNode(n =>
        {
            for (int i = 0; i < 3; ++i)
            {
                Vector2 a = n[i];
                Vector2 b = n[(i + 1) % 3];
                float d = (b - a).magnitude;
                ret.AddUndirected(a, b, d);
            }
        });
        return ret;
    }
}
