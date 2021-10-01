using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace Unity.Markdown.ObjectRenderers
{
    public class LiteralInlineRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, LiteralInline>
    {
        protected override void Write(UIMarkdownRenderer renderer, LiteralInline obj)
        {
            renderer.WriteText(obj.Content.ToString());
        }
    }
}