using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Cilurbo.MetadataTables;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;

namespace Cilurbo.Analyzers.ModuleReferences;

// List all the methods that expose functions from the module reference
[Analyzer ("[r] PInvoke Finder")]
public class PInvokeFinder : IAnalyzer {

	public PInvokeFinder ()
	{
	}

	public int ExternallySearchableColumn => 2;

	public bool IsApplicable (MetadataNode? node)
	{
		return node is ModuleReferenceNode;
	}

	public void SetTable (AnalyzerView view, MetadataNode node)
	{
		DataTable dt = new ();

		dt.Columns.Add (new DataColumn ("Result", typeof (int)));

		DataColumn token_col = new ("Token", typeof (int));
		dt.Columns.Add (token_col);
		view.Style.ColumnStyles.Add (token_col, ColumnStyles.IntHex8);

		dt.Columns.Add (new DataColumn ("Symbol", typeof (string))); // externally searchable column
		dt.Columns.Add (new DataColumn ("P/invoke Method", typeof (string)));

		view.Table = dt;

		if (node is not ModuleReferenceNode mrn)
			return;
		if (mrn.Tag is not ModuleReference mr)
			return;
		if (mrn.Parent is not AssemblyNode an)
			return;
		if (an.Tag is not PEFile pe)
			return;

		dt.ExtendedProperties.Add ("PE", pe);
		dt.ExtendedProperties.Add ("Metadata", MetadataTable.ModuleRef);
		dt.ExtendedProperties.Add ("Analyzer", this);

		int result = 1;
		StringBuilder method_name = new ();
		foreach (var type in an.TypeSystem.GetTopLevelTypeDefinitions ().OrderBy (t => t.Name)) {
			if (type.ParentModule.Name != pe.Name)
				continue;
			foreach (var m in type.Methods.OrderBy (m => m.Name)) {
				if (m.HasBody)
					continue;
				if (m.MetadataToken.IsNil)
					continue;
				MethodDefinitionHandle mh = (MethodDefinitionHandle) m.MetadataToken;
				var md = pe.Metadata.GetMethodDefinition (mh);
				if (!md.Attributes.HasFlag (System.Reflection.MethodAttributes.PinvokeImpl))
					continue;
				var mi = md.GetImport ();
				if (pe.Metadata.GetModuleReference (mi.Module).Name != mr.Name)
					continue;

				method_name.AppendType (type).Append ('.').AppendMethod (m);
				dt.Rows.Add (new object [] {
					result++,
					MetadataTokens.GetToken (mh),
					pe.Metadata.GetString (mi.Name),
					method_name.ToString (),
				});
				method_name.Clear ();
			}
		}
	}

	public void OnActivation (TableView.CellActivatedEventArgs args)
	{
		var table = args.Table;
		if (table.ExtendedProperties ["PE"] is PEFile pe) {
			var rid = MetadataTokens.EntityHandle (TableIndex.MethodDef, (int) table.Rows [args.Row] [1]);
			Program.Select ((n) => {
				if (n.Tag is IMember m) {
					if (m.ParentModule.PEFile?.FileName != pe.FileName)
						return false;
					return m.MetadataToken.Equals (rid);
				}
				return false;
			});
		}
	}
}
