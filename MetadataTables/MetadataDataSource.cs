using System.Collections;

using Terminal.Gui;

namespace Cilurbo.MetadataTables;

class MetadataDataSource : IListDataSource {

	public static readonly MetadataDataSource Shared = new ();

	static readonly List<string> tables = new () {
		MetadataTable.TypeRef,
		MetadataTable.MethodDef,
		MetadataTable.MemberRef,
		MetadataTable.Constant,
		MetadataTable.ModuleRef,
		MetadataTable.TypeSpec,
		MetadataTable.AssemblyRef,
		MetadataTable.File,
	};

	public int Count => tables.Count;

	public int Length => 16;

	public bool IsMarked (int item) => false;

	public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
	{
		driver.AddStr (tables [item]);
	}

	public void SetMark (int item, bool value)
	{
	}

	public IList ToList () => tables;
}
