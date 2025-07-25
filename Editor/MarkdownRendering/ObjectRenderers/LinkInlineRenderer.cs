using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
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
                
                string[] videoFilesTypes = { ".asf", ".avi", ".dv", ".m4v", ".mov", ".mp4", ".mpg", ".mpeg", ".ogv", ".vp8", ".webm", ".wmv" };

                VisualElement resultingElement = null;

                var ext = Path.GetExtension(link);

                if (videoFilesTypes.Contains(ext))
                {// video
                    var vidPlayer = renderer.AddVideoPlayer();
                    vidPlayer.SetVideoUrl(link, false);
                    resultingElement = vidPlayer;
                }
                else
                {// hopefully image
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

                    resultingElement = imgElem;
                }
                
               
                
                var attribute = obj.GetAttributes();
                if (attribute.Classes != null)
                {
                    foreach (var c in attribute.Classes)
                    {
                        resultingElement.AddToClassList(c);
                    }
                }
                
                resultingElement.tooltip = obj.FirstChild.ToString();
            }
        }
    }
}