using System.Collections;
using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ICSharpCode.Decompiler.Metadata;

using Terminal.Gui;

namespace Cilurbo;

class MetadataDataSource : IListDataSource {

	public static readonly MetadataDataSource Shared = new ();

	static readonly List<string> tables = new () {
		MetadataTables.MethodDef,
		MetadataTables.MemberRef,
		MetadataTables.Constant,
		MetadataTables.AssemblyRef,
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

class MetadataTables {

	protected static string GetBlob (MetadataReader metadata, BlobHandle blob)
	{
		StringBuilder sb = new ();
		sb.Append ("[blob: ").Append (metadata.GetHeapOffset (blob)).Append ("] ");
		if (blob.IsNil)
			sb.Append ("nil");
		else {
			foreach (var b in metadata.GetBlobContent (blob)) {
				sb.Append (b.ToString ("x2"));
			}
		}
		return sb.ToString ();
	}

	protected static string GetEnum (object @enum)
	{
		StringBuilder sb = new ();
		switch (Type.GetTypeCode (@enum.GetType ().GetEnumUnderlyingType ())) {
		case TypeCode.Byte:
		case TypeCode.SByte:
			sb.Append (((byte) @enum).ToString ("x2"));
			break;
		case TypeCode.Int16:
		case TypeCode.UInt16:
			sb.Append (((short) @enum).ToString ("x4"));
			break;
		case TypeCode.Int32:
		case TypeCode.UInt32:
			sb.Append (((int) @enum).ToString ("x8"));
			break;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			sb.Append (((long) @enum).ToString ("x16"));
			break;
		}
		var s = @enum.ToString ();
		if (!String.IsNullOrEmpty (s))
			sb.Append (" (").Append (s).Append (')');
		return sb.ToString ();
	}

	protected static string GetString (MetadataReader metadata, StringHandle handle)
	{
		StringBuilder sb = new ();
		sb.Append ("[heap: ").Append (MetadataTokens.GetHeapOffset (handle)).Append ("] ");
		sb.Append (metadata.GetString (handle));
		return sb.ToString ();
	}

	//   							   1234567890123456
	public const string MethodDef = "0x06 MethodDef";
	public const string MemberRef = "0x0A MemberRef";
	public const string Constant = "0x0B Constant";
	public const string AssemblyRef = "0x23 AssemblyRef";

	public static DataTable GetTable (string tableName, PEFile module)
	{
		return tableName switch {
			MethodDef => GetMethodDefTable (module),
			MemberRef => GetMemberRefTable (module),
			Constant => GetConstantTable (module),
			AssemblyRef => GetAssemblyRefTable (module),
			_ => throw new NotImplementedException (),
		};
	}

	static DataTable CreateTable (PEFile module, string tableName)
	{
		DataTable dt = new ();
		dt.ExtendedProperties.Add ("PE", module);
		dt.ExtendedProperties.Add ("Metadata", tableName);
		dt.Columns.Add (new DataColumn ("RID", typeof (int)));
		return dt;
	}

	// 0x23 AssemblyRef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.5-assemblyref-0x23.md
	static DataTable GetAssemblyRefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, AssemblyRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Version", typeof (string)));
		dt.Columns.Add (new DataColumn ("Flags", typeof (string)));
		dt.Columns.Add (new DataColumn ("PublicKeyOrToken", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));
		dt.Columns.Add (new DataColumn ("Culture", typeof (string)));
		dt.Columns.Add (new DataColumn ("HashValue", typeof (string)));

		var m = module.Metadata;
		foreach (var row in m.AssemblyReferences) {
			var aref = m.GetAssemblyReference (row);
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				"{" + aref.Version.ToString () + "}",
				GetEnum (aref.Flags),
				GetBlob (m, aref.PublicKeyOrToken),
				GetString (m, aref.Name),
				GetString (m, aref.Culture),
				GetBlob (m, aref.HashValue),
			});
		}
		return dt;
	}

	// 0x06 MethodDef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.26-methoddef-0x06.md
	static DataTable GetMethodDefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, MethodDef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("RVA", typeof (string)));
		dt.Columns.Add (new DataColumn ("ImplFlags", typeof (string)));
		dt.Columns.Add (new DataColumn ("Flags", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));
		dt.Columns.Add (new DataColumn ("Signature", typeof (string)));
		dt.Columns.Add (new DataColumn ("ParamList", typeof (string)));

		var m = module.Metadata;
		foreach (var row in m.MethodDefinitions) {
			var mdef = m.GetMethodDefinition (row);
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				mdef.RelativeVirtualAddress.ToString ("x8"),
				GetEnum (mdef.ImplAttributes),
				GetEnum (mdef.Attributes),
				GetString (m, mdef.Name),
				GetBlob (m, mdef.Signature),
				// params list
			});
		}
		return dt;
	}

	// 0x0A MemberRef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.25-memberref-0x0a.md
	static DataTable GetMemberRefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, MemberRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Class", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));
		dt.Columns.Add (new DataColumn ("Signature", typeof (string)));

		var m = module.Metadata;
		foreach (var row in m.MemberReferences) {
			var mref = m.GetMemberReference (row);
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				MetadataTokens.GetToken (mref.Parent).ToString ("x8"),
				GetString (m, mref.Name),
				GetBlob (m, mref.Signature),
			});
		}
		return dt;
	}

	// 0x0B Constant
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.9-constant-0x0b.md
	static DataTable GetConstantTable (PEFile module)
	{
		DataTable dt = CreateTable (module, Constant);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Type", typeof (string)));
		dt.Columns.Add (new DataColumn ("Parent", typeof (string)));
		dt.Columns.Add (new DataColumn ("Value", typeof (string)));

		var m = module.Metadata;
		for (var row = 1; row < m.GetTableRowCount (TableIndex.Constant); row++) {
			var handle = MetadataTokens.ConstantHandle (row);
			var c = m.GetConstant (handle);
			dt.Rows.Add (new object [] {
				row,
				MetadataTokens.GetToken (handle).ToString ("x8"),
				GetEnum (c.TypeCode),
				MetadataTokens.GetToken (c.Parent).ToString ("x8"),
				GetBlob (m, c.Value),
			});
		}
		return dt;
	}
}
