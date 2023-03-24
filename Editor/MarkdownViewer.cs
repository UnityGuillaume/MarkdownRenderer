using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Unity.Markdown;
using Object = UnityEngine.Object;

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

    [MenuItem("Window/Markdown Doc Viewer")]
    static void DisplayWindow()
    {
        var win = GetWindow<MarkdownViewer>();
        win.Show();
    }

    static void Open(TextAsset asset)
    {
        var win = GetWindow<MarkdownViewer>();
        win.m_Asset = asset;

        win.Setup();
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            var cmps = Selection.activeGameObject.GetComponents<MonoBehaviour>();
            foreach (var cmp in cmps)
            {
                var t = cmp.GetType();
                
                if (Attribute.GetCustomAttribute(t, typeof(MarkdownDocAttribute)) is MarkdownDocAttribute attribute)
                {
                    var files = AssetDatabase.FindAssets($"{attribute.DocName} t:TextAsset");
                    if (files.Length > 0)
                    {
                        var textAsset =
                            AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(files[0]));

                        m_Asset = textAsset;
                        Setup();
                    }
                }
            }
        }
        else if (Selection.activeObject is TextAsset txtAsset)
        {
            var path = AssetDatabase.GetAssetPath(txtAsset);

            if (Path.GetExtension(path) == ".md")
            {
                m_Asset = txtAsset;
                Setup();
            }
        }
    }

    void Setup()
    { 
        rootVisualElement.Clear();
        rootVisualElement.Add(UIMarkdownRenderer.GenerateVisualElement(m_Asset.text, (lnk) => HandleLink(lnk, m_Asset), true, AssetDatabase.GetAssetPath(m_Asset)));
    }

    public static void HandleLink(string link, TextAsset context)
    {
        if (link.StartsWith("file://"))
        {
            //relative link
            //remove the handler type
            var cleanPath = link.Replace("file://", "");

            if (cleanPath.StartsWith("Assets") || cleanPath.StartsWith("./") || cleanPath.StartsWith("../"))
            {
                var combined = "";
                if (!cleanPath.StartsWith("Assets"))
                {
                    var assetPath = AssetDatabase.GetAssetPath(context);
                    assetPath = Path.GetDirectoryName(assetPath);
                    combined = Path.GetFullPath(Path.Join(assetPath, cleanPath)).Replace("\\", "/");
                    combined = combined.Replace(Application.dataPath, "Assets");
                }
                else
                {
                    combined = cleanPath;
                }

                var obj = AssetDatabase.LoadAssetAtPath<Object>(combined);
                Selection.activeObject = obj;
            }
        }
        else if (link.StartsWith("search"))
        {
            //this is a relative link, so find the actual link
            link = link.Replace("search:", "");

            var files = AssetDatabase.FindAssets(link);

            if (files.Length == 0)
            {
                Debug.LogError($"Couldn't find link : {link}");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(files[0]);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
        }
        else if (link.StartsWith("cmd:"))
        {
            string cmdName;
            string[] parameters;
            
            link = link.Replace("cmd:", "");
            int openingParenthesis = link.IndexOf('(');
            if (openingParenthesis == -1)
            {
                //no parameters
                cmdName = link;
                parameters = Array.Empty<string>();
            }
            else
            {
                //we find the closing one
                int closingParenthesis = link.IndexOf(')');

                cmdName = link.Substring(0, openingParenthesis);
                string parametersString = link.Substring(openingParenthesis+1, closingParenthesis - openingParenthesis - 1);

                parameters = parametersString.Split(',');

            }
            
            UIMarkdownRenderer.Command cmd = new UIMarkdownRenderer.Command()
            {
                CommandName = cmdName,
                CommandParameters = parameters
            };
                
            UIMarkdownRenderer.SendCommand(cmd);
        }

        //any other link is open normally
        Application.OpenURL(link);
    }
}
