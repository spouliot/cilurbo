using Cilurbo.Services;
using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;

namespace Cilurbo.Analyzers;

class AnalyzerView : ExportableTableView {

	public AnalyzerView ()
	{
		Width = Dim.Fill ();
		Height = Dim.Fill ();
		MultiSelect = false;
		FullRowSelect = true;
	}

	public override bool ProcessKey (KeyEvent kb)
	{
		switch (kb.Key) {
		case Key.CtrlMask | Key.D:
			var column = (Table.ExtendedProperties ["Analyzer"] as IAnalyzer)!.ExternallySearchableColumn;
			var symbol = Table.Rows [SelectedRow] [column] as string;
			DashSupport.Open (symbol);
			return true;
		}
		return base.ProcessKey (kb);
	}

	protected override void ExportMarkdown (TextWriter writer)
	{
		if (Table.ExtendedProperties ["Analyzer"] is IAnalyzer analyzer) {
			Title = analyzer.GetType ().Name;
			writer.WriteLine ($"# {Title}");
			writer.WriteLine ();
			if (Table.ExtendedProperties ["PE"] is PEFile pe) {
				writer.WriteLine ($"Assembly: {pe.FullName}");
				writer.WriteLine ();
				writer.WriteLine ($"Location: {pe.FileName}");
				writer.WriteLine ();
			}
			writer.WriteLine ();
			base.ExportMarkdown (writer);
			writer.WriteLine ();
			writer.WriteLine ("Generated by [Cilurbo](https://github.com/spouliot/cilurbo)");
		}
	}
}