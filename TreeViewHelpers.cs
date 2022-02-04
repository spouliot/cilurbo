using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace Cilurbo;

static class TreeViewHelpers {

	public static AssemblyNode Add (this TreeView self, PEFile file)
	{
		// assemblies are first level, no need for recursion
		foreach (var n in self.Objects) {
			if (n is AssemblyNode an) {
				if ((an.Tag == file) || ((an.Tag is PEFile pe) && (pe.FileName == file.FileName)))
					return an;
			}
		}
		AssemblyNode node = new (file);
		self.AddObject (node);
		return node;
	}

	public static ITreeNode? Find (this TreeView self, Predicate<ITreeNode> predicate)
	{
		return self.Find (self.Objects, predicate);
	}

	public static ITreeNode? Find (this TreeView self, ITreeNode node, Predicate<ITreeNode> predicate)
	{
		return self.Find (node.Children, predicate);
	}

	public static ITreeNode? Find (this TreeView self, IEnumerable<ITreeNode> nodes, Predicate<ITreeNode> predicate)
	{
		Stack<ITreeNode> parents = new ();
		return Find (self, nodes, predicate, parents);
	}

	static ITreeNode? Find (this TreeView self, IEnumerable<ITreeNode> nodes, Predicate<ITreeNode> predicate, Stack<ITreeNode> parents)
	{
		foreach (var n in nodes) {
			if (predicate (n))
				return n;

			parents.Push (n);
			if (n.Children.Any ()) {
				var found = Find (self, n.Children, predicate, parents);
				if (found is not null)
					return found;
			}
			parents.Pop ();
		}
		return null;
	}

	public static ITreeNode? Find (this TreeView self, PEFile pe)
	{
		foreach (var n in self.Objects) {
			if (n is AssemblyNode an) {
				if (pe.FileName == an.PEFile.FileName)
					return n;
			}
		}
		return null;
	}

	public static MetadataNode? Select (this TreeView self, ITreeNode? node)
	{
		if (node is not MetadataNode m)
			return null;

		// must be expanded from the top
		Stack<MetadataNode> stack = new ();
		var p = m.Parent;
		while (p is not null) {
			stack.Push (p);
			p = p.Parent;
		}
		while (stack.Count > 0)
			self.Expand (stack.Pop ());

		self.GoTo (m);
		self.SelectedObject = m;
		self.SetFocus ();
		return m;
	}
}
