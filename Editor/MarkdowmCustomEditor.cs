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
         private TextAsset m_Target;

        public virtual void OnEnable()
        {
            m_Target = target as TextAsset;

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
                return UIMarkdownRenderer.GenerateVisualElement(m_Target.text, (lnk) => { MarkdownViewer.HandleLink(lnk, m_Target);}, true, AssetDatabase.GetAssetPath(m_Target));
            }

            var elem = new IMGUIContainer(m_DefaultEditor.OnInspectorGUI);
                
            //by default the stylesheet seems to add some margin, so we reverse them to fit "right"
            elem.style.marginTop = 2;
            elem.style.marginLeft = -15;
            return elem;
        }
     }
}