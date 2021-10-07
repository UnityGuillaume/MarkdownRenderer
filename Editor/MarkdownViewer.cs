using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Unity.Markdown;

public class MarkdownViewer : EditorWindow
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
        var win = GetWindow<MarkdownViewer>();
        win.m_Asset = asset;

        win.Setup();
    }

    void Setup()
    {
       rootVisualElement.Add(UIMarkdownRenderer.GenerateVisualElement(m_Asset.text, (lnk) => HandleLink(lnk, m_Asset)));
    }

    public static void HandleLink(string link, TextAsset context)
    {
        if (link.StartsWith("./") || link.StartsWith("../"))
        {//relative link
            var path = AssetDatabase.GetAssetPath(context);
            
        }
        else if (link.StartsWith("Assets/"))
        {//project link
            
        }
        else
        {//any other link is open normally
            Application.OpenURL(link);
        }
    }
}
