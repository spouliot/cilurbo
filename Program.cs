using System.Reflection;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;

[assembly: AssemblyVersion ("0.1.0.0")]
partial class Program {

	static readonly List<string> assemblies_file_extensions = new () { ".dll", ".exe" };
	static readonly List<string> lists_file_extensions = new () { ".list", ".lst" };

	static object? current_metadata;


	enum Languages {
		IL,
		CSharp,
	}

	static Languages Language = Languages.CSharp;

	static int Main (string [] args)
	{
		try {
			Application.Init ();
			var rv = SetupUI (Application.Top);
			if (rv == 0) {
				foreach (var arg in args) {
					Console.WriteLine ($"Loading {arg}");
					LoadFile (arg);
				}
			}
			Application.Run ();
			Application.Shutdown ();
			return rv;
		} catch (Exception e) {
			Console.WriteLine (e);
			return 1;
		}
	}

	static void LoadFile (string file)
	{
		if (!File.Exists (file)) {
			// TODO error logging
			return;
		}

		var ext = Path.GetExtension (file).ToLowerInvariant ();
		if (assemblies_file_extensions.Contains (ext)) {
			var pe = AssemblyResolver.Resolver.Load (file);
			if (pe is not null) {
				var node = assemblies.Add (pe);
				assemblies.SelectedObject = node;
			}
		} else if (lists_file_extensions.Contains (ext)) {
			foreach (var f in File.ReadLines (file)) {
				LoadFile (f);
			}
		}
	}

	static void ViewSource (Languages language)
	{
		Language = language;
		switch (language) {
		case Languages.CSharp:
			Decompiler (current_metadata);
			break;
		case Languages.IL:
			Disassembler (current_metadata);
			break;
		}
	}

	static void Disassembler (object? metadata)
	{
		TextView textview = EnsureSourceView ();
		switch (metadata) {
		case PEFile file:
			source_tab.Text = file.Name;
			textview.Text = file.Disassemble ();
			break;
		case IEntity entity:
			source_tab.Text = entity.Name;
			textview.Text = entity.Disassemble ();
			break;
		default:
			source_tab.Text = "-";
			textview.Text = "";
			break;
		}
	}

	static void Decompiler (object? metadata)
	{
		TextView textview = EnsureSourceView ();
		switch (metadata) {
		case PEFile file:
			source_tab.Text = file.Name;
			textview.Text = file.Decompile ();
			break;
		case IEntity entity:
			source_tab.Text = entity.Name;
			textview.Text = entity.Decompile ();
			break;
		default:
			source_tab.Text = "-";
			textview.Text = "";
			break;
		}
	}
}
