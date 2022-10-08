using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GraphTest
{
    [Test]
    public void ForEach()
    {
        Graph<string, string> g = new();
        g.AddDirected("a", "b", "ab");
        g.AddDirected("b", "c", "bc");
        g.AddDirected("c", "a", "ca");
        
        HashSet<string> nodes = new(); 
        g.ForEachNode(n => nodes.Add(n));
        Assert.AreEqual(nodes, new string[]{"a", "b", "c"});
        
        HashSet<string> edges = new();
        g.ForEachEdge((_, _, e) => edges.Add(e));
        Assert.AreEqual(edges, new string[]{"ab", "bc", "ca"});
    }

    [Test]
    public void ContainsEdge() {
        Graph<string, string> g = new();
        Assert.False(g.ContainsEdge("a", "b"));
        
        g.AddDirected("a", "b", "ab");
        Assert.True(g.ContainsEdge("a", "b"));
        Assert.False(g.ContainsEdge("b", "a"));

        g.AddUndirected("b", "c", "bc");
        Assert.True(g.ContainsEdge("b", "c"));
        Assert.True(g.ContainsEdge("c", "b"));
    }

    [Test]
    public void RemoveEdge() {
        Graph<string, string> g = new();
        g.AddUndirected("a", "b", "ab");
        g.AddUndirected("b", "c", "bc");
        g.AddUndirected("c", "d", "cd");

        Assert.True(g.ContainsEdge("a", "b"));
        Assert.True(g.ContainsEdge("b", "a"));
        g.RemoveUndirected("b", "a");
        Assert.False(g.ContainsEdge("b", "a"));
        Assert.False(g.ContainsEdge("a", "b"));

        Assert.True(g.ContainsEdge("b", "c"));
        Assert.True(g.ContainsEdge("c", "b"));
        g.RemoveDirected("b", "c");
        Assert.False(g.ContainsEdge("b", "c"));
        Assert.True(g.ContainsEdge("c", "b"));

        Assert.True(g.ContainsEdge("c", "d"));
        Assert.True(g.ContainsEdge("d", "c"));
    }

    [Test]
    public void FindEdge() {
        Graph<string, string> g = new();
        g.AddUndirected("a", "b", "ab");
        g.AddUndirected("b", "c", "bc");
        g.AddUndirected("c", "d", "cd");

        string found = "?";
        Assert.True(g.FindEdge("c", "b", ref found));
        Assert.AreEqual(found, "bc");

        Assert.False(g.FindEdge("d", "a", ref found));

        Dictionary<string, string> bEdges = g.FindEdges("b");
        Assert.Contains(KeyValuePair.Create("c", "bc"), bEdges);
        Assert.Contains(KeyValuePair.Create("a", "ab"), bEdges);
        Assert.False(bEdges.ContainsKey("d"));
    }
}
