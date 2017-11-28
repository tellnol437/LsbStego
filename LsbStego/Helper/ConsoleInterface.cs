using System;

namespace LsbStego.Helper {
	internal class ConsoleInterface {

		/// <summary>
		/// Writes an arbitrary message to the console
		/// </summary>
		/// <param name="message"></param>
		/// <param name="color"></param>
		/// <param name="newLine"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		internal static void Write(string message, ConsoleColor color, bool newLine) {
			Console.ForegroundColor = color;
			switch (newLine) {
				case true:
					Console.WriteLine(message);
					break;
				case false:
					Console.Write(message);
					break;
			}
		}

		/// <summary>
		/// Writes an empty line to the console
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal static void WriteEmptyLine() {
			Console.WriteLine();
		}

		/// <summary>
		/// Writes a horizontal line with hyphens
		/// </summary>
		/// <param name="color"></param>
		internal static void WriteHyphenLine(ConsoleColor color) {
			Console.ForegroundColor = color;
			Console.WriteLine("----------------------------------------------------------------------------------------------------");
		}

		/// <summary>
		/// Writes a horizontal line with underscores
		/// </summary>
		/// <param name="color"></param>
		internal static void WriteUnderscoreLine(ConsoleColor color) {
			Console.ForegroundColor = color;
			Console.WriteLine("____________________________________________________________________________________________________");
		}

		/// <summary>
		/// Writes a horizontal line with sharps
		/// </summary>
		/// <param name="color"></param>
		internal static void WriteSharpLine(ConsoleColor color) {
			Console.ForegroundColor = color;
			Console.WriteLine("####################################################################################################");
		}

		/// <summary>
		/// Read a line from the console
		/// </summary>
		/// <returns></returns>
		internal static string ReadLine() {
			return Console.ReadLine();
		}

		/// <summary>
		/// Read a password line from the console.
		/// This differs from the ReadLine() method as
		/// all typed input is substituted with asterisks.
		/// </summary>
		/// <returns></returns>
		internal static string ReadPassword() {
			string password = "";

			// Read an arbitrary key in the console window
			ConsoleKeyInfo info = Console.ReadKey(true);

			// While the key is not enter
			while (info.Key != ConsoleKey.Enter) {

				// If the key is not backspace
				if (info.Key != ConsoleKey.Backspace) {
					Console.Write("*");
					password += info.KeyChar;

				// If the key is backspace
				} else if (info.Key == ConsoleKey.Backspace) {

					// Begin block to remove one character from the list of password characters
					if (!string.IsNullOrEmpty(password)) {

						// Get the location of the cursor
						password = password.Substring(0, password.Length - 1);

						// Move the cursor to the left by one character
						int pos = Console.CursorLeft;

						// Replace it with space
						Console.SetCursorPosition(pos - 1, Console.CursorTop);

						// Move the cursor to the left by one character again
						Console.Write(" ");
						Console.SetCursorPosition(pos - 1, Console.CursorTop);
					}
				}
				info = Console.ReadKey(true);
			}
			
			// Add a new line because the user pressed enter at the end of the password
			Console.WriteLine();
			return password;
		}
	}
}
