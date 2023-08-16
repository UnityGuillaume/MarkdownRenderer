using System;
using System.IO;
using UIMarkdownRenderer;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIMarkdownRenderer
{ 
     [CustomEditor(typeof(TextAsset))]
     public class MarkdownCustomEditor : Editor
     {
         private Editor m_DefaultEditor;
         private bool m_IsMDFile;
         private string m_TargetPath;

        public virtual void OnEnable()
        {
            m_TargetPath = AssetDatabase.GetAssetPath(target);

            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType("UnityEditor.TextAssetInspector");
            
            CreateCachedEditor(target, type, ref m_DefaultEditor);
            
            //TODO : handle also other extension? Potentially skip that and display every file as Markdown?
            m_IsMDFile = Path.GetExtension(AssetDatabase.GetAssetPath(target)) == ".md";
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_IsMDFile)
            {
                return UIMarkdownRenderer.GenerateVisualElement(File.ReadAllText(m_TargetPath), lnk => MarkdownViewer.HandleLink(lnk, m_TargetPath), true, m_TargetPath);
            }

            var elem = new IMGUIContainer(m_DefaultEditor.OnInspectorGUI);
                
            //by default the stylesheet seems to add some margin, so we reverse them to fit "right"
            elem.style.marginTop = 2;
            elem.style.marginLeft = -15;
            return elem;
        }
     }
}