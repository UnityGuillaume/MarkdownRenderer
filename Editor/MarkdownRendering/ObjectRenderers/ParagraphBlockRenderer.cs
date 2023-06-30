using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class ParagraphBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, ParagraphBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, ParagraphBlock obj)
        {
            renderer.StartBlock();

            var attribute = obj.GetAttributes();
            List<string> classes = new () { "paragraph" };
            if(attribute.Classes != null)
                classes.AddRange(attribute.Classes);

            renderer.StartNewText(classes);
            renderer.WriteLeafBlockInline( obj );
        
            renderer.FinishBlock();
        }
    }
}