using Terminal.Gui;
using Terminal.Gui.Trees;

namespace Cilurbo.Services;

static class DashSupport {

	// TODO use `dash-plugin://` https://kapeli.com/dash_plugins
	static public bool Open (string? search)
	{
		if (String.IsNullOrEmpty (search))
			return false;

		// note: both `System.Net.WebUtility.UrlEncode` and `System.Web.HttpUtility.UrlEncode` encodes spaces as `+`, not `%20`
		// and dash needs the use of percent-encoding https://en.wikipedia.org/wiki/Percent-encoding
		var q = Uri.EscapeDataString (search);

		// `-g` as Dash will decide if it needs to become the active window -> https://github.com/ram-nadella/DashMate.tmbundle/issues/17
		var result = ExecSupport.Run ("open", $"-g \"dash://{q}\"");
		// TODO log error
		if (result != 0)
			MessageBox.Query ("Error", $"Dash failed to open {result}", "_Ok");

		return result == 0;
	}

	static public bool Open (ITreeNode node)
	{
		return Open (node.Text [4..]);
	}
}
