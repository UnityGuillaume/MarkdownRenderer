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
         private UIMarkdownRenderer m_Renderer;
         
         private Editor m_DefaultEditor;
         private bool m_IsMDFile;
         private string m_TargetPath;


         private void Awake()
         {
             m_Renderer = new UIMarkdownRenderer(MarkdownViewer.HandleLink, true);
         }

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
                m_Renderer.OpenFile(Path.GetFullPath(m_TargetPath));
                return m_Renderer.RootElement;
            }

            var elem = new IMGUIContainer(m_DefaultEditor.OnInspectorGUI);
                
            //by default the stylesheet seems to add some margin, so we reverse them to fit "right"
            elem.style.marginTop = 2;
            elem.style.marginLeft = -15;
            return elem;
        }
     }
}