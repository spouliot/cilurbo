using Terminal.Gui;
using Terminal.Gui.Trees;

using ICSharpCode.Decompiler.Metadata;

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
		Stack<ITreeNode> parents = new ();
		var result = Select (self, self.Objects, predicate, parents);
		if (result is not null) {
			if (parents.Count > 0) {
				foreach (var p in parents.Reverse ())
					self.Expand (p);
			}
			self.GoTo (result);
			self.SelectedObject = result;
		}
		return result;
	}

	static ITreeNode? Select (this TreeView self, IEnumerable<ITreeNode> nodes, Predicate<ITreeNode> predicate, Stack<ITreeNode> parents)
	{
		foreach (var n in nodes) {
			if (predicate (n))
				return n;

			parents.Push (n);
			if (n.Children.Any ()) {
				var found = Select (self, n.Children, predicate, parents);
				if (found is not null)
					return found;
			}
			parents.Pop ();
		}
		return null;
	}
}
