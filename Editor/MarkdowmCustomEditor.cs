using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarkdowRenderer
{ 
     [CustomEditor(typeof(TextAsset))]
     public class MarkdownCustomEditor : Editor
     {
         private Editor m_DefaultEditor;
         private bool m_IsMDFile;

        public virtual void OnEnable()
        {
            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType("UnityEditor.TextAssetInspector");
            
            CreateCachedEditor(target, type, ref m_DefaultEditor);
            
            //TODO : handle also other extension? Poitentially skip that and display every file as Markdown?
            m_IsMDFile = Path.GetExtension(AssetDatabase.GetAssetPath(target)) == ".md";
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_IsMDFile)
            {
                return new Label("This is a MD test");
            }
            else
            {
                var elem = new IMGUIContainer(m_DefaultEditor.OnInspectorGUI);
                
                //by default the stylesheet seems to add some margin, so we reverse them to fit "right"
                elem.style.marginTop = 2;
                elem.style.marginLeft = -15;
                return elem;
            }
        }
     }
}