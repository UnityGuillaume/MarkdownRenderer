using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Unity.Markdown.ObjectRenderers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;

namespace Unity.Markdown
{
    public class UIMarkdownRenderer : RendererBase
    {
        private static StyleSheet s_DefaultStylesheet = null;
        private static UIMarkdownRenderer s_StaticRenderer = null;

        public class Command
        {
            public string CommandName;
            public string[] CommandParameters;
        };

        public delegate void CommandHandlerDelegate(Command cmd);
        
        public VisualElement RootElement { protected set; get; }
        public bool LockTextCreation { get; set; } = false;
        public int IndentLevel { get; set; } = 0;

        public string FileFolder => m_FileFolder;

        private Stack<VisualElement> m_BlockStack = new Stack<VisualElement>();

        //useful when using relative file path
        private string m_FileFolder;

        private Label m_CurrentBlockText;
        private Texture m_LoadingTexture;

        private PropertyInfo m_TextHandleFieldInfo;
        private FieldInfo m_TextInfoFieldInfo;

        private Type m_TextElementInfoType;
        private FieldInfo m_TextElementInfoFieldInfo;

        private Type m_LinkInfoType;
        private FieldInfo m_LinkFieldInfo;

        private Action<string> m_CurrentLinkHandler;

        private CommandHandlerDelegate m_CommandHandler;

        class LinkData
        {
            public List<LinkInfoCopy> LinkInfo;
            public List<TextElementInfoCopy> TextElementInfo;
            public bool HoveredIconDisplayed;
        }

        public override object Render(MarkdownObject markdownObject)
        {
            CreateNewRoot();
            Write(markdownObject);

            return this;
        }

        public static VisualElement GenerateVisualElement(string markdownText,  Action<string> linkHandler, bool includeScrollview = true, string filePath = "")
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

            s_StaticRenderer.m_CurrentLinkHandler = linkHandler;
            s_StaticRenderer.m_FileFolder = System.IO.Path.GetDirectoryName(filePath).Replace("Assets", Application.dataPath);

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

        public static void RegisterCommandHandler(CommandHandlerDelegate handler)
        {
            s_StaticRenderer.m_CommandHandler += handler;
        }

        public static void SendCommand(Command cmd)
        {
            s_StaticRenderer.m_CommandHandler.Invoke(cmd);
        }

        void DefaultCommandHandler(Command cmd)
        {
            switch (cmd.CommandName)
            {
                case "log":
                    if (cmd.CommandParameters.Length == 0) return;
                    Debug.Log(cmd.CommandParameters[0]);
                    break;
                case "highlight":
                {
                    if (cmd.CommandParameters.Length != 2)
                    {
                        Debug.LogError("Command highlight with another number of parameters than 2");
                        return;
                    }

                    Highlighter.Highlight(cmd.CommandParameters[0], cmd.CommandParameters[1], HighlightSearchMode.Identifier);
                }
                    break;
            }
        }

        void CreateNewRoot()
        {
            RootElement = new VisualElement();
            RootElement.name = "RendererRoot";
            RootElement.AddToClassList("mainBody");
            
            m_BlockStack.Clear();
            m_BlockStack.Push(RootElement);
        }

        public UIMarkdownRenderer()
        {
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

            m_TextHandleFieldInfo = typeof(TextElement).GetProperty("textHandle", BindingFlags.NonPublic|BindingFlags.Instance);
            Type textCoreHandleType = Type.GetType("UnityEngine.UIElements.TextCoreHandle, UnityEngine.UIElementsModule");
            m_TextInfoFieldInfo =
                textCoreHandleType.GetField("m_TextInfoMesh", BindingFlags.NonPublic | BindingFlags.Instance);

            Type textInfoType =
                Type.GetType("UnityEngine.TextCore.Text.TextInfo, UnityEngine.TextCoreTextEngineModule");
            
            m_LinkFieldInfo = textInfoType.GetField("linkInfo");
            m_TextElementInfoFieldInfo = textInfoType.GetField("textElementInfo");

            m_LinkInfoType = Type.GetType("UnityEngine.TextCore.Text.LinkInfo, UnityEngine.TextCoreTextEngineModule");
            m_TextElementInfoType =
                Type.GetType("UnityEngine.TextCore.Text.TextElementInfo, UnityEngine.TextCoreTextEngineModule");

            m_CommandHandler += DefaultCommandHandler;
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
                string line = slices[i].ToString();
                if (i != lines.Count - 1) line += "\n";
                
                WriteText(line);
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
            imgElement.AddToClassList("md-image");
            imgElement.scaleMode = ScaleMode.ScaleToFit;
            imgElement.image = m_LoadingTexture;

            m_BlockStack.Peek().Add(imgElement);
            StartBlock();

            return imgElement;
        }

        public void OpenLink(string linkTarget)
        {
            if (m_CurrentBlockText.userData == null)
            {
                //this capture the current click handler, so i fm_CurrentLinkHandler is changed before the link is clicked
                //we use the right one.
                var clickHandler = m_CurrentLinkHandler;
                m_CurrentBlockText.RegisterCallback<MouseMoveEvent>(MouseMoveOnLink);
                m_CurrentBlockText.RegisterCallback<ClickEvent>((evt) =>
                {
                    var lnk = CheckLinkAgainstCursor(evt.target as Label, evt.localPosition);
                    if (lnk != null)
                    {
                        string target = lnk.GetLinkId();
                        clickHandler(target);
                    }
                });
                m_CurrentBlockText.RegisterCallback<MouseLeaveEvent>(MouseLeaveOnLink);

                m_CurrentBlockText.userData = new LinkData();
            }

            m_CurrentBlockText.text += "<link=" + linkTarget + "><color=#4C7EFF><u>";
        }

        public void CloseLink()
        {
            m_CurrentBlockText.text += "</u></color></link>";
        }

        void MouseMoveOnLink(MouseMoveEvent evt)
        {
            var label = evt.target as Label;
            var linkData = label.userData as LinkData;
            var link = CheckLinkAgainstCursor(label, evt.localMousePosition);

            if (link == null)
            {
                if (linkData.HoveredIconDisplayed)
                {
                    linkData.HoveredIconDisplayed = false;
                    label.RemoveFromClassList("linkHovered");
                    
                    //needed to force the cursor to update
                    label.SendEvent(new MouseOverEvent());
                }
            }
            else
            {
                if (!linkData.HoveredIconDisplayed)
                {
                    linkData.HoveredIconDisplayed = true;
                    label.AddToClassList("linkHovered");
                    
                    //needed to force the cursor to update
                    label.SendEvent(new MouseOverEvent());
                }
            }
            
        }

        void MouseLeaveOnLink(MouseLeaveEvent evt)
        {
            var label = evt.target as Label;
            var linkData = label.userData as LinkData;
            
            if (linkData.HoveredIconDisplayed)
            {
                linkData.HoveredIconDisplayed = false;
                label.RemoveFromClassList("linkHovered");
            }
        }

        LinkInfoCopy CheckLinkAgainstCursor(Label target, Vector2 localMousePosition)
        {
            var data = target.userData as LinkData;

            if (data == null)
                return null;

            var localPosInvert = localMousePosition;
            localPosInvert.y = target.localBound.height - localPosInvert.y;

            foreach (var link in data.LinkInfo)
            {
                for (int i = 0; i < link.linkTextLength; i++)
                {
                    var textInfo = data.TextElementInfo[link.linkTextfirstCharacterIndex + i];
                    Rect r = new Rect(textInfo.bottomLeft,
                        new Vector2(textInfo.topRight.x - textInfo.topLeft.x, textInfo.topLeft.y - textInfo.bottomLeft.y));
                    
                    if (r.Contains(localPosInvert))
                    { //hovering that link
                        return link;
                    }
                }
            }

            return null;
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

            if (m_CurrentBlockText.userData != null)
            {
                m_CurrentBlockText.generateVisualContent = m_CurrentBlockText.generateVisualContent + OnGenerateLinkVisualContent;
            }
            
            m_BlockStack.Pop(); 
        }

        void OnGenerateLinkVisualContent(MeshGenerationContext ctx)
        {
            Label l = ctx.visualElement as Label;

            LinkData data = l.userData as LinkData;
            
            var th = m_TextHandleFieldInfo.GetValue(ctx.visualElement);
            var textInfo = m_TextInfoFieldInfo.GetValue(th);
            
            var linkInfoArray = m_LinkFieldInfo.GetValue(textInfo) as Array;
            var textElementInfoArray = m_TextElementInfoFieldInfo.GetValue(textInfo) as Array;

            data.TextElementInfo = new List<TextElementInfoCopy>();
            foreach (var obj in textElementInfoArray)
            {
                TextElementInfoCopy cpy = new TextElementInfoCopy();
                CopyFromTo(m_TextElementInfoType, obj, ref cpy);
                data.TextElementInfo.Add(cpy);
            }

            data.LinkInfo = new List<LinkInfoCopy>();
            foreach (var itm in linkInfoArray)
            {
                LinkInfoCopy cpy = new LinkInfoCopy();
                CopyFromTo(m_LinkInfoType, itm, ref cpy);
                data.LinkInfo.Add(cpy);
            }
            
        }

        static void CopyFromTo<T>(Type fromType, object from, ref T to)
        {
            var membersSource = fromType.GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.NonPublic);
            var membersDest = typeof(T).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.NonPublic);
            
            foreach (var info in membersSource)
            {
                var dest = membersDest.FirstOrDefault(fieldInfo => fieldInfo.Name == info.Name);

                if (dest != null)
                {
                    var value = info.GetValue(from);
                    dest.SetValue(to, value);
                }
            }
        }

        //Those are just copy of the internal class in TextCore to easily copy the content of those by reflection as their
        //counterpart are internal. If this is exposed someday, can be removed.
    
        internal class TextElementInfoCopy
        {
            public char character;
            public int index;
            public int spriteIndex;
            public Material material;
            public int materialReferenceIndex;
            public bool isUsingAlternateTypeface;
            public float pointSize;
            public int lineNumber;
            public int pageNumber;
            public int vertexIndex;
            public Vector3 topLeft;
            public Vector3 bottomLeft;
            public Vector3 topRight;
            public Vector3 bottomRight;
            public float origin;
            public float ascender;
            public float baseLine;
            public float descender;
            public float xAdvance;
            public float aspectRatio;
            public float scale;
            public Color32 color;
            public Color32 underlineColor;
            public Color32 strikethroughColor;
            public Color32 highlightColor;
            public bool isVisible;
        }

        internal class LinkInfoCopy
        {
            public int hashCode;
            public int linkIdFirstCharacterIndex;
            public int linkIdLength;
            public int linkTextfirstCharacterIndex;
            public int linkTextLength;
            char[] linkId;
    
            public string GetLinkId() => new string(this.linkId, 0, this.linkIdLength);
        }
    }
}