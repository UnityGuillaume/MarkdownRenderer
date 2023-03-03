# Markdown UIElement

This package add rendering of markdown file by using UIElement.

It will override the default inspector on .md file and offer a simple
API to get a VisualElement root with the makrdown as children to add to
you own windows and tools.

# Installation

- clone that repository
- In the Package Manager, click the + sign on the rop right corner
- Select add a package on disk
- select the package.json in that repo

# Special Syntax

The implementation handle special keyword/command, especially in link, specific to unity.

## Relative path

Using `rel:` before a path mean "relative to the markdown file". This is useful e.g. for documentation with
image on a folder next to the MD file. That way redistributing the documentation doesn't need to know where
in the project the file will be, just that the images are in a folder next to the document.

E.g. for this hierarchy
```
    |- MyDoc.md
    |- Images
        |- MyScreen.png
```

using the markdown

`![A screenshot](rel:Images/MyScreen.png)`

will automatically change that path to the proper full path for the system to load the image

## Commands

You can trigger a command through a link. By using the syntax

`[Click here to open the tool](cmd:toolOpen)`

Then registering a handler through `UIMarkdownRenderer.RegisterCommandHandler` which take a delegate with
a single parameter of type `Command`.

A `Command` contains the `CommandName` a string that is the command name specified in our link (in our
example `toolOpen`) and an array of string `CommandParameters` which is parameters you can send to the command
through `[Click here to start material check](cmd:objectCheck(material))`

**NOTE : SPACE ARE NOT PERMITTED IN COMMAND, THEY BREAK THE LINK**. so `[click](cmd:log(this is log)` won't 
work. But `[click](cmd:save(material,scene))` would work. But `[click](cmd:save(material, scene))` would
**NOT** work because of the space between `,` and `scene`

There is a built-in CommandHandler which handle the given commands.

### Built-in Commands

- `log(word)` : print the word in the console. Can be useful to test if your command handler is registered properly
- `highlight(window,text)` : wrapper for the Unity function `Highlighter.Highlight` highlight the given text in the given window, useful for documentation (**careful
about potential stray space that will break your link display on this one**)

# Known limitations

Right now only external link works, don't handle files or anchors links.

