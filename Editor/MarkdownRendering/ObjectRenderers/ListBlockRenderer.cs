using Markdig.Renderers;
using Markdig.Syntax;

namespace Unity.Markdown.ObjectRenderers
{
    public class ListBlockRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, ListBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, ListBlock obj)
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
}