# Markdown UIElement

This package add rendering of markdown file in Unity by using UIElement.

It will override the default inspector on .md file and offer a simple
API to get a VisualElement root with the markdown as children to add to
you own windows and tools.

It also include a special Attribute to display Markdown doc file for a given Monobehaviour and
the Markdown implementation handle some special keyword link to simplify link in a Unity Project.

- [Installation](#installation)
- [Special syntax](#note-and-special-syntax)
  - [Relative Paths](#relative-path)
  - [Commands](#commands)
- [MarkdownDoc Attribute](#markdowndoc-attribute)
- [Render Markdown in your tools](#render-markdown-in-your-own-tools)


# Installation

- clone that repository
- In the Package Manager, click the + sign on the rop right corner
- Select add a package on disk
- select the package.json in that repo

# Note and Special Syntax

The implementation is a bit more restrictive than most markdown renderer, so this will list some kirk.
It also handle special keyword/command, especially in link, specific to unity.

## Relative path

Local path are handled relatively to where the Markdown file is.

so `![Tutorial image](./doc/image.png)` will load the image.png that is in the doc folder next to the MD
file. The handler look for `..` and `.` at the start of a path to make it relative. 

_Note that explicit file protocol, `file://./doc/image.png` will **not work** as the system won't be able
to modify the path to path the current folder of the MD file_

## Search path

A special handler for this Unity implementation is the `![My Image](search:special_doc)` link type.

This will search for a file called `special_doc` in the asset folder, and will replace with its path.

- In an image link, this allow to store the image anywhere and the file will find it.
- In a link, this will look for the file and set it as the current selection. This means it allows :
  - To link to other Markdown file in the project without having to know their location, as when a Markdown file is selected, it get rendered.
  - To highlight and select any other asset your doc may want to point to (prefab, image, script etc.)

Note however that it doesn't support multiple file with the same name and will always return the first found.

## Commands

You can trigger a command through a link. By using the syntax `[link text](cmd:cmdName)` where cmdName is
the name of a command.

This package got a couple of builtin command but you can also write your own.

> **NOTE : SPACE ARE NOT PERMITTED IN COMMAND, THEY BREAK THE LINK**. so `[click](cmd:log(this is log)` won't
work. `[click](cmd:save(material,scene))` would work but `[click](cmd:save(material, scene))` would
**NOT** work because of the space between `,` and `scene`

### Built-in Commands

- `log(word)` : print the word in the console. 
- `highlight(window,text)` : wrapper for the Unity function `Highlighter.Highlight` highlight the given text in the given window, useful for documentation (**careful
  about potential stray space that will break your link display on this one**)

### Custom Commands

Register a handler through `UIMarkdownRenderer.RegisterCommandHandler` which take a delegate with
a single parameter of type `Command`.

A `Command` contains the `CommandName` a string that is the command name specified in our link (in our
example `toolOpen`) and an array of string `CommandParameters` which is parameters you can send to the command

# MarkdownDoc Attribute

The package contains an attribute `MardownDoc(DocFileName)` that can be applied to a Monobehaviour. 

If the Markdown Doc Viewer is open (Windows > Markdown Doc View or double clicking on a markdown file) is open
and a Gameobject with a Monobehaviour that have a `MarkdownDoc` attribute is selected, the doc specified
in the attribute will be loaded in the window.

The file is search by name, so you do not have to put a full path, just the name without extension (e.g. for
a doc file that is in `Assets/Tools/Docs/MyBehaviourDoc.md` the attribute `MarkdownDoc("MyBehaviourDoc")` will work)

# Render Markdown in your own tools

The markdown renderer is using UIElement, so you can embed it in your own tools. 

Just call

`GenerateVisualElement(string markdownText,  Action<string> LinkHandler, bool includeScrollview = true, string filePath = "")`

- `markdownText` is the content of the markdown file to render
- `linkHandler` is the function called when a link is clicked. MarkdownViewer contains a default one you can use or copy
- `includeScrollview` is if the scrollview should be part of the returned VisualElement or not. Set to false if you want to put in your own scrollview.
- `filePath` is the path to the rendered file. Used by the image rendering code to find relative path image. If you don't use relative path image, you can leave to empty.