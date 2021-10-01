using Markdig.Renderers;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class QuoteBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, QuoteBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, QuoteBlock obj)
        {
            renderer.StartBlock("quote");
            renderer.WriteChildren(obj);
            renderer.FinishBlock();
        }
    }
}