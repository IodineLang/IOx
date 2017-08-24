namespace iox {
	using System;
	using Iodine.Runtime;
	using libiox;
	using static ANSIColor;
	using static PowerlineBuilder;

	public class Shell {

		/// <summary>
		/// The iodine context.
		/// </summary>
		readonly IodineContext Iodine;

		/// <summary>
		/// The prompt.
		/// </summary>
		readonly Prompt Prompt;

		/// <summary>
		/// The configuration.
		/// </summary>
		readonly Configuration Conf;

		/// <summary>
		/// The last buffered user input.
		/// </summary>
		string BufferedInput;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.Shell"/> class.
		/// </summary>
		public Shell (Configuration conf) {
			Conf = conf;

			// Setup Iodine context
			Iodine = new IodineContext ();
			Iodine.AddSearchPaths ("./iodine/modules");
			Iodine.RegisterExtension<IoxExtension> ("iox");

			// Set prompt
			Prompt = new Prompt ($"λ");
			if (Conf.UsePowerlines) {
				var version = ReflectionHelper.GetVersion ().ToString (2);
				Prompt = new Prompt (Powerline ().Segment ($"IoX {version} ", fg: DarkCyan, bg: White).ToAnsiString ());
			}
		}

		/// <summary>
		/// Enter the REPL.
		/// </summary>
		public void Run () {
			
			// Collect version information
			var ioxVersion = ReflectionHelper.GetVersion ().ToString (2);
			var iodineVersion = ReflectionHelper.GetIodineVersion ().ToString (3);
			var iodineBuildDate = ReflectionHelper.GetIodineBuildDate ()?.ToString ("MMM dd yyyy") ?? string.Empty;

			// Print version information
			if (Conf.UsePowerlines) {
				ANSI.WriteLine (
					Powerline ()
					.Segment ($"IoX {ioxVersion} ", fg: DarkCyan, bg: White)
					.Segment ($" Iodine {iodineVersion} {iodineBuildDate}", fg: White, bg: DarkCyan)
					.ToAnsiString ());
			} else {
				ANSI.WriteLine ($"IOx {ioxVersion} (Iodine {iodineVersion} {iodineBuildDate})\n");
			}

			while (true) {
				RunIteration ();
			}
		}

		/// <summary>
		/// Run a single read-evaluate-print iteration.
		/// </summary>
		void RunIteration () {

			// Display the prompt
			ANSI.Write (Prompt.ToString ());

			// Read a line
			BufferedInput = Console.ReadLine ().Trim ();
			if (string.IsNullOrEmpty (BufferedInput)) return;

			// Compile the module
			var module = WrapIodineOperation (this, () => Iodine.CompileSource (BufferedInput));
			if (module == null) return;

			// Invoke the module
			var result = WrapIodineOperation (this, () => Iodine.InvokeModule (module));
			if (result == null) return;

			// Pretty print the result
			PrettyPrint.WriteLine (result);
		}

		/// <summary>
		/// Execute code safely using the Iodine engine.
		/// </summary>
		/// <returns>The iodine operation.</returns>
		/// <param name="instance">Instance.</param>
		/// <param name="action">Action.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		static T WrapIodineOperation<T> (Shell instance, Func<T> action) where T: IodineObject {
			try {

				// Try to just run the specified operation
				return action ();
			} catch (Iodine.Compiler.SyntaxException e) {

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
						Console.Write (instance.BufferedInput);

						// Highlight the error
						Console.CursorLeft = err.Location.Column - token_length;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{instance.BufferedInput.Substring (err.Location.Column - token_length, token_length)}");

						// Print the error location and description
						ANSI.WriteLine ($"{White}{string.Empty.PadLeft (token_length, '^').PadLeft (err.Location.Column)} {Red}{err.Text}");
						continue;
					}

					// Print user input for later highlighting
					Console.WriteLine (instance.BufferedInput);

					// Print the error location and description
					ANSI.WriteLine ($"{White}{"^".PadLeft (err.Location.Column, ' ')} {Red}{err.Text}");
				}
			} catch (UnhandledIodineExceptionException e) {

				// Pretty print the underlying Iodine exception
				PrettyPrint.WriteLine (e.OriginalException);

				// Test if the error occurred in an actual module
				if (e.Frame.Module.Name != "__anonymous__") {

					// Print the stack trace
					e.PrintStack ();
				}
			} catch (ModuleNotFoundException e) {

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
			} catch (Exception e) {

				// The Iodine engine exploded and we have no idea why.
				// Show the error to the user, maybe he can do something useful with it.
				Console.WriteLine (e.Message);
			}

			// Nothing to do here
			return null;
		}
	}
}