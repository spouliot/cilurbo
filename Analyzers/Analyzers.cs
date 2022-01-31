using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;

namespace Cilurbo.Analyzers;

public interface IAnalyzer {

	// use `-1` if nothing useful can be searched
	int ExternallySearchableColumn { get; }

	bool IsApplicable ([NotNullWhen (true)] MetadataNode? node);

	DataTable GetTable (MetadataNode node);
}

[AttributeUsage (AttributeTargets.Class)]
sealed class AnalyzerAttribute : System.Attribute {

	public string Name { get; }

	public AnalyzerAttribute (string name)
	{
		Name = name;
	}
}

sealed class AnalyzerMenuItem : MenuItem {

	public AnalyzerMenuItem (string name, IAnalyzer analyzer)
	{
		Title = name;
		CanExecute = () => analyzer.IsApplicable (Program.SelectedNode);
		Action = () => {
			var node = Program.SelectedNode;
			if (!analyzer.IsApplicable (node))
				return;
			AnalyzersManager.EnsureAnalyzerView ().Table = analyzer.GetTable (node);
		};
	}
}

static class AnalyzersManager {

	public static MenuItem [] BuildMenu ()
	{
		List<MenuItem> list = new ();
		// TODO at some point we might want to extend this to allow external analyzers
		foreach (var type in typeof (AnalyzersManager).Assembly.GetTypes ()) {
			if (!type.IsClass || !type.IsPublic || type.IsAbstract)
				continue;
			if (!typeof (IAnalyzer).IsAssignableFrom (type))
				continue;
			var attr = type.GetCustomAttribute<AnalyzerAttribute> ();
			if (attr is null)
				continue;

			list.Add (new AnalyzerMenuItem (attr.Name, (IAnalyzer) Activator.CreateInstance (type)!));
		}
		list.Sort ();
		return list.ToArray ();
	}

	static AnalyzerView? analyzer_view;
	static TabView.Tab? analyzer_tab;

	internal static AnalyzerView EnsureAnalyzerView ()
	{
		if (analyzer_view is not null) {
			Program.SelectTab (analyzer_tab!);
			return analyzer_view;
		}

		analyzer_view = new ();
		analyzer_view.CellActivated += (e) => {
			if (e.Table.ExtendedProperties ["PE"] is PEFile pe) {
				var rid = MetadataTokens.EntityHandle (TableIndex.MethodDef, (int) e.Table.Rows [analyzer_view.SelectedRow] [1]);
				Program.Select ((n) => {
					if (n.Tag is IMember m) {
						if (m.ParentModule.PEFile != pe)
							return false;
						return m.MetadataToken.Equals (rid);
					}
					return false;
				});
			}
		};
		analyzer_tab = new ("Analyzer", analyzer_view);
		Program.AddTab (analyzer_tab);
		return analyzer_view;
	}
}
