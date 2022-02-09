using System.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;

namespace Cilurbo.MetadataTables;

class MetadataTable {

	protected static string GetBlob (MetadataReader metadata, BlobHandle blob)
	{
		StringBuilder sb = new ();
		// sb.Append ("[blob: ").Append (metadata.GetHeapOffset (blob)).Append ("] ");
		if (blob.IsNil) {
			sb.Append ("nil");
		} else {
			foreach (var b in metadata.GetBlobContent (blob)) {
				sb.Append (b.ToString ("x2"));
			}
		}
		return sb.ToString ();
	}

	protected static string GetEnum (object @enum)
	{
		StringBuilder sb = new ("0x");
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

	protected static string GetString (MetadataReader metadata, StringHandle handle, bool prependOffset = true)
	{
		if (!prependOffset)
			return handle.IsNil ? "0x00000000 nil" : metadata.GetString (handle);

		StringBuilder sb = new ();
		if (prependOffset) {
			sb.Append ("0x");
			if (handle.IsNil) {
				sb.Append ("00000000 nil");
			} else {
				sb.Append (metadata.GetHeapOffset (handle).ToString ("x8"));
				sb.Append (' ');
				sb.Append (metadata.GetString (handle));
			}
		}
		return sb.ToString ();
	}

	//   						   1234567890123456
	public const string TypeRef = "0x01 TypeRef";
	public const string MethodDef = "0x06 MethodDef";
	public const string MemberRef = "0x0A MemberRef";
	public const string Constant = "0x0B Constant";
	public const string ModuleRef = "0x1A ModuleRef";
	public const string TypeSpec = "0x1B TypeSpec";
	public const string AssemblyRef = "0x23 AssemblyRef";
	public const string File = "0x26 File";
	public const string ExportedType = "0x27 ExportedType";

	public static DataTable CreateTable (PEFile module, string tableName)
	{
		DataTable dt = new ();
		dt.ExtendedProperties.Add ("PE", module);
		dt.ExtendedProperties.Add ("Metadata", tableName);
		dt.Columns.Add (new DataColumn ("RID", typeof (int)));
		return dt;
	}

	// 0x01 TypeRef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.38-typeref-0x01.md
	public static DataTable GetTypeRefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, TypeRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("ResolutionScope", typeof (string)));
		dt.Columns.Add (new DataColumn ("TypeName", typeof (string)));
		dt.Columns.Add (new DataColumn ("TypeNamespace", typeof (string)));

		StringBuilder rst_value = new ();
		var m = module.Metadata;
		foreach (var row in m.TypeReferences) {
			var tref = m.GetTypeReference (row);
			var rst = MetadataTokens.GetToken (tref.ResolutionScope);
			rst_value.Append ("0x").Append (rst.ToString ("x8"));
			if ((rst & 0x23000000) == 0x23000000) {
				var aref = m.GetAssemblyReference ((AssemblyReferenceHandle) tref.ResolutionScope);
				rst_value.Append (' ').Append (GetString (m, aref.Name, prependOffset: false));
			} else if ((rst & 0x01000000) == 0x01000000) {
				var ntref = m.GetTypeReference ((TypeReferenceHandle) tref.ResolutionScope);
				rst_value.Append (' ').Append (GetString (m, ntref.Name, prependOffset: false));
			} else {
				// TODO expand resolution scope - it can be many things
				System.Diagnostics.Debugger.Break ();
			}
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				rst_value.ToString (),
				GetString (m, tref.Name),
				GetString (m, tref.Namespace),
			});
			rst_value.Clear ();
		}
		return dt;
	}

	// 0x06 MethodDef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.26-methoddef-0x06.md
	public static DataTable GetMethodDefTable (PEFile module)
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
	public static DataTable GetMemberRefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, MemberRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Class", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));
		dt.Columns.Add (new DataColumn ("Signature", typeof (string)));

		var m = module.Metadata;
		StringBuilder builder = new ();
		ITextOutput output = new StringBuilderTextOutput (builder);
		foreach (var row in m.MemberReferences) {
			var mref = m.GetMemberReference (row);
			var klass = MetadataTokens.GetToken (mref.Parent);
			builder.Append ("0x").Append (klass.ToString ("x8")).Append (' ');
			// MethodDef, ModuleRef, TypeDef, TypeRef, or TypeSpec
			if ((klass & 0x1b000000) == 0x1b000000) {
				var kspec = m.GetTypeSpecification ((TypeSpecificationHandle) mref.Parent);
				kspec.DecodeSignature (new DisassemblerSignatureTypeProvider (module, output), GenericContext.Empty) (ILNameSyntax.TypeName);
			} else if ((klass & 0x01000000) == 0x01000000) {
				var tref = m.GetTypeReference ((TypeReferenceHandle) mref.Parent);
				builder.Append (GetString (m, tref.Name, prependOffset: false));
			} else {
				// TODO expand resolution scope - it can be many things
				System.Diagnostics.Debugger.Break ();
			}
			var klass_string = builder.ToString ();
			builder.Clear ();

			builder.Append ("0x").Append (m.GetHeapOffset (mref.Signature).ToString ("x8")).Append (' ');
			GenericContext context = new (default (TypeDefinitionHandle), module);
			((EntityHandle) row).WriteTo (module, output, context, ILNameSyntax.TypeName);

			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				klass_string,
				GetString (m, mref.Name),
				builder.ToString (),
			});
			builder.Clear ();
		}
		return dt;
	}

	// 0x0B Constant
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.9-constant-0x0b.md
	public static DataTable GetConstantTable (PEFile module)
	{
		DataTable dt = CreateTable (module, Constant);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Type", typeof (string)));
		dt.Columns.Add (new DataColumn ("Parent", typeof (string)));
		dt.Columns.Add (new DataColumn ("Value", typeof (string)));

		var m = module.Metadata;
		for (var row = 1; row <= m.GetTableRowCount (TableIndex.Constant); row++) {
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

	// 0x1A ModuleRef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.31-moduleref-0x1a.md
	public static DataTable GetModuleRefTable (PEFile module)
	{
		DataTable dt = CreateTable (module, ModuleRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));

		var m = module.Metadata;
		for (var row = 1; row <= m.GetTableRowCount (TableIndex.ModuleRef); row++) {
			var handle = MetadataTokens.ModuleReferenceHandle (row);
			var mref = m.GetModuleReference (handle);
			dt.Rows.Add (new object [] {
				row,
				MetadataTokens.GetToken (handle).ToString ("x8"),
				GetString (m, mref.Name),
			});
		}
		return dt;
	}

	// 0x1B TypeSpec
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.39-typespec-0x1b.md
	public static DataTable GetTypeSpecTable (PEFile module)
	{
		DataTable dt = CreateTable (module, ModuleRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Signature", typeof (string)));

		StringBuilder signature = new ();
		ITextOutput output = new StringBuilderTextOutput (signature);
		var m = module.Metadata;
		for (var row = 1; row <= m.GetTableRowCount (TableIndex.TypeSpec); row++) {
			var handle = MetadataTokens.TypeSpecificationHandle (row);
			var tspec = m.GetTypeSpecification (handle);
			signature.Append (MetadataTokens.GetToken (handle).ToString ("x8")).Append (' ');
			tspec.DecodeSignature (new DisassemblerSignatureTypeProvider (module, output), GenericContext.Empty) (ILNameSyntax.TypeName);
			dt.Rows.Add (new object [] {
				row,
				MetadataTokens.GetToken (handle).ToString ("x8"),
				signature.ToString (),
			});
			signature.Clear ();
		}
		return dt;
	}

	// 0x23 AssemblyRef
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.5-assemblyref-0x23.md
	public static DataTable GetAssemblyRefTable (PEFile module)
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

	public static void ActivateAssemblyRefTable (TableView.CellActivatedEventArgs args)
	{
		if (args.Table.ExtendedProperties ["PE"] is not PEFile pe)
			return;

		var handle = MetadataTokens.AssemblyReferenceHandle ((int) args.Table.Rows [args.Row] [0]);
		ICSharpCode.Decompiler.Metadata.AssemblyReference ar = new (pe.Metadata, handle);
		var a = AssemblyResolver.Resolver.Resolve (ar);
		if (a is not null) {
			var an = Program.Select ((n) => (n is AssemblyNode an) && (an.PEFile.FileName == a.FileName));
			if (an is null) {
				an = Program.Add (a, selectAndGoto: true);
			}
		}
		Program.EnsureSourceView ().Show (a);
	}

	// 0x26 File
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.19-file-0x26.md
	public static DataTable GetFileTable (PEFile module)
	{
		DataTable dt = CreateTable (module, AssemblyRef);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Flags", typeof (string)));
		dt.Columns.Add (new DataColumn ("Name", typeof (string)));
		dt.Columns.Add (new DataColumn ("HashValue", typeof (string)));

		var m = module.Metadata;
		foreach (var row in m.AssemblyFiles) {
			var f = m.GetAssemblyFile (row);
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				f.ContainsMetadata ? "0x0000 ContainsMetaData" : "0x0001 ContainsNoMetaData",
				GetString (m, f.Name),
				GetBlob (m, f.HashValue),
			});
		}
		return dt;
	}

	// 0x27 ExportedType
	// https://github.com/stakx/ecma-335/blob/master/docs/ii.22.14-exportedtype-0x27.md
	public static DataTable GetExportedTypeTable (PEFile module)
	{
		DataTable dt = CreateTable (module, ExportedType);
		dt.Columns.Add (new DataColumn ("Token", typeof (string)));
		dt.Columns.Add (new DataColumn ("Flags", typeof (string)));
		dt.Columns.Add (new DataColumn ("TypeDefId", typeof (string)));
		dt.Columns.Add (new DataColumn ("TypeName", typeof (string)));
		dt.Columns.Add (new DataColumn ("TypeNamespace", typeof (string)));
		dt.Columns.Add (new DataColumn ("Implementation", typeof (string)));

		var m = module.Metadata;
		foreach (var row in m.ExportedTypes) {
			var et = m.GetExportedType (row);
			string flags_string;
			// not in the enum :( but seems to be always used alone
			if (et.IsForwarder)
				flags_string = "0x00200000 (TypeForwarder)";
			else
				flags_string = GetEnum (et.Attributes);
			dt.Rows.Add (new object [] {
				MetadataTokens.GetRowNumber (row),
				MetadataTokens.GetToken (row).ToString ("x8"),
				flags_string,
				et.GetTypeDefinitionId ().ToString ("x8"),
				GetString (m, et.Name),
				GetString (m, et.Namespace),
				MetadataTokens.GetToken (et.Implementation).ToString ("x8"),
			});
		}
		return dt;
	}
}
