using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UIMarkdownRenderer.ObjectRenderers;
using UIMarkdownRenderer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using TextElement = UnityEngine.UIElements.TextElement;

namespace UIMarkdownRenderer
{
    public class UIMarkdownRenderer : RendererBase
    {
        private static StyleSheet s_DefaultStylesheet = null;
        private static VisualTreeAsset s_VideoPlayerElementPrefab;
        private static StyleSheet s_VideoPlayerStyleSheet = null;
        
        private static MarkdownPipeline s_StaticPipeline = new MarkdownPipelineBuilder().UseGenericAttributes().UseYamlFrontMatter().Build();

        public class Command
        {
            public string CommandName;
            public string[] CommandParameters;
        };

        public delegate void CommandHandlerDelegate(Command cmd);
        
        /// <summary>
        /// The root visual element. Add this to your UI tree to render the Markdown.
        /// </summary>
        public VisualElement RootElement { protected set; get; }
        
        /// <summary>
        /// This is the root of the content. This will be the same as RootElement if a ScrollView wasn't asked when
        /// creating the Renderer, otherwise this will be the ScrollView
        /// </summary>
        public VisualElement ContentRoot { protected set; get; }
        public bool LockTextCreation { get; set; } = false;
        public int IndentLevel { get; set; } = 0;
        
        public string FileFolder => m_FileFolder;
        
        private bool m_IncludeScrollView;
        private Stack<VisualElement> m_BlockStack = new Stack<VisualElement>();

        private string m_LocalFilePath;
        //useful when using relative file path
        private string m_FileFolder;

        private Label m_CurrentBlockText;
        private Texture m_LoadingTexture;

        //used for jumping to header, the key is a whitespace removed version of the header
        public struct HeaderEntry
        {
            public string HeaderKey;
            public Label HeaderLabel;
        }
        private List<HeaderEntry> m_HeadersToLabelMappings = new();

#if !UNITY_2022_2_OR_NEWER
        private PropertyInfo m_TextHandleFieldInfo;
        private PropertyInfo m_TextInfoFieldInfo;

        private Type m_TextElementInfoType;
        private FieldInfo m_TextElementInfoFieldInfo;

        private Type m_LinkInfoType;
        private FieldInfo m_LinkFieldInfo;
        
        class LinkData
        {
            public List<LinkInfoCopy> LinkInfo;
            public List<TextElementInfoCopy> TextElementInfo;
            public bool HoveredIconDisplayed;
        }
#endif

        private Action<string, UIMarkdownRenderer> m_CurrentLinkHandler;

        private CommandHandlerDelegate m_CommandHandler;

        private List<StyleSheet> m_CustomStylesheets;

        public override object Render(MarkdownObject markdownObject)
        {
            CreateNewRoot();
            Write(markdownObject);

            return this;
        }
        
        //Call to handle all type of link : relative, absolute and special search
        public string ResolveLink(string link)
        {
            if (link.StartsWith("search:"))
            {
                //this is a relative link, so find the actual link
                link = link.Replace("search:", "");

                var files = AssetDatabase.FindAssets(link);

                if (files.Length == 0)
                {
                    Debug.LogError($"Couldn't find image {link}");
                    return "";
                }

                link = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(files[0]));
            }
            else if (link.StartsWith("package:"))
            {
                //will search only in packages
                link = link.Replace("package:", "");
                    
                var files = AssetDatabase.FindAssets($"a:packages {link}");

                if (files.Length == 0)
                {
                    Debug.LogError($"Couldn't find link : {link}");
                    return "";
                }

                link = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(files[0]));
            }
            else if (link.StartsWith(".") || link.StartsWith(".."))
            {
                link = "/" + link;
                link = FileFolder + link;
            }
            else if (link.StartsWith("Packages") || link.StartsWith("Assets"))
            {
                link = Path.GetFullPath(link);
            }

            link = link.Replace("\\", "/");

            return link;
        }

        public void RegisterCommandHandler(CommandHandlerDelegate handler)
        {
            m_CommandHandler += handler;
        }

        public void SendCommand(Command cmd)
        {
            m_CommandHandler.Invoke(cmd);
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
            ContentRoot.Clear();
            m_BlockStack.Clear();
            m_BlockStack.Push(ContentRoot);
        }

        public UIMarkdownRenderer(Action<string, UIMarkdownRenderer> LinkHandler, bool includeScrollview = true)
        {
            if (s_DefaultStylesheet == null)
            {
                s_DefaultStylesheet =  AssetDatabase.LoadAssetAtPath("Packages/com.rtl.markdownrenderer/Styles/MarkdownRenderer.uss", typeof(StyleSheet)) as StyleSheet;
                if(s_DefaultStylesheet == null)
                    Debug.LogError("Couldn't load the MarkdownRenderer.uss stylesheet");
            }

            if (s_VideoPlayerElementPrefab == null)
            {
                s_VideoPlayerElementPrefab =
                    AssetDatabase.LoadAssetAtPath(
                        "Packages/com.rtl.markdownrenderer/Editor/VideoElement/VideoPlayerElement.uxml",
                        typeof(VisualTreeAsset)) as VisualTreeAsset;
                
                s_VideoPlayerStyleSheet =
                    AssetDatabase.LoadAssetAtPath(
                        "Packages/com.rtl.markdownrenderer/Editor/VideoElement/VideoPlayerElement.uss",
                        typeof(StyleSheet)) as StyleSheet;
            }

            m_LoadingTexture = EditorGUIUtility.Load("WaitSpin00") as Texture;

            ObjectRenderers.Add(new YamlFrontMatterHandler());
            
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
            
#if UNITY_2022_2_OR_NEWER
            //In 2022.2 and after there is an event when hovering over link in a label so we can use that instead of reflection
#else
#if UNITY_2022_1_OR_NEWER
            string handleName = "uitkTextHandle";
            string textHandleTypeName = "UnityEngine.TextCore.Text.TextHandle, UnityEngine.TextCoreTextEngineModule";
            string textInfoName = "textInfo";
#else 
            string handleName = "textHandle";
            string textHandleTypeName = "UnityEngine.UIElements.TextCoreHandle, UnityEngine.UIElementsModule";
            string textInfoName = "textInfoMesh";
#endif
             m_TextHandleFieldInfo = typeof(TextElement).GetProperty(handleName, BindingFlags.NonPublic|BindingFlags.Instance);
            Type textCoreHandleType = Type.GetType(textHandleTypeName);
            m_TextInfoFieldInfo =
                textCoreHandleType.GetProperty(textInfoName, BindingFlags.NonPublic | BindingFlags.Instance);

            Type textInfoType =
                Type.GetType("UnityEngine.TextCore.Text.TextInfo, UnityEngine.TextCoreTextEngineModule");
            
            m_LinkFieldInfo = textInfoType.GetField("linkInfo");
            m_TextElementInfoFieldInfo = textInfoType.GetField("textElementInfo");

            m_LinkInfoType = Type.GetType("UnityEngine.TextCore.Text.LinkInfo, UnityEngine.TextCoreTextEngineModule");
            m_TextElementInfoType =
                Type.GetType("UnityEngine.TextCore.Text.TextElementInfo, UnityEngine.TextCoreTextEngineModule");
#endif
            m_CommandHandler += DefaultCommandHandler;
            
            m_CurrentLinkHandler = LinkHandler;
            m_CustomStylesheets = new List<StyleSheet>();
            m_HeadersToLabelMappings.Clear();
            
            ContentRoot = new VisualElement
            {
                name = "RendererRoot"
            };
            ContentRoot.AddToClassList("mainBody");

            m_IncludeScrollView = includeScrollview;
            if (includeScrollview)
            {
                RootElement = new ScrollView();
                RootElement.name = "ScrollViewRoot";
                RootElement.Add(ContentRoot);
            }
            else
            {
                RootElement = ContentRoot;
            }

            if(s_DefaultStylesheet != null)
                RootElement.styleSheets.Add(s_DefaultStylesheet);

            foreach (var stylesheet in m_CustomStylesheets)
            {
                RootElement.styleSheets.Add(stylesheet);
            }
        }

        public void OpenFile(string filePath)
        {
            m_LocalFilePath = Path.GetDirectoryName(filePath);
            m_FileFolder = Path.GetFullPath(m_LocalFilePath);
            
            SetMarkdown(File.ReadAllText(filePath));
        }

        public void SetMarkdown(string markdownText)
        {
            var parsed = Markdown.Parse(markdownText, s_StaticPipeline);
            Render(parsed);
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

        public void AddCustomUSS(string path)
        {
            if (path.StartsWith("."))
            {
                path = m_LocalFilePath + path.Remove(0, 1);
            }
            
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);

            if (stylesheet == null)
            {
                Debug.LogError($"Couldn't find custom USS {path} defined in file {m_LocalFilePath}");
                return;
            }
            
            m_CustomStylesheets.Add(stylesheet);
        }

        public void StartNewText(List<string> classLists)
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

        public void StartNewText(params string[] classLists)
        {
            StartNewText(classLists.ToList());
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

        public VideoPlayerElement AddVideoPlayer()
        {
            FinishBlock();

            VideoPlayerElement newPlayer = s_VideoPlayerElementPrefab.Instantiate().Q<VideoPlayerElement>();
            newPlayer.styleSheets.Add(s_VideoPlayerStyleSheet);
            
            m_BlockStack.Peek().Add(newPlayer);
            StartBlock();

            return newPlayer;
        }

#if !UNITY_2022_2_OR_NEWER
        public void OpenLink(string linkTarget)
        {
            if (m_CurrentBlockText.userData == null)
            {
                //this capture the current click handler, so if m_CurrentLinkHandler is changed before the link is clicked
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
#else
        
        public void OpenLink(string linkTarget)
        {
            m_CurrentBlockText.text += "<link=" + linkTarget + "><color=#4C7EFF><u>";
            m_CurrentBlockText.RegisterCallback<PointerOverLinkTagEvent>(evt =>
            {
                (evt.target as Label).AddToClassList("linkHovered");
            });
            m_CurrentBlockText.RegisterCallback<PointerOutLinkTagEvent>(evt =>
            {
                (evt.target as Label).RemoveFromClassList("linkHovered");
            });
            
            m_CurrentBlockText.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                m_CurrentLinkHandler(evt.linkID, this);
            });
        }
    #endif

        public void CloseLink()
        {
            m_CurrentBlockText.text += "</u></color></link>";
        }

        public void RegisterHeader(string headerText, Label label)
        {
            //we first change all space to - but also remove any non alpha numeric character 
            var sanitizedHeader = headerText.Replace(" ", "-").ToLowerInvariant();
            sanitizedHeader = String.Concat(sanitizedHeader.Where(c => char.IsLetterOrDigit(c) || c == '-'));
            
            m_HeadersToLabelMappings.Add(new HeaderEntry()
            {
                HeaderKey = sanitizedHeader,
                HeaderLabel = label
            });
        }

        /// <summary>
        /// Will return the Label that correspond to the link (in the form of #header-text) passed as parameter 
        /// </summary>
        /// <param name="link">An anchor link in the form #header-text</param>
        /// <returns></returns>
        public Label GetLabelFromHeaderAnchor(string link)
        {
            foreach (var headerEntry in m_HeadersToLabelMappings)
            {
                if (headerEntry.HeaderKey.StartsWith(link.Remove(0, 1)))
                    return headerEntry.HeaderLabel;
            }
            return null;
        }

        public void ScrollToHeader(string link)
        {
            if (!m_IncludeScrollView)
                return;

            var label = GetLabelFromHeaderAnchor(link);
            if (label == null) return;
            
            
            var scrollView = RootElement as ScrollView;
            var localLabelPos = scrollView.WorldToLocal(label.worldBound);
            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, localLabelPos.y);
        }


#if !UNITY_2022_2_OR_NEWER
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
#endif
        
        internal VisualElement StartBlock(List<string> classList)
        {
            if (LockTextCreation)
                return null;

            var newBlock = new VisualElement();
            newBlock.AddToClassList("block");
            foreach (var c in classList)
            {
                newBlock.AddToClassList(c);
            }

            m_BlockStack.Peek().Add(newBlock);

            m_BlockStack.Push(newBlock);

            return newBlock;
        }

        internal VisualElement StartBlock(params string[] classList)
        {
            return StartBlock(classList.ToList());
        }

        internal void FinishBlock()
        {
            if (LockTextCreation)
            {
                //finishing a block when locked from creating new text mean we just jump a line (used by list)
                m_CurrentBlockText.text += "\n";
                return;
            }

#if !UNITY_2022_2_OR_NEWER
            if (m_CurrentBlockText.userData != null)
            {
                m_CurrentBlockText.generateVisualContent += OnGenerateLinkVisualContent;
            }
#endif
            
            m_BlockStack.Pop(); 
        }

#if !UNITY_2022_2_OR_NEWER
        void OnGenerateLinkVisualContent(MeshGenerationContext ctx)
        {
            Label l = ctx.visualElement as Label;

            LinkData data = l.userData as LinkData;
            
            var th = m_TextHandleFieldInfo.GetValue(ctx.visualElement);
            var textInfo = m_TextInfoFieldInfo.GetValue(th);
            
            var linkInfoArray = m_LinkFieldInfo.GetValue(textInfo) as Array;
            var textElementInfoArray = m_TextElementInfoFieldInfo.GetValue(textInfo) as Array;
            
            Debug.Log((ctx.visualElement as Label).text);
            
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
                Debug.Log("Link info copied");
                data.LinkInfo.Add(cpy);
            }
            
            Debug.Log($"Data link info length {data.LinkInfo.Count}");
            
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
#endif

        //Those are just copy of the internal class in TextCore to easily copy the content of those by reflection as their
        //counterpart are internal. If this is exposed someday, can be removed.
    
#if !UNITY_2022_2_OR_NEWER
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

#endif
    }
}