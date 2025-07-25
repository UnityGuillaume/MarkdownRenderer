using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal class MediaPopoutWindow : EditorWindow
{
    private VisualElement m_OriginalContainer;
    private int m_OriginalPositionIndex;

    private VisualElement m_PopOutElement;

    private void OnFocus()
    {
        //needed to receive key event
        rootVisualElement.focusable = true;
        rootVisualElement.Focus();
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyPressed);
    }

    private void OnLostFocus()
    {
        //remove focus from the root
        rootVisualElement.Blur();
        rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyPressed);
    }

    void OnKeyPressed(KeyDownEvent evt)
    {
        if(evt.keyCode == KeyCode.Escape)
            Close();
    }

    public static void Popout(VisualElement element)
    {
        var win = CreateInstance<MediaPopoutWindow>();
        win.ShowUtility();

        //we go back up the parent chain to make sure every stylesheet that could be relevant to that element is also
        //added to the window
        var currentElement = element;
        while (currentElement != null)
        {
            for (int i = 0; i < currentElement.styleSheets.count; ++i)
            {
                if (!win.rootVisualElement.styleSheets.Contains(currentElement.styleSheets[i]))
                {
                    win.rootVisualElement.styleSheets.Add(currentElement.styleSheets[i]);
                }
            }
            currentElement = currentElement.parent;
        }

        //Make sure we get all the stylesheets of the original element added to the popup window
        for (int i = 0; i < element.styleSheets.count; ++i)
        {
            Debug.Log(element.styleSheets[i]);
            if (!win.rootVisualElement.styleSheets.Contains(element.styleSheets[i]))
            {
                win.rootVisualElement.styleSheets.Add(element.styleSheets[i]);
            }
        }

        win.m_PopOutElement = element;

        win.m_OriginalContainer = element.parent;
        win.m_OriginalPositionIndex = win.m_OriginalContainer.IndexOf(element);

        win.rootVisualElement.Add(element);
        win.m_PopOutElement.AddToClassList("popout-media");
    }

    //This is called by the tutorial window when changing page to make sure we don't have any left over pop out media
    public static void EnsureClosed()
    {
        if (HasOpenInstances<MediaPopoutWindow>())
        {
            var win = GetWindow<MediaPopoutWindow>();
            win.Close();
        }
    }
    private void OnDestroy()
    {
        if(m_PopOutElement == null)
            return;

        m_PopOutElement.RemoveFromClassList("popout-media");
        if (m_OriginalContainer != null)
        {
            m_OriginalContainer.Insert(m_OriginalPositionIndex, m_PopOutElement);
        }
    }
}
