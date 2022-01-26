using Cilurbo.Services;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Terminal.Gui;

namespace Cilurbo;

public enum Languages {
	IL,
	CSharp,
}

class SourceView : View {

	public SourceView ()
	{
		Width = Dim.Fill ();
		Height = Dim.Fill ();

		TextView = new SourceTextView (this);
		Add (TextView);

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

	public SourceTextView TextView { get; private set; }

	public string? Title { get; set; }


	Languages language = Languages.CSharp;

	public Languages Language {
		get { return language; }
		set {
			if (language != value) {
				language = value;
				Show (current_metadata);
			}
		}
	}

	object? current_metadata;

	public void Show (object? metadata)
	{
		var tab = (SuperView.SuperView as TabView)!.SelectedTab;
		switch (metadata) {
		case PEFile file:
			Title = file.Name;
			tab.Text = file.Name;
			TextView.Text = Language == Languages.IL ? file.Disassemble () : file.Decompile ();
			break;
		case IEntity entity:
			Title = entity.Name;
			tab.Text = entity.Name;
			TextView.Text = Language == Languages.IL ? entity.Disassemble () : entity.Decompile ();
			break;
		default:
			Title = "-";
			tab.Text = "-";
			TextView.Text = "";
			break;
		}
		current_metadata = metadata;
	}
}

class SourceTextView : TextView {

	readonly SourceView parent;

	public SourceTextView (SourceView parentView)
	{
		parent = parentView;

		Width = Dim.Fill ();
		Height = Dim.Fill ();
		ReadOnly = true;
	}

	public override bool ProcessKey (KeyEvent kb)
	{
		switch (kb.Key) {
		case Key.CtrlMask | Key.E:
			SaveSource ();
			return true;
		case Key.CtrlMask | Key.G:
			// syntax coloring is based on the file extension, the filename is not really important
			_ = GistSupport.Gist (parent.Title ?? "Cilurbo", parent.Language == Languages.IL ? "code.il" : "code.cs", Text.ToString ());
			return true;
		}
		return base.ProcessKey (kb);
	}

	void SaveSource ()
	{
		using SaveDialog d = new ("Export Source", "", new () { parent.Language == Languages.IL ? ".il" : ".cs" });
		Application.Run (d);
		if (!d.Canceled && (d.FilePath is not null)) {
			File.WriteAllText (d.FilePath.ToString ()!, Text.ToString ());
		}
	}
}
