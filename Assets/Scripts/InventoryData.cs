using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryData
{
    public Dictionary<string, int> items { get; } = new();

    public static InventoryData instance = new();

    public void Add(string name, int count)
    {
        if (items.ContainsKey(name)) items[name] += count;
        else items[name] = count;
    }

    public void Remove(string name, int count)
    {
        if (items.ContainsKey(name) && items[name] > count) items[name] -= count;
        else items.Remove(name);
    }
}
