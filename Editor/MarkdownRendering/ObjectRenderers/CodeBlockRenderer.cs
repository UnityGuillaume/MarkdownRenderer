using Markdig.Renderers;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class CodeBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, CodeBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, CodeBlock obj)
        {
            renderer.StartBlock("codeblock");

            renderer.StartNewText();
            renderer.WriteLeafRawLines( obj );
        
            renderer.FinishBlock();
        }
    }
}