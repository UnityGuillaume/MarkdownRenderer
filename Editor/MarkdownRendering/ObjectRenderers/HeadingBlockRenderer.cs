using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using UnityEngine.UIElements;

namespace UIMarkdownRenderer.ObjectRenderers
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

            var newBlock = renderer.StartBlock();

            var attribute = obj.GetAttributes();
            List<string> classes = new () { headingText, "header" };
            if(attribute.Classes != null)
                classes.AddRange(attribute.Classes);
            
            renderer.StartNewText(classes);
            renderer.WriteLeafBlockInline( obj );

            renderer.FinishBlock();
            
            //the simplest is to find the Label in the new block and read its text to have the label
            var label = newBlock.Q<Label>();
            renderer.RegisterHeader(label.text, label);
        }
    }
}