namespace iox {
	using System;
	using System.Linq;
	using Mono.Terminal;

	public class HinterWrapper {
		readonly Shell Shell;
		readonly LineEditor LineEditor;

		public HinterWrapper (Shell shell) {
			Shell = shell;
			LineEditor = new LineEditor ("iox") {
				HeuristicsMode = "iodine",
				TabAtStartCompletes = false,
			};

			LineEditor.AutoCompleteEvent += (string text, int pos) => {
				var strippedText = text.Split (' ').Last ();
				string [] completions = new string [0];
				if (text.Contains ('.')) {
					var attrName = text.Substring (0, text.LastIndexOf ('.'));
					var attrObj = Shell.Iodine.CompileAndInvokeOrNull (attrName);
					if (attrObj != null) {
						text = text.Split (new [] { ' ', '.', '(', '[', '{', '}', ']', ')' }).Last ();
						pos = text.Length;
						completions = (
							attrObj.Attributes
							.Where (attr => attr.Key.StartsWith (text, StringComparison.Ordinal))
							.Select (attr => attr.Key)
							.Where (attr => !attr.StartsWith ("__", StringComparison.Ordinal) && !attr.EndsWith ("__", StringComparison.Ordinal))
							.Select (attr => attr.Substring (pos))
							.Reverse ()
							.ToArray ()
						);
					}
				} else {
					text = text.Split (new [] { ' ', '.', '(', '[', '{', '}', ']', ')' }).Last ();
					pos = text.Length;
					completions = (
						Shell.Iodine.Engine.Context.Globals
						.Concat (Shell.Iodine.Engine.Context.InteractiveLocals)
						.Where (attr => attr.Key.StartsWith (text, StringComparison.Ordinal))
						.Select (attr => attr.Key)
						.Where (attr => !attr.StartsWith ("__", StringComparison.Ordinal) && !attr.EndsWith ("__", StringComparison.Ordinal))
						.Select (attr => attr.Substring (pos))
						.Reverse ()
						.ToArray ()
					);
				}
				return new LineEditor.Completion (text.Split (' ').Last (), completions);
			};
		}

		public string Edit (string prompt, string initial = null) {
			return LineEditor.Edit (prompt, initial ?? string.Empty);
		}

		public void SaveHistory () {
			LineEditor.SaveHistory ();
		}
	}
}
