# Markdown UIElement

**This package is neither endorsed nor supported by Unity, this is a personal project**

This package add rendering of markdown file in Unity by using UIElement.

> This has only been tested in 2021.3 LTS and a bit 2022.2. As this use some Reflection to
> access some text property, it may breaks on other versions

It will override the default inspector on .md file and offer a simple
API to get a VisualElement root with the markdown as children to add to
you own windows and tools.

It also include a special Attribute to display Markdown doc file for a given Monobehaviour and
the Markdown implementation handle some special keyword link to simplify link in a Unity Project.

- [Installation](#installation)
- [Special syntax](#note-and-special-syntax)
  - [Relative Paths](#relative-path)
  - [Absolute Paths](#absolute-path)
  - [Search path](#search-path)
  - [Package Search Path](#package-search-path)
  - [Commands](#commands)
  - [Custom Class and USS Files](#custom-classes-and-uss-files)
- [MarkdownDoc Attribute](#markdowndoc-attribute)
- [Render Markdown in your tools](#render-markdown-in-your-own-tools)


# Installation

You can install this package either with :

- git :
  - in the Package Manager, click on the + at the top right corner
  - choose "add package from git URL"
  - paste the git URL from that repository (https may work better than ssh due to authentication)

- locally :
  - clone that repository
  - In the Package Manager, click the + sign on the rop right corner
  - Select add a package on disk
  - select the package.json in that repo

_Note on git method_ : this will require git to be installed on your machine (e.g. you can run git in your terminal)
If you installed git when the Unity editor or Hub was running, the PATH it used won't be updated so close them all completly before.
<br/>**ON WINDOWS this mean also right click -> quit on the hub icon in the tray icon that may be hidden next to the time/date***

# Note and Special Syntax

The Markdown support implementation may miss some bits, so this list some quirks it may have.
This also highlight some special keyword/command it has, especially in link, specific to unity.

## Relative path

Local path are handled relatively to where the Markdown file is.

so `![Tutorial image](./doc/image.png)` will load the image.png that is in the doc folder next to the MD
file. The handler look for `..` and `.` at the start of a path to make it relative. 

_Note that explicit file protocol, `file://./doc/image.png` will **not work** as the system won't be able
to modify the path to the current folder of the MD file_

## Absolute path

The system can handle absolute path in the of form of

`Assets/Folder/SubFolder/file.png`

It can also handle package path (refers to the documentation on [accessing packages assets](https://docs.unity3d.com/Manual/upm-assets.html) 
for how those path works). As an example to access this file you would use the path

`Packages/com.rtl.markdownrenderer/Readme.md`

## Search path

A special handler for this Unity implementation is the `![My Image](search:special_doc)` link type.

This will search for a file called `special_doc` in the Assets folder, and will use its path.

- In an image link, this allow to store the image anywhere and the file will find it.
- In a link, this will look for the file and set it as the current Editor selection. This means it allows :
  - To link to other Markdown file in the project without having to know their location, as when a Markdown file is selected, it get rendered.
  - To highlight and select any other asset your doc may want to point to (prefab, image, script etc.)

Note however that it doesn't support multiple file with the same name and will always return the first found.

## Package search path

The package search path works as the Search path but will only search in packages. 

`package:my_image.png`

There is no way to search in a specific packages, this will search in all packages, but will ignore the Assets folder. This is useful to avoid
having your package users files found by your search path.

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

## Custom Classes and USS Files

The renderer have the Markdig Extensions for YAML front matter and Generic attributes enabled. This means you can set custom class to elements
in your Markdown and give a custom uss file used to render it (in addition to the default ones)

### Custom classes

Custom classes are defined using Generic Attribute (see [Markdig Generic Attributes documentation](https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/GenericAttributesSpecs.md) 
for more info).

```
{.test-class}
This paragraph will have .test-class class assigned to it

{.whole-block}
> ## This is a quote block
> that whole block have the .whole-block class assigned to it
> {.line-class} This line will have .line-class class on it
> 
> Not this line though
```

As a note for image, you need to place the custom class **AFTER THE IMAGE LINE** as if you add it before, it will apply it the block before the image

`![Image](./path/to/image) {.classToApplyToImage}`

### Custom USS files

In addition to custom classes on elements in Markdown, you can specify a USS file to be used to render that file.

This use Markdig YAML front matter extension. To speicify the uss add this front matter at the top of your Markdown file

```markdown
---
uss: ./path/to/file.uss
---

This file will use the file specified in addition to the default USS
```

Refer to Markdig [YAML Front Matter documentation](https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/YamlSpecs.md) for the format possible
but this was tested only with two `---` delimiter.

**Path can be relative to the Markdown file (using `./` as the start of the path like in the example) or they can be absolute by using
a path starting with `Assets/` like `Assets/Docs/Styles/myStyle.uss`**  

_Note: there is no actual YAML parsing, the system only supports uss as a key for now_

# MarkdownDoc Attribute

The package contains an attribute `MarkdownDoc(DocFileName)` that can be applied to a Monobehaviour. 

If the Markdown Doc Viewer is open (Windows > Markdown Doc View or double clicking on a markdown file)
and a Gameobject with a Monobehaviour that have a `MarkdownDoc` attribute is selected, the doc specified
in the attribute will be loaded in the window.

The file is searched by name, so you do not have to put a full path, just the name without extension (e.g. for
a doc file that is in `Assets/Tools/Docs/MyBehaviourDoc.md` the attribute `MarkdownDoc("MyBehaviourDoc")` will work)

# Render Markdown in your own tools

The markdown renderer is using UIElement, so you can embed it in your own tools. 

Just call

```
GenerateVisualElement(string markdownText,  Action<string> LinkHandler, bool includeScrollview = true, string filePath = "")
```

- `markdownText` : the content of the markdown file to render
- `linkHandler` : the function called when a link is clicked. MarkdownViewer contains a default one you can use or copy
- `includeScrollview` : if the scrollview should be part of the returned VisualElement or not. Set to false if you want to put in your own scrollview.
- `filePath` : the path to the rendered file. Used by the image rendering code to find relative path image. If you don't use relative path image, you can leave to empty.