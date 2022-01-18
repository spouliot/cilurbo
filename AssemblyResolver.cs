using ICSharpCode.Decompiler.Metadata;

class AssemblyResolver : IAssemblyResolver {

	static public readonly AssemblyResolver Resolver = new ();

	static readonly Dictionary<string, PEFile> assemblies_map = new ();
	static readonly List<string> assemblies_file_extensions = new () { ".dll", ".exe" };
	readonly List<string> directories = new ();

	public void AddSearchDirectory (string? directory)
	{
		// some `Directory.*` API returns null
		if ((directory is not null) && !directories.Contains (directory))
			directories.Add (directory);
	}

	public PEFile? Load (string file)
	{
		if (!File.Exists (file)) {
			// TODO error logging
			return null;
		}

		try {
			PEFile pe = new (file);
			// TODO error checking
			if (assemblies_map.TryAdd (pe.Name, pe))
				AddSearchDirectory (Path.GetDirectoryName (file));
			return pe;
		}
		catch (Exception) {
			return null;
		}
	}

	public PEFile? Resolve (IAssemblyReference reference)
	{
		var name = reference.Name;
		if (assemblies_map.TryGetValue (name, out var assembly))
			return assembly;

		foreach (var dir in directories) {
			foreach (var ext in assemblies_file_extensions) {
				var file = Path.Combine (dir, reference.Name + ext);
				return Load (file);
			}
		}
		return null;
	}

	public Task<PEFile?> ResolveAsync (IAssemblyReference reference)
	{
		TaskCompletionSource<PEFile?> tcs = new ();
		tcs.SetResult (Resolve (reference));
		return tcs.Task;
	}

	public PEFile? ResolveModule (PEFile mainModule, string moduleName)
	{
		throw new NotImplementedException();
	}

	public Task<PEFile?> ResolveModuleAsync (PEFile mainModule, string moduleName)
	{
		throw new NotImplementedException();
	}
}
