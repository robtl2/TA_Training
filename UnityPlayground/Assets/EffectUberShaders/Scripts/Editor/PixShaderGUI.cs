using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using PixRenderPipline;

public class PixShaderGUI : SShaderGUI
{

    public override bool DrawElse(MaterialEditor materialEditor, MaterialProperty property, string displayLabel)
    {
        if (!displayLabel.StartsWith("SSS/"))
            return false;

        Material mat = materialEditor.target as Material;

        displayLabel = displayLabel.Substring(4);

        string[] labels = PixSSSProfiles.profileNames;

        List<int> values = new List<int>();
        for (int i = 0; i < labels.Length; ++i) values.Add(i);

        int lastValue = (int)mat.GetFloat(property.name);
        lastValue = values.IndexOf(lastValue);
        EditorGUI.indentLevel++;
        int curIndex = EditorGUILayout.Popup(displayLabel, lastValue, labels);
        EditorGUI.indentLevel--;
        int curValue = values[curIndex];

        if (curValue != lastValue)
            mat.SetFloat(property.name, (float)curValue);

        return true;
    }
}

