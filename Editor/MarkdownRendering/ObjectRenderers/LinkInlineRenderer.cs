using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using UnityEngine.Networking;

namespace Unity.Markdown.ObjectRenderers
{
    public class LinkInlineRenderer : MarkdownObjectRenderer<UIMarkdownRenderer, LinkInline>
    {
        protected override void Write(UIMarkdownRenderer renderer, LinkInline obj)
        {
            string link = obj.GetDynamicUrl != null ? obj.GetDynamicUrl() ?? obj.Url : obj.Url;
            if (!obj.IsImage)
            {
                renderer.OpenLink(link);
                renderer.WriteChildren(obj);
                renderer.CloseLink();
            }
            else
            {
                var uwr = new UnityWebRequest(link, UnityWebRequest.kHttpVerbGET);
                var imgElem = renderer.AddImage();

                uwr.downloadHandler = new DownloadHandlerTexture();
                var asyncOp = uwr.SendWebRequest();

                asyncOp.completed += operation =>
                {
                    imgElem.image = DownloadHandlerTexture.GetContent(uwr);
                    imgElem.tooltip = "This is a test";
                    uwr.Dispose();
                };
            }
        }
    }
}