using Markdig.Renderers;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class HeadingBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, HeadingBlock>
    {
    
        private static readonly string[] HeadingTexts = {
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
        };
    
        protected override void Write(UIMarkdownRenderer renderer, HeadingBlock obj)
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
}