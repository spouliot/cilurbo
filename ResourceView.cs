using System.Collections;
using System.Data;
using System.Resources;
using ICSharpCode.Decompiler.Metadata;
using Terminal.Gui;

namespace Cilurbo;

class ResourceView : View {

	public ResourceView ()
	{
		Width = Dim.Fill ();
		Height = Dim.Fill ();

		TextView = new TextView () {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			ReadOnly = true,
			Visible = false,
		};
		Add (TextView);

		TableView = new ExportableTableView () {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Visible = false,
		};
		Add (TableView);

		ScrollBarView sbv = new (TextView, true);

		// copied from terminal.gui sample
		sbv.ChangedPosition += () => {
			TextView.TopRow = sbv.Position;
			if (TextView.TopRow != sbv.Position) {
				sbv.Position = TextView.TopRow;
			}
			TextView.SetNeedsDisplay ();
		};

		sbv.OtherScrollBarView.ChangedPosition += () => {
			TextView.LeftColumn = sbv.OtherScrollBarView.Position;
			if (TextView.LeftColumn != sbv.OtherScrollBarView.Position) {
				sbv.OtherScrollBarView.Position = TextView.LeftColumn;
			}
			TextView.SetNeedsDisplay ();
		};

		sbv.VisibleChanged += () => {
			if (sbv.Visible && TextView.RightOffset == 0) {
				TextView.RightOffset = 1;
			} else if (!sbv.Visible && TextView.RightOffset == 1) {
				TextView.RightOffset = 0;
			}
		};

		sbv.OtherScrollBarView.VisibleChanged += () => {
			if (sbv.OtherScrollBarView.Visible && TextView.BottomOffset == 0) {
				TextView.BottomOffset = 1;
			} else if (!sbv.OtherScrollBarView.Visible && TextView.BottomOffset == 1) {
				TextView.BottomOffset = 0;
			}
		};

		TextView.DrawContent += (e) => {
			sbv.Size = TextView.Lines;
			sbv.Position = TextView.TopRow;
			if (sbv.OtherScrollBarView != null) {
				sbv.OtherScrollBarView.Size = TextView.Maxlength;
				sbv.OtherScrollBarView.Position = TextView.LeftColumn;
			}
			sbv.LayoutSubviews ();
			sbv.Refresh ();
		};
	}

	public TextView TextView { get; private set; }
	public ExportableTableView TableView { get; private set; }

	public string? Title { get; set; }

	public void Show (Resource resource)
	{
		var tab = (SuperView.SuperView as TabView)!.SelectedTab;

		Title = resource.Name;
		tab.Text = resource.Name;
		switch (Path.GetExtension (resource.Name)) {
		case ".resources":
			TextView.Visible = false;
			TableView.Table = GetDataTable (resource);
			TableView.Visible = true;
			break;
		// TODO - images/video/sounds... open externally ?
		default: // read as text
			TableView.Visible = false;
			try {
				var stream = resource.TryOpenStream ();
				if (stream is not null)
					TextView.Text = new StreamReader (stream).ReadToEnd ();
			} catch (Exception ex) {
				TextView.Text = ex.ToString ();
			}
			TextView.Visible = true;
			break;
		}
	}

	static DataTable GetDataTable (Resource resource)
	{
		DataTable dt = new ();
		dt.Columns.Add (new DataColumn ("Key", typeof (string)));
		dt.Columns.Add (new DataColumn ("Value", typeof (string)));
		dt.Columns.Add (new DataColumn ("Type", typeof (string)));
		var stream = resource.TryOpenStream ();
		if (stream is null)
			return dt;

		try {
			ResourceReader res = new (stream);
			foreach (DictionaryEntry kvp in res) {
				var key = kvp.Key as string;
				res.GetResourceData (key!, out var type, out var _);
				var value = kvp.Value is null ? "<null>" : kvp.Value.ToString ()!;
				dt.Rows.Add (new object [] {
					key is null ? "<null>" : key.ToString (),
					value,
					type,
				});
			}
		} catch (Exception ex) {
			dt.Rows.Add (new object [] { ex.ToString (), "", "" });
		}
		return dt;
	}
}
