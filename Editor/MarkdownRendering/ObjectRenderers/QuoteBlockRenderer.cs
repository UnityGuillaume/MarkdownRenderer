using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class QuoteBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, QuoteBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, QuoteBlock obj)
        {
            var attribute = obj.GetAttributes();
            List<string> classes = new () { "quote" };
            if (attribute.Classes != null)
                classes.AddRange(attribute.Classes);

            renderer.StartBlock(classes);
            renderer.WriteChildren(obj);
            renderer.FinishBlock();
        }
    }
}