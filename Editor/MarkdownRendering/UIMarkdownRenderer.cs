using System;
using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Unity.Markdown.ObjectRenderers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Markdown
{

    public class UIMarkdownRenderer : RendererBase
    {
        private static StyleSheet s_DefaultStylesheet = null;
        private static UIMarkdownRenderer s_StaticRenderer = null;
        
        public VisualElement RootElement { protected set; get; }
        public bool LockTextCreation { get; set; } = false;
        public int IndentLevel { get; set; } = 0;

        private Stack<VisualElement> m_BlockStack = new Stack<VisualElement>();

        private Label m_CurrentBlockText;
        private Texture m_LoadingTexture;

        class LinkData
        {

        }

        public override object Render(MarkdownObject markdownObject)
        {
            RootElement.Clear();
            Write(markdownObject);
            return this;
        }

        public static VisualElement GenerateVisualElement(string markdownText, bool includeScrollview = true)
        {
            if (s_DefaultStylesheet == null)
            {
                s_DefaultStylesheet =  AssetDatabase.LoadAssetAtPath("Packages/com.rtl.markdownrenderer/Styles/MarkdownRenderer.uss", typeof(StyleSheet)) as StyleSheet;
            
                if(s_DefaultStylesheet == null)
                    Debug.LogError("Couldn't load the MarkdownRenderer.uss stylesheet");
            }

            if (s_StaticRenderer == null)
            {
                s_StaticRenderer = new UIMarkdownRenderer();
            }
            
            s_StaticRenderer.Render(Markdig.Markdown.Parse(markdownText));

            VisualElement returnElem = null;
            if (includeScrollview)
            {
                returnElem = new ScrollView();
                returnElem.Add(s_StaticRenderer.RootElement);
            }
            else
            {
                returnElem =  s_StaticRenderer.RootElement;
            }
            
            if(s_DefaultStylesheet != null)
                returnElem.styleSheets.Add(s_DefaultStylesheet);

            return returnElem;
        }

        public UIMarkdownRenderer()
        {
            RootElement = new VisualElement();
            RootElement.name = "RendererRoot";
            RootElement.AddToClassList("mainBody");
            m_BlockStack.Push(RootElement);

            m_LoadingTexture = EditorGUIUtility.Load("WaitSpin00") as Texture;

            ObjectRenderers.Add(new ParagraphBlockRenderer());
            ObjectRenderers.Add(new HeadingBlockRenderer());
            ObjectRenderers.Add(new ListBlockRenderer());
            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new ThematiceBreakBlockRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());

            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new LiteralInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
        }

        internal void WriteLeafBlockInline(LeafBlock block)
        {
            var inline = block.Inline as Inline;

            while (inline != null)
            {
                Write(inline);
                inline = inline.NextSibling;
            }
        }

        internal void WriteLeafRawLines(LeafBlock block)
        {
            if (block.Lines.Lines == null)
            {
                return;
            }

            var lines = block.Lines;
            var slices = lines.Lines;

            for (int i = 0; i < lines.Count; i++)
            {
                WriteText(slices[i].ToString() + "\n");
            }
        }

        public void StartNewText(params string[] classLists)
        {
            if (LockTextCreation)
                return;

            m_CurrentBlockText = new Label();
            foreach (var c in classLists)
            {
                m_CurrentBlockText.AddToClassList(c);
            }

            m_BlockStack.Peek().Add(m_CurrentBlockText);
        }

        public void WriteText(string text)
        {
            m_CurrentBlockText.text += text;
        }

        public Image AddImage()
        {
            FinishBlock();

            Image imgElement = new Image();
            imgElement.image = m_LoadingTexture;

            m_BlockStack.Peek().Add(imgElement);
            StartBlock();

            return imgElement;
        }

        public void OpenLink(string linkTarget)
        {
            if (m_CurrentBlockText.userData == null)
            {
                m_CurrentBlockText.RegisterCallback<MouseMoveEvent>(MouseMoveOnLink);
                m_CurrentBlockText.RegisterCallback<MouseLeaveEvent>(MouseLeaveOnLink);
            }

            m_CurrentBlockText.text += "<link=" + linkTarget + "><color=#4C7EFF><u>";
        }

        public void CloseLink()
        {
            m_CurrentBlockText.text += "</u></color></link>";
        }

        void MouseMoveOnLink(MouseMoveEvent evt)
        {

        }

        void MouseLeaveOnLink(MouseLeaveEvent evt)
        {

        }

        internal void StartBlock(params string[] classList)
        {
            if (LockTextCreation)
                return;

            var newBlock = new VisualElement();
            newBlock.AddToClassList("block");
            foreach (var c in classList)
            {
                newBlock.AddToClassList(c);
            }

            m_BlockStack.Peek().Add(newBlock);

            m_BlockStack.Push(newBlock);
        }

        internal void FinishBlock()
        {
            if (LockTextCreation)
            {
                //finishing a block when locked from creating new text mean we just jump a line (used by list)
                m_CurrentBlockText.text += "\n";
                return;
            }

            m_BlockStack.Pop();
        }
    }
}