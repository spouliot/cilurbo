using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Cilurbo.MetadataTables;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

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

	public DataTable GetTable (MetadataNode node)
	{
		DataTable dt = new ();
		if (node is not ModuleReferenceNode mrn)
			return dt;
		if (mrn.Tag is not ModuleReference mr)
			return dt;
		if (mrn.Parent is not AssemblyNode an)
			return dt;
		if (an.Tag is not PEFile pe)
			return dt;

		dt.ExtendedProperties.Add ("PE", pe);
		dt.ExtendedProperties.Add ("Metadata", MetadataTable.ModuleRef);
		dt.ExtendedProperties.Add ("Analyzer", this);
		dt.Columns.Add (new DataColumn ("Result", typeof (int)));
		dt.Columns.Add (new DataColumn ("RID", typeof (int)));
		dt.Columns.Add (new DataColumn ("Symbol", typeof (string))); // externally searchable column
		dt.Columns.Add (new DataColumn ("P/invoke Method", typeof (string)));

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
					MetadataTokens.GetRowNumber (mh),
					pe.Metadata.GetString (mi.Name),
					method_name.ToString (),
				});
				method_name.Clear ();
			}
		}

		return dt;
	}
}
