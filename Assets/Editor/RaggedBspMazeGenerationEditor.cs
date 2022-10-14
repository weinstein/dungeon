using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaggedBspMazeGenerator))]
public class RaggedBspMazeGenerationEditor : Editor
{
    static int depth = 1;

    private void OnEnable()
    {
        
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RaggedBspMazeGenerator gen = (RaggedBspMazeGenerator)target;
        depth = EditorGUILayout.IntField("Depth: ", depth);
        if (GUILayout.Button("Generate"))
        {
            gen.Generate(depth);
        }
        if (GUILayout.Button("Clear"))
        {
            gen.EraseAll();
        }
    }
}
