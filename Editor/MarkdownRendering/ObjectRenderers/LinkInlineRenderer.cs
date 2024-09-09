using System;
using System.Collections.Generic;
using System.IO;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

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
                link = renderer.ResolveLink(link);

                if (!link.StartsWith("http"))
                    link = "file://" + Path.Combine(renderer.FileFolder, link);

                var uwr = new UnityWebRequest(link, UnityWebRequest.kHttpVerbGET);
                var imgElem = renderer.AddImage();
                imgElem.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    if (imgElem.image != null)
                    {
                        var texture = imgElem.image;
                        float aspectRatio = texture.width / (float)texture.height;
                        float targetWidth = evt.newRect.width;
                        float targetHeight = targetWidth/aspectRatio;

                        if (!Mathf.Approximately(targetWidth, evt.newRect.width) ||
                            !Mathf.Approximately(targetHeight, evt.newRect.height))
                        {
                            //we always set the width as 100% as this will allow to resize on parent resize
                            //be height will be based on aspect ratio
                            imgElem.style.width = Length.Percent(100);
                            imgElem.style.height = targetHeight;
                        }
                    }
                });
                
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
                    try
                    {
                        imgElem.image = DownloadHandlerTexture.GetContent(uwr);
                        //force a resize to call our custom callback
                        imgElem.style.width = 10;
                    }
                    catch (Exception x)
                    {
                        if (!x.Message.StartsWith("HTTP/1.1 404"))
                            throw;

                        Debug.LogWarning($"{x.Message}: {link}");
                    }
                    uwr.Dispose();
                };
            }
        }
    }
}