using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkdownDocAttribute : Attribute
{
    public string DocName => _docName;
    private string _docName;

    public MarkdownDocAttribute(string docName)
    {
        _docName = docName;
    }
}
