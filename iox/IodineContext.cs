namespace iox {
	using Iodine.Interop;
	using Iodine.Runtime;

	/// <summary>
	/// Iodine context.
	/// </summary>
	public class IodineContext {

		/// <summary>
		/// The Iodine engine.
		/// </summary>
		public readonly IodineEngine Engine;

		/// <summary>
		/// The Iodine context.
		/// </summary>
		readonly Iodine.Compiler.IodineContext Context;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:libiox.IodineContext"/> class.
		/// </summary>
		public IodineContext () {

			// Create the Iodine engine
			Engine = new IodineEngine ();

			// Set the Iodine engine context
			Context = Engine.Context;
		}

		/// <summary>
		/// Register an Iodine extension.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void RegisterExtension<T> (string name) where T: IodineModule, new() {
			Context.ExposeModule (name, new T ());
		}

		/// <summary>
		/// Expose a global attribute.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="attr">Attribute.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void ExposeGlobal<T> (string name, T attr) where T: IodineObject {
			Context.Globals.Add (name, attr);
		}

		/// <summary>
		/// Add paths to the Iodine search path.
		/// </summary>
		/// <param name="paths">Paths.</param>
		public void AddSearchPaths (params string [] paths) {
			Context.SearchPath.AddRange (paths);
		}

		/// <summary>
		/// Compile source code to an Iodine module.
		/// </summary>
		/// <returns>The source.</returns>
		/// <param name="source">Source.</param>
		public IodineModule CompileSource (string source) {

			// Create a source unit from the source string
			var unit = Iodine.Compiler.SourceUnit.CreateFromSource (source);

			// Compile the source unit
			return unit.Compile (Context);
		}

		/// <summary>
		/// Invoke an Iodine module.
		/// </summary>
		/// <returns>The result of the invocation.</returns>
		/// <param name="module">Module.</param>
		public IodineObject InvokeModule (IodineModule module) {

			// Invoke the module
			return Context.Invoke (module, new IodineObject [0]);
		}

		/// <summary>
		/// Compile and invoke source code and return the resulting object or null.
		/// </summary>
		/// <returns>The and invoke or null.</returns>
		/// <param name="source">Source.</param>
		public IodineObject CompileAndInvokeOrNull (string source) {

			// Try compiling and invoking some source
			try {
				return InvokeModule (CompileSource (source));
			} catch {
				return null;
			}
		}
	}
}
