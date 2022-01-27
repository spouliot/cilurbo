using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;
using Terminal.Gui.Trees;

using Cilurbo.Services;

namespace Cilurbo;

partial class Program {

	static int sep = 40;
	static readonly TreeView assemblies = new () {
		X = 0,
		Y = 2,
		Width = sep,
		Height = Dim.Fill (1),
		CanFocus = true,
		MultiSelect = false,
	};

	static readonly TabView tabs = new () {
		X = sep,
		Y = 1,
		Width = Dim.Fill (),
		Height = Dim.Fill (1),
		CanFocus = true,
	};

	static readonly TableView table = new () {
		X = 0,
		Y = 0,
		Width = Dim.Fill (),
		Height = Dim.Fill (1),
		CanFocus = true,
		MultiSelect = false,
		Shortcut = Key.CtrlMask | Key.A,
	};

	static int SetupUI (Toplevel top)
	{
		StatusBar statusBar = new (new StatusItem [] {
			new (Key.F9, "~F9~ Menu", null),
			new (Key.Null, "...", null),
		});

		assemblies.SelectionChanged += (sender, e) => {
			statusBar.Items [1].Title = e.NewValue.Text;
		};

		assemblies.ObjectActivated += ObjectActivated;

		MenuBar menu = new (new MenuBarItem [] {
			new ("_File", new MenuItem? [] {
				new ("_Open...", "", FileOpen, null, null, Key.CtrlMask | Key.O),
				new ("Open _List...", "", FileOpenList, null, null, Key.CtrlMask | Key.R),
				new ("_Save List...", "", FileSaveList, () => { return assemblies.Objects.Any (); }, null, Key.CtrlMask | Key.S),
				null,
				new ("_Quit", "", FileQuit, null, null, Key.CtrlMask | Key.Q),
			}),
			new ("_Edit", new MenuItem [] {
				new ("_Copy", "", EditCopy, null, null, Key.CtrlMask | Key.C),
			}),
			new ("_View", new MenuItem? [] {
				new ("Assemblies Tree", "", ViewAssemblyTree, null, null, Key.F1),
				new ("Disassembler View (IL)", "", ViewDisassembler, null, null, Key.F2),
				new ("Decompiler View (C#)", "", ViewDecompiler, null, null, Key.F3),
				new ("Metadata Tables", "", ViewMetadataTables, () => { return assemblies.SelectedObject is AssemblyNode; }, null, Key.F4),
				null,
				new ("Collapse all tree nodes", "", ViewCollapseAllNodes, null, null, Key.CtrlMask | Key.U),
				null,
				new ("Enlarge TreeView", "", EnlargeTreeView, null, null, Key.ShiftMask | Key.CursorRight),
				new ("Reduce TreeView", "", ReduceTreeView, null, null, Key.ShiftMask | Key.CursorLeft),
				null,
				new ("_Preferences...", "", ViewPreferences, null, null, Key.F5),
			}),
			new ("_Help", new MenuItem [] {
				new ("Key Bindings...", "", HelpKeyBindings),
				new ("About...", "", HelpAbout),
			}),
		});
		top.Add (menu);

		Label label = new ("Loaded Assemblies") {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = 1,
			CanFocus = false,
		};

		top.Add (label, assemblies, tabs, statusBar);
		top.ColorScheme = Colors.Base;
		SetupScrollBar (); // superview won't be null here
		return 0;
	}

	static void ObjectActivated (ObjectActivatedEventArgs<ITreeNode> e)
	{
		var node = e.ActivatedObject;
		switch (node) {
		case AssemblyReferenceNode arn:
			if (arn.Tag is AssemblyReference ar) {
				var pe = AssemblyResolver.Resolver.Resolve (ar);
				if (pe is not null) {
					var a = assemblies.Select ((n) => pe.Equals (n.Tag) && (n is AssemblyNode));
					if (a is null) {
						var an = assemblies.Add (pe);
						assemblies.SelectedObject = an;
						assemblies.GoTo (an);
					}
					EnsureSourceView ().Show (pe);
				}
			}
			break;
		case BaseTypeNode btn:
			var f = assemblies.Select ((n) => btn.Tag.Equals (n.Tag) && (n is TypeNode));
			// it might not be loaded yet (or not found)
			if (f is null) {
				var pe = (btn.Tag as ITypeDefinition)!.ParentModule.PEFile;
				if (pe is not null) {
					assemblies.Add (pe);
					// try again with the assembly loaded
					f = assemblies.Select ((n) => btn.Tag.Equals (n.Tag) && (n is TypeNode));
				}
			}
			if (f is not null)
				EnsureSourceView ().Show (f.Tag);
			break;
		default:
			assemblies.Expand (node);
			EnsureSourceView ().Show (node.Tag);
			assemblies.SetFocus ();
			break;
		}
	}

	static void EnlargeTreeView ()
	{
		if (sep > 80)
			return;
		sep++;
		var p = tabs.X + 1;
		tabs.X = p;
		tabs.Width = Dim.Fill ();
		assemblies.Width += 1;
	}

	static void ReduceTreeView ()
	{
		if (sep < 10)
			return;
		sep--;
		tabs.X -= 1;
		tabs.Width = Dim.Fill ();
		assemblies.Width -= 1;
	}

	static void FileOpen ()
	{
		using OpenDialog d = new ("Open Assemblies", "", assemblies_file_extensions, OpenDialog.OpenMode.File) {
			AllowsMultipleSelection = true,
		};
		Application.Run (d);
		if (!d.Canceled && d.FilePaths.Count > 0) {
			foreach (var f in d.FilePaths) {
				LoadFile (f);
			}
		}
	}

	static void FileOpenList ()
	{
		using OpenDialog d = new ("Open Assembly List", "", lists_file_extensions, OpenDialog.OpenMode.File);
		Application.Run (d);
		if (!d.Canceled && (d.FilePath is not null)) {
			// this means a list can include another list, e.g. an app list can include a framework list
			LoadFile (d.FilePath.ToString ()!);
		}
	}

	static void FileSaveList ()
	{
		using SaveDialog d = new ("Save Assembly List", "", lists_file_extensions);
		Application.Run (d);
		if (!d.Canceled && (d.FilePath is not null)) {
			StringBuilder list = new ();
			foreach (var node in assemblies.Objects) {
				if (node.Tag is PEFile pe) {
					list.AppendLine (pe.FileName);
				}
			}
			File.AppendAllText (d.FilePath.ToString ()!, list.ToString ());
		}
	}

	static void FileQuit ()
	{
		Application.Top.Running = false;
	}

	static void EditCopy ()
	{
		if (Application.Top.Focused is TextView tv)
			tv.Copy ();
	}

	static void ViewAssemblyTree ()
	{
		assemblies.SetFocus ();
	}

	static void ViewDisassembler ()
	{
		EnsureSourceView ().Language = Languages.IL;
	}

	static void ViewDecompiler ()
	{
		EnsureSourceView ().Language = Languages.CSharp;
	}

	static void ViewCollapseAllNodes ()
	{
		assemblies.CollapseAll ();
		assemblies.SetFocus ();
	}

	static readonly Dictionary<PEFile, TabView.Tab> metadata_tables_tabs = new ();

	static TabView.Tab GetMetadataTab (PEFile pe)
	{
		if (metadata_tables_tabs.TryGetValue (pe, out var tab))
			return tab;

		ListView listview = new () {
			X = 0,
			Y = 0,
			Width = MetadataDataSource.Shared.Length + 1,
			Height = Dim.Fill (),
			AllowsMultipleSelection = false,
			CanFocus = true,
			Source = MetadataDataSource.Shared,
		};

		TableView table = new () {
			X = Pos.Right (listview),
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			CanFocus = true,
			FullRowSelect = true,
		};
		table.CellActivated += (e) => {
			switch (e.Table.ExtendedProperties ["Metadata"]) {
			case MetadataTables.AssemblyRef:
				if (e.Table.ExtendedProperties ["PE"] is not PEFile pe)
					break;
				var handle = MetadataTokens.AssemblyReferenceHandle ((int) e.Table.Rows [e.Row] [0]);
				AssemblyReference ar = new (pe.Metadata, handle);
				var a = AssemblyResolver.Resolver.Resolve (ar);
				if (a is not null) {
					var an = assemblies.Select ((n) => a.Equals (n.Tag) && (n is AssemblyNode));
					if (an is null) {
						an = assemblies.Add (a);
						assemblies.SelectedObject = an;
						assemblies.SetFocus ();
					}
				}
				EnsureSourceView ().Show (a);
				break;
			}
		};

		View v = new () {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			CanFocus = false,
		};
		v.Add (listview, table);

		listview.OpenSelectedItem += (args) => {
			var table_name = listview.Source.ToList () [listview.SelectedItem] as string;
			table.Table = MetadataTables.GetTable (table_name!, pe);
			table.SetFocus ();
		};

		tab = new TabView.Tab ($"{pe.Name} Metadata", v);
		tabs.AddTab (tab, true);
		metadata_tables_tabs.Add (pe, tab);
		return tab;
	}

	static void ViewMetadataTables ()
	{
		// menu is disabled (same condition) but this gets called anyway if the (menu) shortcut is used
		if (assemblies.SelectedObject.Tag is not PEFile pe)
			return;

		var tab = GetMetadataTab (pe);
		tabs.SelectedTab = tab;
		tab.View.SetFocus ();
	}

	static void ViewPreferences ()
	{
		MessageBox.Query ("Preferences", "TODO", "_Ok");
	}

	static void HelpKeyBindings ()
	{
		ExecSupport.Run ("open https://github.com/spouliot/cilurbo/wiki/KeyBindings");
	}

	static void HelpAbout ()
	{
		StringBuilder aboutMessage = new ();
		aboutMessage.AppendLine ("A simple, text-user interface over .net assemblies");
		aboutMessage.AppendLine (@"_________ .__.__              ___.           ");
		aboutMessage.AppendLine (@"\_   ___ \|__|  |  __ ________\_ |__   ____  ");
		aboutMessage.AppendLine (@"/    \  \/|  |  | |  |  \_  __ \ __ \ /  _ \ ");
		aboutMessage.AppendLine (@"\     \___|  |  |_|  |  /|  | \/ \_\ (  <_> )");
		aboutMessage.AppendLine (@" \______  /__|____/____/ |__|  |___  /\____/ ");
		aboutMessage.AppendLine (@"        \/                         \/        ");
		aboutMessage.AppendLine ("");
		aboutMessage.AppendLine ($"Version: {typeof (Program).Assembly.GetName ().Version}");
		aboutMessage.AppendLine ($"Using ICSharpCode.Decompiler {FileVersionInfo.GetVersionInfo (typeof (PEFile).Assembly.Location).ProductVersion}");
		aboutMessage.AppendLine ($"and Terminal.Gui {FileVersionInfo.GetVersionInfo (typeof (Application).Assembly.Location).ProductVersion}");
		aboutMessage.AppendLine ("");
		MessageBox.Query ("About Cilurbo", aboutMessage.ToString (), "_Ok");
	}

	// copied from terminal.gui samples
	static void SetupScrollBar ()
	{
		// When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
		assemblies.Style.LeaveLastRow = true;

		ScrollBarView _scrollBar = new (assemblies, true);
		_scrollBar.ShowScrollIndicator = true;

		_scrollBar.ChangedPosition += () => {
			assemblies.ScrollOffsetVertical = _scrollBar.Position;
			if (assemblies.ScrollOffsetVertical != _scrollBar.Position) {
				_scrollBar.Position = assemblies.ScrollOffsetVertical;
			}
			assemblies.SetNeedsDisplay ();
		};

		_scrollBar.OtherScrollBarView.ChangedPosition += () => {
			assemblies.ScrollOffsetHorizontal = _scrollBar.OtherScrollBarView.Position;
			if (assemblies.ScrollOffsetHorizontal != _scrollBar.OtherScrollBarView.Position) {
				_scrollBar.OtherScrollBarView.Position = assemblies.ScrollOffsetHorizontal;
			}
			assemblies.SetNeedsDisplay ();
		};

		assemblies.DrawContent += (e) => {
			_scrollBar.Size = assemblies.ContentHeight;
			_scrollBar.Position = assemblies.ScrollOffsetVertical;
			_scrollBar.OtherScrollBarView.Size = assemblies.GetContentWidth (true);
			_scrollBar.OtherScrollBarView.Position = assemblies.ScrollOffsetHorizontal;
			_scrollBar.Refresh ();
		};
	}

	static readonly TabView.Tab source_tab = new ("", new SourceView ());

	static SourceView EnsureSourceView ()
	{
		if (tabs.Tabs.Contains (source_tab)) {
			tabs.SelectedTab = source_tab;
			return (source_tab.View as SourceView)!;
		}

		tabs.AddTab (source_tab, true);
		tabs.SelectedTab = source_tab;

		return (source_tab.View as SourceView)!;
	}
}
