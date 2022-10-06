using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph<NodeT, EdgeT> //where EdgeT : IComparable<EdgeT>
{
    private class Node
    {
        public Node(NodeT data)
        {
            this.data = data;
        }
        public NodeT data { get; }
        public Dictionary<NodeT, EdgeT> outgoing = new();
        public Dictionary<NodeT, EdgeT> incoming = new();
    }

    private struct HalfEdge
    {
        public Node src;
        public Node dst;
        public EdgeT data;
    }

    private Dictionary<NodeT, Node> nodes = new();

    private int Size() { return nodes.Count; }

    public void Clear()
    {
        nodes.Clear();
    }

    public override string ToString()
    {
        string ret = "edges {\n";
        foreach (KeyValuePair<NodeT, Node> kv in nodes)
        {
            foreach (KeyValuePair<NodeT, EdgeT> edge in kv.Value.outgoing)
            {
                ret += kv.Key + " -> " + edge.Key + " [" + edge.Value + "]\n";
            }
            foreach (KeyValuePair<NodeT, EdgeT> redge in kv.Value.incoming)
            {
                ret += kv.Key + " <- " + redge.Key + " [" + redge.Value + "]\n";
            }
        }
        ret += "}";
        return ret;
    }

    private Node LookupOrInsert(NodeT k)
    {
        Node existing = nodes.GetValueOrDefault(k, null);
        if (existing != null) return existing;
        return nodes[k] = new(k);
    }

    private void LookupOrInsert(NodeT src, NodeT dst, EdgeT e)
    {
        Node srcNode = LookupOrInsert(src);
        if (!srcNode.outgoing.ContainsKey(dst))
        {
            srcNode.outgoing[dst] = e;
        }
        Node dstNode = LookupOrInsert(dst);
        if (!dstNode.incoming.ContainsKey(src))
        {
            dstNode.incoming[src] = e;
        }
    }

    public void Add(NodeT k)
    {
        LookupOrInsert(k);
    }

    public void AddDirected(NodeT src, NodeT dst, EdgeT e)
    {
        LookupOrInsert(src, dst, e);
    }

    public void AddUndirected(NodeT src, NodeT dst, EdgeT e)
    {
        LookupOrInsert(src, dst, e);
        LookupOrInsert(dst, src, e);
    }

    public void RemoveDirected(NodeT src, NodeT dst)
    {
        if (!nodes.ContainsKey(src) || !nodes.ContainsKey(dst)) return;
        Node srcNode = nodes[src];
        Node dstNode = nodes[dst];
        srcNode.outgoing.Remove(dst);
        dstNode.incoming.Remove(src);
    }

    public void RemoveUndirected(NodeT src, NodeT dst)
    {
        RemoveDirected(src, dst);
        RemoveDirected(dst, src);
    }

    public void Remove(NodeT key)
    {
        if (!nodes.ContainsKey(key)) return;
        Node keyNode = nodes[key];
        List<NodeT> toRemove = new();
        foreach (NodeT dst in keyNode.outgoing.Keys) toRemove.Add(dst);
        foreach (NodeT dst in toRemove) RemoveDirected(key, dst);
        toRemove.Clear();
        foreach (NodeT src in keyNode.incoming.Keys) toRemove.Add(src);
        foreach (NodeT src in toRemove) RemoveDirected(src, key);
        nodes.Remove(key);
    }

    public bool FindEdge(NodeT src, NodeT dst, ref EdgeT edge)
    {
        if (!nodes.ContainsKey(src)) return false;
        Node srcNode = nodes[src];
        if (!srcNode.outgoing.ContainsKey(dst)) return false;
        edge = srcNode.outgoing[dst];
        return true;
    }

    public bool ContainsEdge(NodeT src, NodeT dst)
    {
        if (!nodes.ContainsKey(src)) return false;
        return nodes[src].outgoing.ContainsKey(dst);
    }

    public Dictionary<NodeT, EdgeT> FindEdges(NodeT src)
    {
        if (!nodes.ContainsKey(src)) return null;
        return nodes[src].outgoing;
    }

    public void ForEachEdge(Action<NodeT, NodeT, EdgeT> onEdge)
    {
        foreach (KeyValuePair<NodeT, Node> ns in nodes)
        {
            NodeT src = ns.Key;
            foreach (KeyValuePair<NodeT, EdgeT> e in ns.Value.outgoing)
            {
                NodeT dst = e.Key;
                onEdge(src, dst, e.Value);
            }
        }
    }

    public void ForEachNode(Action<NodeT> onNode)
    {
        foreach (NodeT n in nodes.Keys) onNode(n);
    }

    public Graph<NodeT, EdgeT> MinimalSpanningTree(IComparer<EdgeT> edgeCmp)
    {
        List<HalfEdge> sortedEdges = new();
        foreach (KeyValuePair<NodeT, Node> node in nodes)
        {
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.Value.outgoing)
            {
                HalfEdge he = new();
                he.src = node.Value;
                he.dst = nodes[edge.Key];
                he.data = edge.Value;
                sortedEdges.Add(he);
            }
        }
        sortedEdges.Sort((x, y) => edgeCmp.Compare(x.data, y.data));

        Graph<NodeT, EdgeT> ret = new();
        int curEdgeIdx = 0;
        int nEdges = 0;
        DisjointSets<Node> nodeSets = new();
        while (nEdges < Size() - 1)
        {
            HalfEdge e = sortedEdges[curEdgeIdx++];
            if (nodeSets.Find(e.src) == nodeSets.Find(e.dst)) continue;
            nodeSets.Union(e.src, e.dst);
            ret.AddDirected(e.src.data, e.dst.data, e.data);
            Debug.Log("add span " + e.src.data + " -> " + e.dst.data);
            ++nEdges;
        }
        return ret;
    }

    public Graph<NodeT, EdgeT> Undirected()
    {
        Graph<NodeT, EdgeT> undirected = new();
        foreach (KeyValuePair<NodeT, Node> node in nodes)
        {
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.Value.outgoing)
            {
                undirected.AddUndirected(node.Key, edge.Key, edge.Value);
            }
        }
        return undirected;
    }

    static T FirstOrDefault<T>(IEnumerable<T> elems, T fallback)
    {
        foreach (T elem in elems)
        {
            return elem;
        }
        return fallback;
    }

    public void TraverseDepthFirst(Action<NodeT> onNode, Action<NodeT, NodeT, EdgeT> onEdge)
    {
        HashSet<Node> visited = new();
        Stack<Node> toBeVisited = new();
        Node seed = FirstOrDefault(nodes.Values, null);
        if (seed != null) toBeVisited.Push(seed);
        while (toBeVisited.Count > 0)
        {
            Node node = toBeVisited.Pop();
            visited.Add(node);
            onNode(node.data);
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.outgoing)
            {
                Node dst = nodes[edge.Key];
                if (!visited.Contains(dst))
                {
                    onEdge(node.data, dst.data, edge.Value);
                    toBeVisited.Push(dst);
                }
            }
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.incoming)
            {
                Node src = nodes[edge.Key];
                if (!visited.Contains(src))
                {
                    onEdge(src.data, node.data, edge.Value);
                    toBeVisited.Push(src);
                }
            }
        }
    }

    public void TraverseBreadthFirst(Action<NodeT> onNode, Action<NodeT, NodeT, EdgeT> onEdge)
    {
        HashSet<Node> visited = new();
        Queue<Node> toBeVisited = new();
        Node seed = FirstOrDefault(nodes.Values, null);
        if (seed != null) toBeVisited.Enqueue(seed);
        while (toBeVisited.Count > 0)
        {
            Node node = toBeVisited.Dequeue();
            visited.Add(node);
            onNode(node.data);
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.outgoing)
            {
                Node dst = nodes[edge.Key];
                if (!visited.Contains(dst))
                {
                    onEdge(node.data, dst.data, edge.Value);
                    toBeVisited.Enqueue(dst);
                }
            }
            foreach (KeyValuePair<NodeT, EdgeT> edge in node.incoming)
            {
                Node src = nodes[edge.Key];
                if (!visited.Contains(src))
                {
                    onEdge(src.data, node.data, edge.Value);
                    toBeVisited.Enqueue(src);
                }
            }
        }
    }
}
