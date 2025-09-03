using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))] // Here we modify the default Editor to add a button
// Generate button
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (DrawDefaultInspector()) //If any value in editor was changed - width or height
        {
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor(); //regenerate automatically the map
            }
        }

        if(GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
