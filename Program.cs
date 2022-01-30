using Terminal.Gui;

namespace Cilurbo;

partial class Program {

	static readonly List<string> assemblies_file_extensions = new () { ".dll", ".exe" };
	static readonly List<string> lists_file_extensions = new () { ".list", ".lst" };

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
				var node = metadata_tree.Add (pe);
				metadata_tree.SelectedObject = node;
			}
		} else if (lists_file_extensions.Contains (ext)) {
			foreach (var f in File.ReadLines (file)) {
				LoadFile (f);
			}
		}
	}
}
