using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class UIElementRenderer : Markdig.Renderers.RendererBase
{
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
        Write(markdownObject);
        return this;
    }

    public UIElementRenderer()
    {
        RootElement = new VisualElement();
        RootElement.name = "RendererRoot";
        RootElement.AddToClassList("mainBody");
        m_BlockStack.Push(RootElement);

        m_LoadingTexture = EditorGUIUtility.Load("WaitSpin00") as Texture;
        
        ObjectRenderers.Add(new UIElementBlockParagraphRenderer());
        ObjectRenderers.Add(new UIElementHeadingBlockRenderer());
        ObjectRenderers.Add(new UIElementBlockListRenderer());
        ObjectRenderers.Add(new UIElementCodeBlockRenderer());
        ObjectRenderers.Add(new UIElementThematicBreakBlockRenderer());
        ObjectRenderers.Add(new UIElementQuoteBlockRenderer());
        
        ObjectRenderers.Add(new UIElementEmphasisRenderer());
        ObjectRenderers.Add(new UIElementInlineLiteralRenderer());
        ObjectRenderers.Add(new UIElementLineBreakRenderer());
        ObjectRenderers.Add(new UIElementInlineCodeRenderer());
        ObjectRenderers.Add(new UIElementInlineLinkRenderer());
    }

    internal void WriteLeafBlockInline( LeafBlock block )
    {
        var inline = block.Inline as Inline;

        while( inline != null )
        {
            Write( inline );
            inline = inline.NextSibling;
        }
    }
    
    internal void WriteLeafRawLines( LeafBlock block )
    {
        if( block.Lines.Lines == null )
        {
            return;
        }

        var lines  = block.Lines;
        var slices = lines.Lines;

        for( int i = 0; i < lines.Count; i++ )
        {
            WriteText(slices[i].ToString() + "\n");
        }
    }
    
    public void StartNewText(params string[] classLists)
    {
        if(LockTextCreation)
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

public class UIElementBlockParagraphRenderer : MarkdownObjectRenderer<UIElementRenderer, ParagraphBlock>
{
    protected override void Write(UIElementRenderer renderer, ParagraphBlock obj)
    {
        renderer.StartBlock();

        renderer.StartNewText("paragraph");
        renderer.WriteLeafBlockInline( obj );
        
        renderer.FinishBlock();
    }
}

public class UIElementBlockListRenderer : MarkdownObjectRenderer<UIElementRenderer, ListBlock>
{
    protected override void Write(UIElementRenderer renderer, ListBlock obj)
    {
        renderer.StartBlock();

        renderer.StartNewText("list");

        bool lockValue = renderer.LockTextCreation;
        renderer.LockTextCreation = true;
        
        renderer.IndentLevel++;
        for( var i = 0; i < obj.Count; i++ )
        {
            renderer.WriteText("<indent="+ (renderer.IndentLevel * 12) +"px>");
            renderer.WriteText(obj.IsOrdered ? (i+1) + ". " : "\u2022 ");
            renderer.WriteChildren( obj[ i ] as ListItemBlock );
            renderer.WriteText("</indent>");
        }
        
        renderer.LockTextCreation = lockValue;
        
        renderer.IndentLevel--;
        renderer.FinishBlock();
    }
}

public class UIElementHeadingBlockRenderer : MarkdownObjectRenderer<UIElementRenderer, HeadingBlock>
{
    
    private static readonly string[] HeadingTexts = {
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
    };
    
    protected override void Write(UIElementRenderer renderer, HeadingBlock obj)
    {
        int index = obj.Level - 1;
        string headingText = ((uint)index < (uint)HeadingTexts.Length)
            ? HeadingTexts[index]
            : "h" + obj.Level.ToString(System.Globalization.CultureInfo.InvariantCulture);
        
        renderer.StartBlock();

        renderer.StartNewText(headingText, "header");
        renderer.WriteLeafBlockInline( obj );

        renderer.FinishBlock();
    }
}

public class UIElementCodeBlockRenderer : MarkdownObjectRenderer<UIElementRenderer, CodeBlock>
{
    protected override void Write(UIElementRenderer renderer, CodeBlock obj)
    {
        renderer.StartBlock("codeblock");

        renderer.StartNewText();
        renderer.WriteLeafRawLines( obj );
        
        renderer.FinishBlock();
    }
}

public class UIElementQuoteBlockRenderer : MarkdownObjectRenderer<UIElementRenderer, QuoteBlock>
{
    protected override void Write(UIElementRenderer renderer, QuoteBlock obj)
    {
        renderer.StartBlock("quote");
        renderer.WriteChildren(obj);
        renderer.FinishBlock();
    }
}

public class UIElementThematicBreakBlockRenderer : MarkdownObjectRenderer<UIElementRenderer, ThematicBreakBlock>
{
    protected override void Write(UIElementRenderer renderer, ThematicBreakBlock obj)
    {
        renderer.StartBlock("line");
        renderer.FinishBlock();
    }
}

public class UIElementLineBreakRenderer : MarkdownObjectRenderer<UIElementRenderer, LineBreakInline>
{
    protected override void Write(UIElementRenderer renderer, LineBreakInline obj)
    {
        renderer.WriteText(obj.IsHard ? "\n" : " ");
    }
}

public class UIElementEmphasisRenderer : MarkdownObjectRenderer<UIElementRenderer, EmphasisInline>
{
    protected override void Write(UIElementRenderer renderer, EmphasisInline obj)
    {
        renderer.WriteText(obj.DelimiterCount == 2 ? "<b>" : "<i>");
        renderer.WriteChildren(obj);
        renderer.WriteText(obj.DelimiterCount == 2 ? "</b>" : "</i>");
    }
}

public class UIElementInlineLiteralRenderer : MarkdownObjectRenderer<UIElementRenderer, LiteralInline>
{
    protected override void Write(UIElementRenderer renderer, LiteralInline obj)
    {
        renderer.WriteText(obj.Content.ToString());
    }
}

public class UIElementInlineCodeRenderer : MarkdownObjectRenderer<UIElementRenderer, CodeInline>
{
    protected override void Write(UIElementRenderer renderer, CodeInline obj)
    {
        renderer.WriteText(obj.Content.ToString());
    }
}

public class UIElementInlineLinkRenderer : MarkdownObjectRenderer<UIElementRenderer, LinkInline>
{
    protected override void Write(UIElementRenderer renderer, LinkInline obj)
    {
        string link = obj.GetDynamicUrl != null ? obj.GetDynamicUrl() ?? obj.Url : obj.Url;
        if (!obj.IsImage)
        {
            renderer.OpenLink(link);
            renderer.WriteChildren(obj);
            renderer.CloseLink();
        }
        else
        {
            var uwr = new UnityWebRequest(link, UnityWebRequest.kHttpVerbGET);
            var imgElem = renderer.AddImage();

            uwr.downloadHandler = new DownloadHandlerTexture();
            var asyncOp = uwr.SendWebRequest();

            asyncOp.completed += operation =>
            {
                imgElem.image = DownloadHandlerTexture.GetContent(uwr);
                imgElem.tooltip = "This is a test";
                uwr.Dispose();
            };
        }
    }
}