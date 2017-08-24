namespace iox {
	using System.Linq;
	using Iodine.Runtime;
	using static ANSIColor;

	public class IoxExtension : IodineModule {

		static readonly string __DOC__ = $@"
		The Iodine shell manager.
		Use the `help (object)` function to get information about any object.
		".Trim ().Split ('\n').Select (s => s.Trim ()).Aggregate ((a, b) => $"{a}\n{b}");

		public IoxExtension () : base ("iox") {
			ExistsInGlobalNamespace = true;
			SetAttribute ("__doc__", new IodineString (__DOC__));
			SetAttribute ("help", new BuiltinMethodCallback (help, this));
		}

		IodineObject help (VirtualMachine vm, IodineObject self, IodineObject[] args) {

			// Test argument count
			if (args.Length == 0) {
				ANSI.WriteLine ("Please pass an object to the help function!");
				return IodineNull.Instance;
			}

			// Get the target object
			var target = args [0];

			// Test __doc__ attribute existence
			if (!target.HasAttribute ("__doc__") || !(target.GetAttribute (vm, "__doc__") is IodineString)) {
				ANSI.WriteLine ($"The specified {White}{target.TypeDef.Name}{Default} does not provide any documentation :(");
				return IodineNull.Instance;
			}

			// Write documentation
			ANSI.WriteLine (((IodineString) target.GetAttribute (vm, "__doc__")).Value);
			return IodineNull.Instance;
		}
	}
}