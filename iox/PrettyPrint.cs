namespace iox {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Iodine.Runtime;
	using static ANSIColor;

	/// <summary>
	/// Pretty print.
	/// </summary>
	public static class PrettyPrint {

		// TODO: Write formatters for more types

		/// <summary>
		/// The color scheme.
		/// Inspired by the node repl.
		/// </summary>
		static readonly Dictionary<Type, ANSIColor> ColorScheme
		= new Dictionary<Type, ANSIColor> {
			[typeof (IodineNull)] = DarkGray,
			[typeof (IodineBool)] = DarkYellow,
			[typeof (IodineFloat)] = DarkYellow,
			[typeof (IodineBytes)] = Cyan,
			[typeof (IodineBigInt)] = DarkYellow,
			[typeof (IodineString)] = DarkGreen,
			[typeof (IodineInteger)] = DarkYellow,
			[typeof (IodineException)] = Red,
		};

		/// <summary>
		/// The formatters.
		/// </summary>
		static readonly Dictionary<Type, Func<IodineObject, string, string, string>> Formatters
		= new Dictionary<Type, Func<IodineObject, string, string, string>> {
			[typeof (IodineNull)] = IodineNullFormatter,
			[typeof (IodineBool)] = IodineBoolFormatter,
			[typeof (IodineList)] = IodineListFormatter,
			[typeof (IodineTuple)] = IodineTupleFormatter,
			[typeof (IodineRange)] = IodineRangeFormatter,
			[typeof (IodineBytes)] = IodineBytesFormatter,
			[typeof (IodineString)] = IodineStringFormatter,
			[typeof (IodineException)] = IodineExceptionFormatter,
			[typeof (IodineDictionary)] = IodineDictionaryFormatter,
		};

		/// <summary>
		/// Pretty print the specified object.
		/// </summary>
		/// <param name="obj">Object.</param>
		public static void Write (IodineObject obj) {
			ANSI.Write (Format (obj));
		}

		/// <summary>
		/// Pretty print the specified object.
		/// </summary>
		/// <param name="obj">Object.</param>
		public static void WriteLine (IodineObject obj) {
			ANSI.WriteLine (Format (obj));
		}

		/// <summary>
		/// Pretty format the specified object.
		/// </summary>
		/// <returns>The format.</returns>
		/// <param name="obj">Object.</param>
		public static string Format (IodineObject obj) {

			// Get the type of the object
			var type = obj.GetType ();

			// Exceptions are special snowflakes
			if (obj.Base?.GetType ().Name == "IodineException") {
				type = obj.Base.GetType ();
			}

			// Try getting the formatting color
			ANSIColor color;
			ColorScheme.TryGetValue (type, out color);

			// Test if the object has a custom formatter
			if (Formatters.ContainsKey (type)) {

				// Invoke the formatter
				return ANSI.Sanitize (Formatters [type] (obj, color?.fg ?? string.Empty, $"{ANSI.ESC}[0m"));
			}

			// Oops, there's no custom formatter for that object
			return ANSI.Sanitize ($"{color ?? White}{obj}");
		}

		static string IodineNullFormatter (IodineObject target, string start, string stop)
		=> $"{start}null{stop}";

		static string IodineStringFormatter (IodineObject target, string start, string stop)
		=> $"{start}'{((IodineString) target).Value}'{stop}";

		static string IodineBoolFormatter (IodineObject target, string start, string stop)
		=> $"{start}{((IodineBool) target).Value.ToString ().ToLowerInvariant ()}{stop}";

		static string IodineListFormatter (IodineObject target, string start, string stop)
		=> $"[ {string.Join ($", ", ((IodineList) target).Objects.Select (obj => Format (obj)))} ]";

		static string IodineTupleFormatter (IodineObject target, string start, string stop) {
			var tuple = (IodineTuple) target;
			return $"( {string.Join ($", ", tuple.Objects.Select (obj => Format (obj)))} )";
		}

		static string IodineBytesFormatter (IodineObject target, string start, string stop) {
			var bytes = (IodineBytes) target;
			var byteList = new IodineList (bytes.Value.Select (b => new IodineInteger (b)).ToList<IodineObject> ());
			return $"{start}{bytes.Value.LongLength}b{stop} {Format (byteList)}";
		}

		static string IodineDictionaryFormatter (IodineObject target, string start, string stop) {
			var dict = (IodineDictionary) target;
			return $"{{ {string.Join (", ", dict.Keys.Select (key => $"{Format (key)} : {Format (dict.Get (key))}"))} }}";
		}

		static string IodineExceptionFormatter (IodineObject target, string start, string stop) {
			var exception = (IodineException) target;
			var exceptionName = exception.TypeDef.Name;
			return $"{start}{exceptionName}{stop}: {exception.Message}";
		}

		static string IodineRangeFormatter (IodineObject target, string start, string stop) {
			var range = (IodineRange) target;
			var rangeFields = range.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
			var rangeStart = new IodineInteger ((long) rangeFields.First (field => field.Name == "min").GetValue (range));
			var rangeStop = new IodineInteger ((long) rangeFields.First (field => field.Name == "end").GetValue (range));
			var rangeStep = new IodineInteger ((long) rangeFields.First (field => field.Name == "step").GetValue (range));
			return $"Range (start: {Format (rangeStart)} stop: {Format (rangeStop)} step: {Format (rangeStep)})";
		}
	}
}
