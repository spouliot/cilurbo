using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;

namespace Cilurbo.MetadataTables;

public class MetadataTableView : View {
	readonly Label assembly_label;
	readonly ListView listview;

	public MetadataTableView ()
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

		ExportableTableView table = new () {
			X = Pos.Right (listview),
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			CanFocus = true,
			FullRowSelect = true,
		};
		table.Style.AlwaysShowHeaders = true;
		table.CellActivated += (e) => {
			switch (e.Table.ExtendedProperties ["Metadata"]) {
			case MetadataTable.AssemblyRef:
				if (e.Table.ExtendedProperties ["PE"] is not PEFile pe)
					break;
				var handle = MetadataTokens.AssemblyReferenceHandle ((int) e.Table.Rows [e.Row] [0]);
				AssemblyReference ar = new (pe.Metadata, handle);
				var a = AssemblyResolver.Resolver.Resolve (ar);
				if (a is not null) {
					var an = Program.Select ((n) => a.Equals (n.Tag) && (n is AssemblyNode));
					if (an is null) {
						an = Program.Add (a, selectAndGoto: true);
					}
				}
				Program.EnsureSourceView ().Show (a);
				break;
			}
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
				listview.SelectedItem = 0;
				listview.SetFocus ();
			} else {
				assembly_label.Text = "Assembly: -";
			}
		}
	}
}
