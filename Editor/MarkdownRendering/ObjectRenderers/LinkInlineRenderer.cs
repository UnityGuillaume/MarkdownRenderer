using System.Collections.Generic;
using System.IO;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace UIMarkdownRenderer.ObjectRenderers
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
                link = UIMarkdownRenderer.ResolveLink(link);
                
                if(!link.StartsWith("http"))
                    link = "file://"+Path.GetFullPath(link);
                
                var uwr = new UnityWebRequest(link, UnityWebRequest.kHttpVerbGET);
                var imgElem = renderer.AddImage();
                
                var attribute = obj.GetAttributes();
                if (attribute.Classes != null)
                {
                    foreach (var c in attribute.Classes)
                    {
                        imgElem.AddToClassList(c);
                    }
                }
                
                imgElem.tooltip = obj.FirstChild.ToString();

                uwr.downloadHandler = new DownloadHandlerTexture();
                var asyncOp = uwr.SendWebRequest();

                asyncOp.completed += operation =>
                {
                    imgElem.image = DownloadHandlerTexture.GetContent(uwr);
                    uwr.Dispose();
                };
            }
        }
    }
}