using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

static class Decompiler {

	static readonly Dictionary<string, CSharpDecompiler> decompilers = new ();

	static CSharpDecompiler GetDecompiler (PEFile file)
	{
		if (!decompilers.TryGetValue (file.FileName, out var decompiler)) {
			decompiler = new CSharpDecompiler (file.FileName, new DecompilerSettings {
				ThrowOnAssemblyResolveErrors = false,
			});
			decompilers.Add (file.FileName, decompiler);
		}
		return decompiler;
	}

	static public string Decompile (this PEFile pe)
	{
		var d = GetDecompiler (pe);
		return d.DecompileModuleAndAssemblyAttributesToString ();
	}

	static public string Decompile (this IEntity entity)
	{
		var d = GetDecompiler (entity.ParentModule.PEFile!);
		return d.DecompileAsString (entity.MetadataToken);
	}
}
