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

	public static ITreeNode? Select (this TreeView self, Predicate<ITreeNode> predicate)
	{
		return self.Select (self.Objects, predicate);
	}

	public static ITreeNode? Select (this TreeView self, ITreeNode node, Predicate<ITreeNode> predicate)
	{
		return self.Select (node.Children, predicate);
	}

	public static ITreeNode? Select (this TreeView self, IEnumerable<ITreeNode> nodes, Predicate<ITreeNode> predicate)
	{
		Stack<ITreeNode> parents = new ();
		var result = Find (self, nodes, predicate, parents);
		if (result is not null) {
			if (parents.Count > 0) {
				foreach (var p in parents.Reverse ())
					self.Expand (p);
			}
			self.GoTo (result);
			self.SelectedObject = result;
			self.SetFocus ();
		}
		return result;
	}

	public static ITreeNode? Find (this TreeView self, Predicate<ITreeNode> predicate)
	{
		return self.Find (self.Objects, predicate);
	}

	public static ITreeNode? Find (this TreeView self, ITreeNode node, Predicate<ITreeNode> predicate)
	{
		return self.Select (node.Children, predicate);
	}

	public static ITreeNode? Find (this TreeView self, IEnumerable<ITreeNode> nodes, Predicate<ITreeNode> predicate)
	{
		Stack<ITreeNode> parents = new ();
		return Find (self, self.Objects, predicate, parents);
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

	// helper to avoid recursion since assembly nodes are always first level
	public static ITreeNode? Select (this TreeView self, PEFile pe)
	{
		foreach (var n in self.Objects) {
			if (n is AssemblyNode an) {
				if (pe.FileName == an.PEFile.FileName) {
					self.GoTo (n);
					self.SelectedObject = n;
					self.SetFocus ();
					return n;
				}
			}
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
}
