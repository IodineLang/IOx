namespace iox {
	using System;
	using System.Text;

	/// <summary>
	/// Console helper.
	/// </summary>
	public static class ConsoleHelper {

		/// <summary>
		/// Set the stdout encoding to UTF-8.
		/// </summary>
		public static void EnableUTF8 () {
			Console.OutputEncoding = Encoding.UTF8;
		}
	}
}
