using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;
using Markdig;
using Markdig.Syntax;
using MarkdowRenderer;
using Unity.Markdown;
using UnityEngine.UIElements;

public class MDViewer : EditorWindow
{
    private TextAsset m_Asset;

    [OnOpenAssetAttribute(0)]
    public static bool HandleDblClick(int instanceID, int line)
    {
        var path = AssetDatabase.GetAssetPath(instanceID);
        if (path.Contains(".md"))
        {
            Open(EditorUtility.InstanceIDToObject(instanceID) as TextAsset);
            return true; 
        }
        
        return false;
    }

    static void Open(TextAsset asset)
    {
        var win = GetWindow<MDViewer>();
        win.m_Asset = asset;

        win.Setup();
    }
    
    private void CreateGUI()
    {
        
    }

    void Setup()
    {
       rootVisualElement.Add(UIMarkdownRenderer.GenerateVisualElement(m_Asset.text));
    }
}
