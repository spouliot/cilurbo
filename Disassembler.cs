using System.Reflection.Metadata;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

static class Disassembler {

	static public string Disassemble (this PEFile file)
	{
		using var writer = new StringWriter ();
		var output = new PlainTextOutput (writer);
		ReflectionDisassembler rd = new (output, CancellationToken.None);
		rd.WriteAssemblyHeader (file);
		output.WriteLine ();
		rd.WriteModuleHeader (file);
		return writer.ToString ();
	}
	
	static public string Disassemble (this IEntity entity)
	{
		using var writer = new StringWriter ();
		var output = new PlainTextOutput (writer);
		ReflectionDisassembler rd = new (output, CancellationToken.None);
		var pe = entity.ParentModule.PEFile!;
		switch (entity) {
		case ITypeDefinition type:
			rd.DisassembleType (pe, (TypeDefinitionHandle) type.MetadataToken);
			break;
		case IField field:
			rd.DisassembleField (pe, (FieldDefinitionHandle) field.MetadataToken);
			break;
		case IProperty property:
			rd.DisassembleProperty (pe, (PropertyDefinitionHandle) property.MetadataToken);
			break;
		case IEvent @event:
			rd.DisassembleEvent (pe, (EventDefinitionHandle) @event.MetadataToken);
			break;
		case IMethod method:
			rd.DisassembleMethod (pe, (MethodDefinitionHandle) method.MetadataToken);
			break;
		}
		return writer.ToString ();
	}
}
