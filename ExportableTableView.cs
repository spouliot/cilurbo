using System.Data;
using Cilurbo.Services;
using Terminal.Gui;

namespace Cilurbo;

class ExportableTableView : TableView {

	public ExportableTableView ()
	{
	}

	public string? Title { get; set; }

	public override bool ProcessKey (KeyEvent kb)
	{
		switch (kb.Key) {
		case Key.CtrlMask | Key.E:
			ExportTable ();
			return true;
		case Key.CtrlMask | Key.G:
			GistTable ();
			return true;
		}
		return base.ProcessKey (kb);
	}

	protected virtual void ExportMarkdown (TextWriter writer)
	{
		foreach (DataColumn column in Table.Columns)
			writer.Write ($"| {column.ColumnName} ");
		writer.WriteLine ("|");

		foreach (DataColumn column in Table.Columns) {
			switch (Type.GetTypeCode (column.DataType)) {
			case TypeCode.String:
				writer.Write ("| :---- ");
				break;
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
				writer.Write ("| ----: ");
				break;
			default:
				writer.Write ("| :---: ");
				break;
			}
		}
		writer.WriteLine ("|");

		foreach (DataRow row in Table.Rows) {
			for (int i = 0; i < Table.Columns.Count; i++)
				writer.Write ($"| {row [i]} ");
			writer.WriteLine ("|");
		}
		writer.Flush ();
	}

	void ExportTable ()
	{
		using SaveDialog d = new ("Export Table", "", new () { ".md" });
		Application.Run (d);
		if (!d.Canceled && (d.FilePath is not null)) {
			using StreamWriter writer = new (d.FilePath.ToString ()!);
			ExportMarkdown (writer);
			writer.Close ();
		}
	}

	void GistTable ()
	{
		using StringWriter writer = new ();
		ExportMarkdown (writer);
		writer.Close ();
		_ = GistSupport.Gist (Title ?? "cilurbo", "table.md", writer.ToString ());
	}
}
