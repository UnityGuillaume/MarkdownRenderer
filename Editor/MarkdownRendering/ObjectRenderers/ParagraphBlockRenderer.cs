using Markdig.Renderers;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class ParagraphBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, ParagraphBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, ParagraphBlock obj)
        {
            renderer.StartBlock();

            renderer.StartNewText("paragraph");
            renderer.WriteLeafBlockInline( obj );
        
            renderer.FinishBlock();
        }
    }
}