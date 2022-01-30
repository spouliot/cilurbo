using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;

namespace Cilurbo.MetadataTables;

public class MetadataView : View {
	readonly Label assembly_label;
	readonly ListView listview;

	readonly MetadataTableView table;

	public MetadataView ()
	{
		Width = Dim.Fill ();
		Height = Dim.Fill ();
		CanFocus = false;

		assembly_label = new () {
			Text = "Assembly: -",
		};
		Label table_label = new () {
			Y = 1,
			Text = "Tables:",
		};
		listview = new () {
			X = 0,
			Y = 2,
			Width = MetadataDataSource.Shared.Length + 1,
			Height = Dim.Fill (),
			AllowsMultipleSelection = false,
			CanFocus = true,
			Source = MetadataDataSource.Shared,
		};
		table = new () {
			X = Pos.Right (listview),
			Y = 1,
		};

		Add (assembly_label, table_label, listview, table);

		listview.OpenSelectedItem += (args) => {
			if (PEFile is null)
				return;
			var table_name = listview.Source.ToList () [listview.SelectedItem] as string;
			table.Table = MetadataTable.GetTable (table_name!, PEFile);
			table.SetFocus ();
		};
	}

	PEFile? pefile;

	public PEFile? PEFile {
		get { return pefile; }
		set {
			if (pefile == value)
				return;
			pefile = value;
			if (pefile is not null) {
				assembly_label.Text = $"Assembly: {pefile.FullName}";
				table.Table = null;
				listview.SelectedItem = 0;
				listview.SetFocus ();
			} else {
				assembly_label.Text = "Assembly: -";
			}
		}
	}
}
