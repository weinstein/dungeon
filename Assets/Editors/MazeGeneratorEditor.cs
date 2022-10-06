using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MazeGeneratorBehavior), true)]
public class MazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MazeGeneratorBehavior gen = (MazeGeneratorBehavior)target;
        if (GUILayout.Button("Generate"))
        {
            gen.Generate();
        }
    }
}
