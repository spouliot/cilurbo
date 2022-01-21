using System.Text;

using ICSharpCode.Decompiler.TypeSystem;

static class Formatters {

	static readonly Dictionary<string, string> wellKnownTypeNames = new ();

	static Formatters ()
	{
		// TODO: this needs to be different for IL and C#
		wellKnownTypeNames.Add ("System.Void", "void");
		wellKnownTypeNames.Add ("System.Boolean", "bool");
		wellKnownTypeNames.Add ("System.Byte", "byte");
		wellKnownTypeNames.Add ("System.SByte", "sbyte");
		wellKnownTypeNames.Add ("System.Char", "char");
		wellKnownTypeNames.Add ("System.Decimal", "decimal");
		wellKnownTypeNames.Add ("System.Double", "double");
		wellKnownTypeNames.Add ("System.Single", "float");
		wellKnownTypeNames.Add ("System.Int32", "int");
		wellKnownTypeNames.Add ("System.UInt32", "uint");
		wellKnownTypeNames.Add ("System.Int64", "long");
		wellKnownTypeNames.Add ("System.UInt64", "ulong");
		wellKnownTypeNames.Add ("System.Object", "object");
		wellKnownTypeNames.Add ("System.Int16", "short");
		wellKnownTypeNames.Add ("System.UInt16", "ushort");
		wellKnownTypeNames.Add ("System.String", "string");
		wellKnownTypeNames.Add ("System.IntPtr", "native int");
		wellKnownTypeNames.Add ("System.UIntPtr", "native uint");
	}

	static public StringBuilder AppendType (this StringBuilder self, IType type)
	{
		string name_suffix = "";
		switch (type.Kind) {
		case TypeKind.NInt:
			return self.Append ("nint");
		case TypeKind.NUInt:
			return self.Append ("nuint");
		case TypeKind.Array:
			name_suffix = "[]";
			break;
		case TypeKind.Pointer:
			name_suffix = "*";
			break;
		case TypeKind.ByReference:
			name_suffix = "&";
			break;
		case TypeKind.Class:
		case TypeKind.Delegate:
		case TypeKind.Enum:
		case TypeKind.FunctionPointer:
		case TypeKind.Interface:
		case TypeKind.Struct:
		case TypeKind.Tuple:
		case TypeKind.TypeParameter:
		case TypeKind.Unknown:
		case TypeKind.Void:
			break;
		default:
			break;
		}
		var name = type.FullName!;
		if (name_suffix.Length > 0)
			name = name [0..^name_suffix.Length];
		if (wellKnownTypeNames.TryGetValue (name, out name)) {
			self.Append (name);
			if (name_suffix.Length > 0)
				self.Append (name_suffix);
			return self;
		}
		return self.Append (type.Name);
	}

	static public StringBuilder AppendMethod (this StringBuilder self, IMethod method)
	{
		self.Append (method.Name);
		self.Append ('(');
		if (method.Parameters.Count > 0) {
			foreach (var p in method.Parameters) {
				self.AppendType (p.Type).Append (',');
			}
			self.Length--;
		}
		return self.Append (") : ").AppendType (method.ReturnType);
	}
}
