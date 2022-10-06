using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/ItemDescriptor")]
public class ItemDescriptor : ScriptableObject
{
    new public string name;
    public string description;
    public GameObject prefab;
}
