![screenshot](cilurbo.png)

This is a proof-of-concept for implementing several ideas I have to investigate .net assemblies, largely around how good (or bad) IL linking is for applications. The basic parts, disassembler and decompiler, are useful by themselves for many other purposes.

As long as this remains a proof-of-concept (before 1.0) expect many breaking changes.

## Goals

* Easy to build, extend and update (to latest [ICSharpCode.Decompiler](https://github.com/icsharpcode/ILSpy/tree/master/ICSharpCode.Decompiler) versions)
* Cross platform (at least macOS and Linux)

## Features

* [Keyboard driven](https://github.com/spouliot/cilurbo/wiki/KeyBindings)
* [Metadata Tree](https://github.com/spouliot/cilurbo/wiki/MetadataTree)
* [View disassembly (IL)](https://github.com/spouliot/cilurbo/wiki/Disassembler)
* [View decompiled source code (C#)](https://github.com/spouliot/cilurbo/wiki/Decompiler)
* View metadata tables (in progress)
* [Gist support](https://github.com/spouliot/cilurbo/wiki/Gist)
    * Source code: [C#](https://gist.github.com/spouliot/7f212838bba691181c6153b3e51e2d54) and [IL](https://gist.github.com/spouliot/d04409250cf7b9549000f07523efc6f4)
    * [Metadata tables](https://gist.github.com/spouliot/6a7ac81007849b99ce351047e16aaedc)
* [Dash support](https://github.com/spouliot/cilurbo/wiki/Dash) : quickly open documentation for the selected item
* Analyzers
    * [P/Invoke Finder](https://github.com/spouliot/cilurbo/wiki/AnalyzerPInvokeFinder)
    * Method Uses

## TODO

* Add missing metadata tables
* Add decompiler options / preferences
* Add more analysis tools
* Add resources

Features are being added on a "as I need them" basis.

## Built on top

* [ICSharpCode.Decompiler](https://github.com/icsharpcode/ILSpy/tree/master/ICSharpCode.Decompiler)
* [Terminal.Gui](https://github.com/migueldeicaza/gui.cs)
