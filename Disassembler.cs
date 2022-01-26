using System.Reflection.Metadata;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace Cilurbo;

static class Disassembler {

	static public string Disassemble (this PEFile file)
	{
		using StringWriter writer = new ();
		PlainTextOutput output = new (writer);
		ReflectionDisassembler rd = new (output, CancellationToken.None);
		rd.WriteAssemblyHeader (file);
		output.WriteLine ();
		rd.WriteModuleHeader (file);
		return writer.ToString ();
	}

	static public string Disassemble (this IEntity entity)
	{
		using StringWriter writer = new ();
		PlainTextOutput output = new (writer);
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
			if (property.Getter is not null) {
				output.WriteLine ();
				rd.DisassembleMethod (pe, (MethodDefinitionHandle) property.Getter.MetadataToken);
			}
			if (property.Setter is not null) {
				output.WriteLine ();
				rd.DisassembleMethod (pe, (MethodDefinitionHandle) property.Setter.MetadataToken);
			}
			break;
		case IEvent @event:
			rd.DisassembleEvent (pe, (EventDefinitionHandle) @event.MetadataToken);
			if (@event.AddAccessor is not null) {
				output.WriteLine ();
				rd.DisassembleMethod (pe, (MethodDefinitionHandle) @event.AddAccessor.MetadataToken);
			}
			if (@event.RemoveAccessor is not null) {
				output.WriteLine ();
				rd.DisassembleMethod (pe, (MethodDefinitionHandle) @event.RemoveAccessor.MetadataToken);
			}
			if (@event.InvokeAccessor is not null) {
				output.WriteLine ();
				rd.DisassembleMethod (pe, (MethodDefinitionHandle) @event.InvokeAccessor.MetadataToken);
			}
			break;
		case IMethod method:
			rd.DisassembleMethod (pe, (MethodDefinitionHandle) method.MetadataToken);
			break;
		}
		return writer.ToString ();
	}
}
