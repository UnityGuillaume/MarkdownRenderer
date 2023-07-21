using System.Collections;
using System.Collections.Generic;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using UIMarkdownRenderer;
using UnityEngine;

namespace UIMarkdownRenderer
{
    //This is not a renderer as we ignore the YAML front matter block and only use its data
    public class YamlFrontMatterHandler : MarkdownObjectRenderer<UIMarkdownRenderer, YamlFrontMatterBlock>
    {
        protected override void Write(UIMarkdownRenderer renderer, YamlFrontMatterBlock obj)
        {
            //we do not handle real YAML as for now we only support specific uss, so manually parse
            foreach (var line in obj.Lines)
            {
                var data = line.ToString();
                if(string.IsNullOrEmpty(data)) continue;

                string[] content = data.Split(':');
                if (content[0].Trim() == "uss")
                {
                    string path = content[1].Trim();
                    renderer.AddCustomUSS(path);
                }
            }
        }
    }
}