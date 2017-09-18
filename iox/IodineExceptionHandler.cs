namespace iox {
	using System;
	using System.Linq;
	using static ANSIColor;

	public static class IodineExceptionHandler {

		public static void HandleModuleNotFoundException (Shell instance, Iodine.Runtime.ModuleNotFoundException e) {
			// Print the name of the missing module
			ANSI.WriteLine ($"Unable to find module '{White}{e.Name}{ANSI.ESC}[0m'");

			// Iterate over the search paths
			foreach (var searchPath in e.SearchPath) {

				// Create a relative URI of the search path
				var workingPathUri = new Uri (Environment.CurrentDirectory);
				var searchPathUri = new Uri (System.IO.Path.GetFullPath (searchPath), UriKind.RelativeOrAbsolute);
				var relativePathUri = workingPathUri.MakeRelativeUri (searchPathUri);

				// Print the relative search path
				ANSI.WriteLine ($"- ./{relativePathUri}");
			}
		}

		public static void HandleUnhandledIodineException (Shell instance, Iodine.Runtime.UnhandledIodineExceptionException e) {
			
			// Pretty print the underlying Iodine exception
			PrettyPrint.WriteLine (e.OriginalException);

			// Test if the error occurred in an actual module
			if (e.Frame.Module.Name != "__anonymous__") {

				// Print the stack trace
				e.PrintStack ();
			}
		}
		
		public static void HandleSyntaxException (Shell instance, Iodine.Compiler.SyntaxException e) {
			
			// Iterate over syntax errors
			foreach (var err in e.ErrorLog.Errors) {

				// Test if the error came from somewhere else
				if (!string.IsNullOrEmpty (err.Location.File)) {

					// Get the name of the file that caused the error
					var path = System.IO.Path.GetFileNameWithoutExtension (err.Location.File);

					// Print the error location and description
					ANSI.WriteLine ($"Error at {White}{path}{Default} ({err.Location.Line + 1}:{err.Location.Column}): {err.Text}");
					continue;
				}

				// Test if the error has an associated token
				if (err.HasToken) {

					// Get the length of the associated token
					var token_length = err.Token.Value.Length;

					// Print user input for later highlighting
					var line = instance.BufferedInput.Split ('\n').ElementAtOrDefault (err.Location.Line)?.Trim ();
					if (!string.IsNullOrEmpty (line)) {
						
						// Highlight the error
						var errHighlight = line.Substring (err.Location.Column, line [err.Location.Column] == '"' ? token_length + 2 : token_length);
						var errUnderline = string.Empty.PadLeft (token_length, '^').PadLeft (err.Location.Column);
						Console.Write (line);
						Console.CursorLeft = err.Location.Column;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{errHighlight}");
						ANSI.WriteLine ($"{White}{errUnderline}");
					}
				} else {

					// Print user input for later highlighting
					var line = instance.BufferedInput.Split ('\n').ElementAtOrDefault (err.Location.Line)?.Trim ();
					if (!string.IsNullOrEmpty (line)) {
						
						// Highlight the error
						var startPosition = Math.Max (0, err.Location.Column - 1);
						Console.Write (line);
						Console.CursorLeft = startPosition;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line [Math.Min (line.Length, startPosition)]}");
						ANSI.WriteLine ($"{White}{"^".PadLeft (err.Location.Column, ' ')}");
					}
				}

				// Print the error location and description
				ANSI.WriteLine ($"\n{White}SyntaxError{(err.Location.Line != 0 ? $" at line {err.Location.Line + 1}" : string.Empty)}: {Red}{err.Text}");
			}
		}
	}
}
