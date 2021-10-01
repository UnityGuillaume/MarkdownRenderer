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
using UnityEngine.UIElements;

public class MDViewer : EditorWindow
{
    static StyleSheet s_StyleSheet = null;
    
    private TextAsset m_Asset;
    private MarkdownDocument m_Document;

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

        win.m_Document = Markdown.Parse(win.m_Asset.text);

        win.Setup();
    }
    
    private void CreateGUI()
    {
        
    }

    void Setup()
    {
        if (s_StyleSheet == null)
        {
            var guids = AssetDatabase.FindAssets("t:StyleSheet MarkdownStyle");

            if (guids.Length == 0)
            {
                Debug.LogError("Couldn't find the USS File for the MD Viewer");
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
        }
        
        if(s_StyleSheet != null)
            rootVisualElement.styleSheets.Add(s_StyleSheet);
        
        UIElementRenderer render = new UIElementRenderer();
        render.Render(m_Document);

        var scrollView = new ScrollView();
        rootVisualElement.Add(scrollView);
        
        scrollView.Add(render.RootElement);
    }
}
