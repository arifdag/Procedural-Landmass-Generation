using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapPreview mapPreview = (MapPreview) target;
        
        if(DrawDefaultInspector())
                mapPreview.DrawMapOnEditor();
        
        if (GUILayout.Button("Generate"))
        {
            mapPreview.DrawMapOnEditor();
        }
        
        
    }
}
