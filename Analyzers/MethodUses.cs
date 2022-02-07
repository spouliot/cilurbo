using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Cilurbo.MetadataTables;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;

namespace Cilurbo.Analyzers.MemberReferences;

// List all the methods, fields, types... that are used by the selected method
[Analyzer ("[CcMm] Method Uses")]
public class MethodUses : IAnalyzer {

	public MethodUses ()
	{
	}

	public int ExternallySearchableColumn => 2;

	public bool IsApplicable (MetadataNode? node)
	{
		return node is MethodNode || node is ConstructorNode;
	}

	AssemblyNode GetAssemblyNode (MetadataNode node)
	{
		if (node is AssemblyNode an)
			return an;
		return GetAssemblyNode (node.Parent!);
	}

	public void SetTable (AnalyzerView view, MetadataNode node)
	{
		DataTable dt = new ();

		dt.Columns.Add (new DataColumn ("Result", typeof (int)));

		DataColumn token_col = new ("Token", typeof (int));
		dt.Columns.Add (token_col);
		view.Style.ColumnStyles.Add (token_col, ColumnStyles.IntHex8);

		dt.Columns.Add (new DataColumn ("Symbol", typeof (string))); // externally searchable column

		view.Table = dt;

		if (node is not MemberNode mn)
			return;
		if (mn.Tag is not IMember m)
			return;
		AssemblyNode an = GetAssemblyNode (node);
		if (an.Tag is not PEFile pe)
			return;

		dt.ExtendedProperties.Add ("PE", pe);
		MetadataModule mainModule = (MetadataModule) m.ParentModule;
		dt.ExtendedProperties.Add ("MetadataModule", mainModule);
		dt.ExtendedProperties.Add ("Metadata", MetadataTable.MethodDef);
		dt.ExtendedProperties.Add ("Analyzer", this);

		var md = pe.Metadata.GetMethodDefinition ((MethodDefinitionHandle) m.MetadataToken);
		var body = pe.Reader.GetMethodBody (md.RelativeVirtualAddress);
		var blob = body.GetILReader ();
		var result = 1;
		HashSet<IEntity> used = new ();
		while (blob.RemainingBytes > 0) {
			ILOpCode opCode = blob.DecodeOpCode ();
			switch (opCode) {
			case ILOpCode.Call:
			case ILOpCode.Callvirt:
			case ILOpCode.Ldfld:
			case ILOpCode.Ldflda:
			case ILOpCode.Ldftn:
			case ILOpCode.Ldsfld:
			case ILOpCode.Ldsflda:
			case ILOpCode.Ldtoken:
			case ILOpCode.Ldvirtftn:
			case ILOpCode.Newobj:
			case ILOpCode.Stfld:
			case ILOpCode.Stsfld:
				var member = MetadataTokenHelpers.EntityHandleOrNil (blob.ReadInt32 ());
				if (member.IsNil)
					continue;
				var operand = mainModule.ResolveEntity (member);
				if (operand is null)
					continue;
				if (used.Contains (operand))
					continue;
				used.Add (operand);
				dt.Rows.Add (new object [] {
					result++,
					MetadataTokens.GetToken (member),
					operand.FullName,
				});
				break;
			default:
				ILParser.SkipOperand (ref blob, opCode);
				break;
			}
		}
	}

	public void OnActivation (TableView.CellActivatedEventArgs args)
	{
		var table = args.Table;
		if (table.ExtendedProperties ["MetadataModule"] is MetadataModule mainModule) {
			var rid = MetadataTokens.EntityHandle ((int) table.Rows [args.Row] [1]);
			if (mainModule.ResolveEntity (rid) is not IEntity t)
				return;
			// getter/setters should look for their property, same for events...
			if ((t is IMethod m) && (m.AccessorOwner is not null))
				t = m.AccessorOwner;
			Program.Select ((n) => {
				if (n.Tag is IEntity e) {
					if (e.ParentModule.PEFile?.FileName != t.ParentModule.PEFile?.FileName)
						return false;
					if (e.MetadataToken.Equals (t.MetadataToken))
						return true;
				}
				return false;
			});
		}
	}
}
