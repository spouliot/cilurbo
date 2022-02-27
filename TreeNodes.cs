using System.Reflection.Metadata;
using System.Text;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui.Trees;

namespace Cilurbo;

public abstract class MetadataNode : ITreeNode {

	protected MetadataNode (MetadataNode? parent)
	{
		Parent = parent;
		Children = Array.Empty<ITreeNode> ();
		Tag = Text = String.Empty;
	}

	public MetadataNode? Parent { get; }

	public string Text { get; set; }

	public IList<ITreeNode> Children { get; set; }

	public object Tag { get; set; }
}

public class AssemblyNode : MetadataNode {
	public AssemblyNode (PEFile file) : base (null)
	{
		Tag = file;
		var metadata = file.Metadata;
		var ad = metadata.GetAssemblyDefinition ();
		var aname = metadata.GetString (ad.Name);
		StringBuilder sb = new ("[A] ");
		sb.Append (aname).Append (" (").Append (ad.Version).Append (')');
		Text = sb.ToString ();

		TypeSystem = new (file, AssemblyResolver.Resolver, TypeSystemOptions.Default | TypeSystemOptions.Uncached);

		List<ITreeNode> nodes = new ();
		SortedList<string, MetadataNode> references = new ();
		foreach (var ar in file.AssemblyReferences) {
			references.Add (ar.Name, new AssemblyReferenceNode (ar, this));
		}
		foreach (var node in references.Values) {
			nodes.Add (node);
		}
		references.Clear ();

		foreach (var mrh in metadata.GetModuleReferences ()) {
			var mr = metadata.GetModuleReference (mrh);
			var name = metadata.GetString (mr.Name);
			references.Add (name, new ModuleReferenceNode (name, mr, this));
		}
		foreach (var node in references.Values) {
			nodes.Add (node);
		}
		references.Clear ();

		foreach (var res in file.Resources) {
			references.Add (res.Name, new ResourceNode (res.Name, res, this));
		}
		foreach (var node in references.Values) {
			nodes.Add (node);
		}
		references.Clear ();

		List<string> list = new ();
		foreach (var type in TypeSystem.GetTopLevelTypeDefinitions ()) {
			if (type.ParentModule.Name != aname)
				continue;
			var ns = type.Namespace;
			if (!list.Contains (ns))
				list.Add (ns);
		}
		list.Sort ();
		foreach (var ns in list) {
			nodes.Add (new NamespaceNode (ns, aname, TypeSystem, this));
		}
		list.Clear ();

		Children = nodes;
	}

	public DecompilerTypeSystem TypeSystem { get; private set; }

	public PEFile PEFile => (Tag as PEFile)!;
}

public class AssemblyReferenceNode : MetadataNode {

	public AssemblyReferenceNode (ICSharpCode.Decompiler.Metadata.AssemblyReference ar, MetadataNode parent) : base (parent)
	{
		Tag = ar;
		Text = "[R] " + ar.Name;
	}
}

public class ModuleReferenceNode : MetadataNode {

	public ModuleReferenceNode (string name, ModuleReference mr, MetadataNode parent) : base (parent)
	{
		Tag = mr;
		Text = "[r] " + name;
	}
}

public class ResourceNode : MetadataNode {

	public ResourceNode (string name, Resource res, MetadataNode parent) : base (parent)
	{
		Tag = res;
		Text = "[s] " + name;
	}
}

public class NamespaceNode : MetadataNode {

	public NamespaceNode (string fullname, string parentName, IDecompilerTypeSystem typeSystem, MetadataNode parent) : base (parent)
	{
		Tag = fullname;
		if (fullname.Length == 0)
			Text = "[N] -";
		else
			Text = "[N] " + fullname;

		List<ITreeNode> types = new ();
		foreach (var type in typeSystem.GetTopLevelTypeDefinitions ().OrderBy (t => t.Name)) {
			if (type.ParentModule.Name != parentName)
				continue;
			if (type.Namespace == fullname)
				types.Add (new TypeNode (type, typeSystem, this));
		}
		Children = types;
	}
}

public class TypeNode : MetadataNode {

	public TypeNode (ITypeDefinition type, IDecompilerTypeSystem typeSystem, MetadataNode parent) : base (parent)
	{
		StringBuilder sb = new ("[T] ");
		sb.Append (type.Name);
		if (type.TypeParameterCount > 0) {
			sb.Append ('<');
			for (int i = 0; i < type.TypeParameterCount; i++) {
				var p = type.TypeParameters [i];
				if (i > 0)
					sb.Append (", ");
				switch (p.Variance) {
				case VarianceModifier.Covariant:
					sb.Append ("out ");
					break;
				case VarianceModifier.Contravariant:
					sb.Append ("in ");
					break;
				}
				sb.Append (p.Name);
			}
			sb.Append ('>');
		}
		Text = sb.ToString ();

		TypeDefinitionHandle handle = (TypeDefinitionHandle) type.MetadataToken;
		if (typeSystem.MainModule.ResolveEntity (handle) is not ITypeDefinition t)
			throw new InvalidOperationException ();
		Tag = TypeDefinition = t;

		List<ITreeNode> nodes = new ();

		foreach (var b in t.DirectBaseTypes.OrderBy (b => b.Name)) {
			// if (type.MetadataToken == b.MetadataToken)
			// 	continue;
			if (t.Kind != TypeKind.Interface || t.Kind == b.Kind) {
				var d = b.GetDefinition ();
				if (d is null)
					continue;
				nodes.Add (new BaseTypeNode (d, this));
			}
		}
		foreach (var n in t.NestedTypes.OrderBy (n => n.Name)) {
			nodes.Add (new TypeNode (n, typeSystem, this));
		}
		foreach (var f in t.Fields.OrderBy (f => f.Name)) {
			nodes.Add (new FieldNode (f, this));
		}
		foreach (var e in t.Events.OrderBy (e => e.Name)) {
			nodes.Add (new EventNode (e, this));
		}
		foreach (var p in t.Properties.OrderBy (p => p.Name)) {
			nodes.Add (new PropertyNode (p, this));
		}
		foreach (var m in t.Methods.OrderBy (m => m.Name)) {
			// e.g. some default .ctor on struct are present in the collection
			if (m.MetadataToken.IsNil)
				continue;
			if (m.IsConstructor)
				nodes.Add (new ConstructorNode (m, this));
			else
				nodes.Add (new MethodNode (m, this));
		}

		Children = nodes;
	}

	public ITypeDefinition TypeDefinition { get; private set; }
}

public class BaseTypeNode : MetadataNode {

	public BaseTypeNode (ITypeDefinition type, MetadataNode parent) : base (parent)
	{
		if (type.Kind == TypeKind.Interface)
			Text = "[i] " + type.Name;
		else
			Text = "[b] " + type.Name;
		Tag = type;
	}
}

public abstract class MemberNode : MetadataNode {

	protected abstract char InstanceCode { get; }
	protected abstract char StaticCode { get; }

	protected MemberNode (MetadataNode parent) : base (parent)
	{
	}

	protected MemberNode (IMember member, MetadataNode parent) : base (parent)
	{
		StringBuilder sb = new ("[");
		if (member.IsStatic)
			sb.Append (StaticCode);
		else
			sb.Append (InstanceCode);
		sb.Append ("] ").Append (member.Name);
		sb.Append (" : ").AppendType (member.ReturnType);
		Text = sb.ToString ();
		Tag = member;
	}
}

public class FieldNode : MemberNode {
	protected override char InstanceCode => 'f';

	protected override char StaticCode => 'F';

	public FieldNode (IField field, MetadataNode parent) : base (field, parent)
	{
	}
}

public class EventNode : MemberNode {
	protected override char InstanceCode => 'e';

	protected override char StaticCode => 'E';

	public EventNode (IEvent @event, MetadataNode parent) : base (@event, parent)
	{
		List<ITreeNode> nodes = new ();
		var a = @event.AddAccessor;
		if (a is not null)
			nodes.Add (new MethodNode (a, this));
		var r = @event.RemoveAccessor;
		if (r is not null)
			nodes.Add (new MethodNode (r, this));
		var i = @event.InvokeAccessor;
		if (i is not null)
			nodes.Add (new MethodNode (i, this));
		Children = nodes;
	}
}

public class PropertyNode : MemberNode {
	protected override char InstanceCode => 'p';

	protected override char StaticCode => 'P';

	public PropertyNode (IProperty property, MetadataNode parent) : base (property, parent)
	{
		List<ITreeNode> nodes = new ();
		var g = property.Getter;
		if (g is not null)
			nodes.Add (new MethodNode (g, this));
		var s = property.Setter;
		if (s is not null)
			nodes.Add (new MethodNode (s, this));
		Children = nodes;
	}
}

public class MethodNode : MemberNode {
	protected override char InstanceCode => 'm';

	protected override char StaticCode => 'M';

	public MethodNode (IMethod method, MetadataNode parent) : base (parent)
	{
		StringBuilder sb = new ("[");
		if (method.IsStatic)
			sb.Append (StaticCode);
		else
			sb.Append (InstanceCode);
		sb.Append ("] ").AppendMethod (method);
		Text = sb.ToString ();
		Tag = method;
	}
}

public class ConstructorNode : MemberNode {
	protected override char InstanceCode => 'c';

	protected override char StaticCode => 'C';

	public ConstructorNode (IMethod method, MetadataNode parent) : base (parent)
	{
		StringBuilder sb = new ("[");
		if (method.IsStatic)
			sb.Append (StaticCode);
		else
			sb.Append (InstanceCode);
		sb.Append ("] ").Append (method.DeclaringType.Name);
		sb.Append ('(');
		if (method.Parameters.Count > 0) {
			foreach (var p in method.Parameters) {
				sb.AppendType (p.Type).Append (',');
			}
			sb.Length--;
		}
		sb.Append (')');
		Text = sb.ToString ();
		Tag = method;
	}
}
