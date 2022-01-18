using System.Reflection.Metadata;
using System.Text;

using Terminal.Gui.Trees;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

class AssemblyNode : ITreeNode {
	public AssemblyNode (PEFile file)
	{
		Tag = file;
		var metadata = file.Metadata;
		var ad = metadata.GetAssemblyDefinition ();
		var aname = metadata.GetString (ad.Name);
		StringBuilder sb = new ("[A] ");
		sb.Append (aname).Append (" (").Append (ad.Version).Append (')');
		Text = sb.ToString ();

		DecompilerTypeSystem typeSystem = new (file, AssemblyResolver.Resolver,
			TypeSystemOptions.Default | TypeSystemOptions.Uncached);

		List<ITreeNode> nodes = new ();
		SortedList<string, ITreeNode> references = new ();
		foreach (var ar in file.AssemblyReferences) {
			references.Add (ar.Name, new AssemblyReferenceNode (ar));
		}
		foreach (var node in references.Values) {
			nodes.Add (node);
		}
		references.Clear ();

		foreach (var mrh in metadata.GetModuleReferences ()) {
			var mr = metadata.GetModuleReference (mrh);
			var name = metadata.GetString (mr.Name);
			references.Add (name, new ModuleReferenceNode (name, mr));
		}
		foreach (var node in references.Values) {
			nodes.Add (node);
		}
		references.Clear ();

		List<string> list = new ();
		foreach (var type in typeSystem.GetTopLevelTypeDefinitions ()) {
			if (type.ParentModule.Name != aname)
				continue;
			var ns = type.Namespace;
			if (!list.Contains (ns))
				list.Add (ns);
		}
		list.Sort ();
		foreach (var ns in list) {
			nodes.Add (new NamespaceTreeNode (ns, aname, typeSystem));
		}
		list.Clear ();

		// TODO: add resources

		Children = nodes;
	}

    public string Text { get; set; }

    public IList<ITreeNode> Children { get; }

    public object Tag { get; set; }
}

class AssemblyReferenceNode : ITreeNode {

	public AssemblyReferenceNode (ICSharpCode.Decompiler.Metadata.AssemblyReference ar)
	{
		Tag = ar;
		Text = "[R] " + ar.Name;
	}

	public string Text { get; set; }

	public IList<ITreeNode> Children => Array.Empty<ITreeNode> ();

	public object Tag { get; set; }
}

class ModuleReferenceNode : ITreeNode {
	
	public ModuleReferenceNode (string name, ModuleReference mr)
	{
		Tag = mr;
		Text = "[r] " + name;
	}

	public string Text { get; set; }

	public IList<ITreeNode> Children => Array.Empty<ITreeNode> ();

	public object Tag { get; set; }
}

class NamespaceTreeNode : ITreeNode {

    public NamespaceTreeNode (string fullname, string parent, IDecompilerTypeSystem typeSystem)
	{
		Tag = fullname;
		if (fullname.Length == 0)
			Text = "[N] -";
		else
			Text = "[N] " + fullname;

		List<ITreeNode> types = new ();
		foreach (var type in typeSystem.GetTopLevelTypeDefinitions ().OrderBy (t => t.Name)) {
			if (type.ParentModule.Name != parent)
				continue;
			if (type.Namespace == fullname)
				types.Add (new TypeTreeNode (type, typeSystem));
		}
		Children = types;
	}

    public string Text { get; set; }

    public IList<ITreeNode> Children { get; private set; }

    public object Tag { get; set; }
}

class TypeTreeNode : ITreeNode {

	public TypeTreeNode (ITypeDefinition type, IDecompilerTypeSystem typeSystem)
	{
		Text = "[T] " + type.Name;

		TypeDefinitionHandle handle = (TypeDefinitionHandle) type.MetadataToken;
        if (typeSystem.MainModule.ResolveEntity (handle) is not ITypeDefinition t)
            throw new InvalidOperationException ();
        Tag = t;

		List<ITreeNode> nodes = new ();
		
		// var baseTypes = t.GetNonInterfaceBaseTypes().Where (b => b.DeclaringType != t).ToList();
		// var b2 = t.GetAllBaseTypes ();
		// var b3 = t.GetAllBaseTypeDefinitions ();

		// var b4 = t.ParentModule.PEFile.Metadata.GetTypeDefinition (handle);
		// var b5 = b4.BaseType;

		foreach (var b in t.DirectBaseTypes.OrderBy (b => b.Name)) {
			// if (type.MetadataToken == b.MetadataToken)
			// 	continue;
			if (t.Kind != TypeKind.Interface || t.Kind == b.Kind)
				nodes.Add (new BaseTypeNode (b.GetDefinition ()!));
		}
		foreach (var n in t.NestedTypes.OrderBy (n => n.Name)) {
			nodes.Add (new TypeTreeNode (n, typeSystem));
		}
		foreach (var f in t.Fields.OrderBy (f => f.Name)) {
			nodes.Add (new FieldNode (f));
		}
		foreach (var e in t.Events.OrderBy (e => e.Name)) {
			nodes.Add (new EventNode (e));
		}
		foreach (var p in t.Properties.OrderBy (p => p.Name)) {
			nodes.Add (new PropertyNode (p));
		}
		foreach (var m in t.Methods.OrderBy (m => m.Name)) {
			// e.g. some default .ctor on struct are present in the collection
			if (m.MetadataToken.IsNil)
				continue;
			if (m.IsConstructor)
				nodes.Add (new ConstructorNode (m));
			else
				nodes.Add (new MethodNode (m));
		}

		Children = nodes;
	}

    public string Text { get; set; }

    public IList<ITreeNode> Children { get; private set; }

    public object Tag { get; set; }
}

class BaseTypeNode : ITreeNode {
	
	public BaseTypeNode (ITypeDefinition type)
	{
		if (type.Kind == TypeKind.Interface)
			Text = "[i] " + type.Name;
		else
			Text = "[b] " + type.Name;
		Tag = type;
	}

	public string Text { get; set; }

	public IList<ITreeNode> Children => Array.Empty<ITreeNode> ();

	public object Tag { get; set; }
}

abstract class MemberNode : ITreeNode {

	protected abstract char InstanceCode { get; }
	protected abstract char StaticCode { get; }
	
#pragma warning disable 8618
	protected MemberNode ()
	{
		// setting Text and Tag to non-null is a responsability to subclasses calling this .ctor
	}
#pragma warning restore 8618

	protected MemberNode (IMember member)
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

	public string Text { get; set; }

	public IList<ITreeNode> Children => Array.Empty<ITreeNode> ();

	public object Tag { get; set; }
}

class FieldNode : MemberNode {
    protected override char InstanceCode => 'f';

    protected override char StaticCode => 'F';

	public FieldNode (IField field) : base (field)
	{
	}
}

class EventNode : MemberNode {
    protected override char InstanceCode => 'e';

    protected override char StaticCode => 'E';

	public EventNode (IEvent @event) : base (@event)
	{
	}
}

class PropertyNode : MemberNode {
    protected override char InstanceCode => 'p';

    protected override char StaticCode => 'P';

	public PropertyNode (IProperty property) : base (property)
	{
	}
}

class MethodNode : MemberNode {
    protected override char InstanceCode => 'm';

    protected override char StaticCode => 'M';

	public MethodNode (IMethod method)
	{
		StringBuilder sb = new ("[");
		if (method.IsStatic)
			sb.Append (StaticCode);
		else
			sb.Append (InstanceCode);
		sb.Append ("] ").Append (method.Name);
		sb.Append ('(');
		if (method.Parameters.Count > 0) {
			foreach (var p in method.Parameters) {
				sb.AppendType (p.Type).Append (',');
			}
			sb.Length--;
		}
		sb.Append (')');
		sb.Append (" : ").AppendType (method.ReturnType);
		Text = sb.ToString ();
		Tag = method;
	}
}

class ConstructorNode : MemberNode {
    protected override char InstanceCode => 'c';

    protected override char StaticCode => 'C';

	public ConstructorNode (IMethod method)
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
