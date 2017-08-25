namespace iox {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
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
		/// The assembly version.
		/// </summary>
		readonly Version AssemblyVersion;

		/// <summary>
		/// The last buffered user input.
		/// </summary>
		string BufferedInput;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.Shell"/> class.
		/// </summary>
		public Shell (Configuration conf) {
			Conf = conf;

			// Get assembly version
			AssemblyVersion = ReflectionHelper.GetVersion ();

			// Setup Iodine context
			Iodine = new IodineContext ();
			Iodine.AddSearchPaths ("./iodine/modules");
			Iodine.RegisterExtension<IoxExtension> ("iox");

			// Set prompt
			Prompt = new Prompt ("λ");
			if (Conf.UsePowerlines) {
				Prompt = new Prompt (Powerline ().Segment ($"IoX {AssemblyVersion.ToString (2)} ", fg: DarkCyan, bg: White).ToAnsiString ());
			}
		}

		/// <summary>
		/// Enter the REPL.
		/// </summary>
		public void Run () {
			
			// Collect version information
			var ioxVersion = AssemblyVersion.ToString (2);
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

			// Read user input
			BufferedInput = ReadUserInput ();
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

		string ReadUserInput () {

			// Declare variables
			var accum = new StringBuilder ();
			string lastLine = string.Empty;
			bool editingFinished = false;
			bool correctIndent = false;
			int foldCount = 0;
			int line = 1;
			int indent = 0;

			// Define getFoldCount function
			var getFoldCount = new Func<string, int> (str => {
				var lexer = new Iodine.Compiler.Tokenizer (
					new Iodine.Compiler.ErrorSink (),
					new Iodine.Compiler.SourceReader (str, "__anonymous__")
				);
				IEnumerable<Iodine.Compiler.Token> tokens;
				try {
					tokens = lexer.Scan ();
				} catch {
					return 0;
				}
				return (
					tokens.Count (t => new [] {
						global::Iodine.Compiler.TokenClass.OpenBrace,
						global::Iodine.Compiler.TokenClass.OpenBracket,
						global::Iodine.Compiler.TokenClass.OpenParan,
					}.Contains (t.Class)) -
					tokens.Count (t => new [] {
						global::Iodine.Compiler.TokenClass.CloseBrace,
						global::Iodine.Compiler.TokenClass.CloseBracket,
						global::Iodine.Compiler.TokenClass.CloseParan,
					}.Contains (t.Class))
				);
			});

			// Define getPrompt function
			var getPrompt = new Func<int, string> (lineNum => {
				if (Conf.UsePowerlines) {
					var powerline = Powerline ()
						.Segment ($"IoX {AssemblyVersion.ToString (2)} ", fg: DarkCyan, bg: White)
						.Segment ($"{lineNum}", fg: White, bg: DarkCyan)
						.ToAnsiString ();
					return powerline;
				}
				return $"{lineNum} λ";
			});

			// Define rewriteIndent function
			var rewriteIndent = new Action (() => {
				
				// Save cursor state
				var currentCursorTop = Console.CursorTop;
				var targetCursorTop = Math.Max (0, Console.CursorTop - 1);

				// Rewrite last line
				Console.CursorTop = targetCursorTop;
				Console.CursorLeft = 0;
				Console.Write ("".PadRight (Console.WindowWidth));
				Console.CursorTop = targetCursorTop;
				Console.CursorLeft = 0;
				Prompt.Push (getPrompt (Math.Max (0, line - 1)));
				ANSI.Write (Prompt.ToString ());
				Prompt.Pop ();
				Console.Write (string.Empty.PadLeft (indent * 2));
				Console.Write (lastLine);

				// Restore cursor state
				Console.CursorTop = currentCursorTop;
				Console.CursorLeft = 0;
			});

			// Read more lines
			while (!editingFinished) {
				
				// Test if this is the first line
				if (line == 1) {

					// Duplicate prompt
					Prompt.Dup ();
				} else {
					
					// Push new prompt to visually indicate multi-line editing
					Prompt.Push (getPrompt (line));
				}

				// Test if the indentation of the previous line should be rewritten
				if (correctIndent) {

					// Rewrite line
					rewriteIndent ();

					// Do not correct the indentation again
					correctIndent = false;
				}

				// Test if the prompt of the previous line should be rewritten
				if (line == 2) {
					
					// Save cursor state
					var currentCursorTop = Console.CursorTop;
					var targetCursorTop = Math.Max (0, Console.CursorTop - 1);

					// Rewrite last line
					Console.CursorTop = targetCursorTop;
					Console.CursorLeft = 0;
					Console.Write ("".PadRight (Console.WindowWidth));
					Console.CursorTop = targetCursorTop;
					Console.CursorLeft = 0;
					Prompt.Push (getPrompt (1));
					ANSI.Write (Prompt.ToString ());
					Prompt.Pop ();
					Console.Write (lastLine);

					// Restore cursor state
					Console.CursorTop = currentCursorTop;
					Console.CursorLeft = 0;
				}

				// Write prompt
				ANSI.Write (Prompt.ToString ());

				// Write indent
				ANSI.Write (string.Empty.PadLeft (indent * 2));

				// Read line
				lastLine = Console.ReadLine ().Trim ();

				// Update state
				var localFoldCount = getFoldCount (lastLine);
				foldCount += localFoldCount;
				if (localFoldCount > 0) {
					indent += 1;
				} else if (localFoldCount < 0) {
					correctIndent = true;
					indent = Math.Max (0, indent - 1);
				}
				editingFinished = foldCount == 0;
				line += 1;

				// Test for negative (unfixable) grouping mismatch
				if (foldCount < 0) {

					// Clear buffer
					accum.Clear ();

					// Output error
					ANSI.WriteLine ($"{Red}Mismatched bracket, brace, or parenthesis group!");
					editingFinished = true;
				} else {
					
					// Append line to buffer
					accum.AppendLine (lastLine);
				}

				// Restore prompt
				Prompt.Pop ();
			}

			// Rewrite indent if fold count is 0
			if (correctIndent) rewriteIndent ();

			// Return buffer
			return accum.ToString ();
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
						var line = instance.BufferedInput.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						Console.CursorLeft = err.Location.Column - token_length;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line.Substring (err.Location.Column - token_length, token_length)}");
						ANSI.WriteLine ($"{White}{string.Empty.PadLeft (token_length, '^').PadLeft (err.Location.Column)}");
					} else {

						// Print user input for later highlighting
						var line = instance.BufferedInput.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						Console.CursorLeft = err.Location.Column - 1;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line [err.Location.Column - 1]}");
						ANSI.WriteLine ($"{White}{"^".PadLeft (err.Location.Column, ' ')}");
					}

					// Print the error location and description
					ANSI.WriteLine ($"\n{White}SyntaxError{(err.Location.Line != 0 ? $" at line {err.Location.Line + 1}" : string.Empty)}: {Red}{err.Text}");
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