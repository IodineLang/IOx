namespace iox {
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Prompt.
	/// </summary>
	public class Prompt {

		/// <summary>
		/// The prompt stack.
		/// </summary>
		readonly Stack<string> Stack;

		/// <summary>
		/// The current prompt.
		/// </summary>
		string CurrentPrompt;

		/// <summary>
		/// Get the length of the prompt.
		/// </summary>
		/// <value>The length.</value>
		public int Length {
			get { return ToString ().Length; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="iox.Prompt"/> class.
		/// </summary>
		public Prompt () {
			CurrentPrompt = string.Empty;
			Stack = new Stack<string> ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="iox.Prompt"/> class.
		/// </summary>
		/// <param name="prompt">Prompt.</param>
		public Prompt (string prompt) : this () {
			CurrentPrompt = prompt;
		}

		/// <summary>
		/// Duplicate the current prompt.
		/// </summary>
		public void Dup () {
			Stack.Push (CurrentPrompt);
		}

		/// <summary>
		/// Push the specified prompt.
		/// </summary>
		/// <param name="prompt">Prompt.</param>
		public void Push (string prompt) {
			Stack.Push (CurrentPrompt);
			CurrentPrompt = prompt;
		}

		/// <summary>
		/// Restore the last prompt.
		/// </summary>
		public void Pop () {
			CurrentPrompt = Stack.Pop ();
		}

		/// <summary>
		/// Print the current prompt.
		/// </summary>
		public void Print () {
			if (CurrentPrompt != string.Empty)
				Console.Write (this);
		}

		public override string ToString () {
			return (
				CurrentPrompt.Length > 0
				? string.Format ("{0} ", CurrentPrompt)
				: CurrentPrompt
			);
		}
	}
}
