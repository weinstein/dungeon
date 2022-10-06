using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisjointSets<T>
{
    private Dictionary<T, T> parent = new();

    public void Clear() { parent.Clear(); }

    public T Find(T key)
    {
        if (parent.ContainsKey(key))
        {
            return parent[key] = Find(parent[key]);
        } else
        {
            return key;
        }
    }

    public void Union(T lhs, T rhs)
    {
        T lhsRep = Find(lhs);
        T rhsRep = Find(rhs);
        parent[lhsRep] = rhsRep;
    }
}
