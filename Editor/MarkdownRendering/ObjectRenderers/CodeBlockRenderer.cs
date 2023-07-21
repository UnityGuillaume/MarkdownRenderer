using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace UIMarkdownRenderer.ObjectRenderers
{
    public class CodeBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, CodeBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, CodeBlock obj)
        {
            var attribute = obj.GetAttributes();
            List<string> classes = new () { "codeblock" };
            if(attribute.Classes != null)
                classes.AddRange(attribute.Classes);
            
            renderer.StartBlock(classes);

            renderer.StartNewText();
            renderer.WriteLeafRawLines( obj );
        
            renderer.FinishBlock();
        }
    }
}