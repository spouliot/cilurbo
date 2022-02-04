using Terminal.Gui;

namespace Cilurbo;

public static class ColumnStyles {

	public static readonly TableView.ColumnStyle IntHex8 = new () {
		RepresentationGetter = (object o) => {
			if (o is int i)
				return "0x" + i.ToString ("x8");
			return o.ToString ();
		}
	};
}
