using LsbStego.Helper;
using System;

namespace LsbStego {

	/// <summary>
	/// This class provides the main entry point and therefore can be refered to as the main class.
	/// No classes or methods have been taken directly or indirectly from other sources except the ones noted as such.
	/// </summary>
	public class Program {

		/// <summary>
		/// LsbStego's entry point. From here, hiding or extracting routines are invoked.
		/// </summary>
		/// <param name="args"></param>
		public static void Main(string[] args) {
			PrintHeader();
			ProgramExecutor pe = new ProgramExecutor();

			while (true) {
				ConsoleInterface.Write(" -- Main menu --", ConsoleColor.Gray, true);
				ConsoleInterface.Write("(1) Hide a message", ConsoleColor.Gray, true);
				ConsoleInterface.Write("(2) Extract a message", ConsoleColor.Gray, true);
				ConsoleInterface.Write("(3) Exit application", ConsoleColor.Gray, true);

				bool inputValid;
				do {
					inputValid = true;
					ConsoleInterface.Write("    : ", ConsoleColor.Gray, false);
					string choice;
					try {
						choice = Console.ReadLine();
						switch (choice) {
							case "1":
								PrintSubHeader("hiding");
								pe.startHidingRoutine();
								ConsoleInterface.WriteEmptyLine();
								break;
							case "2":
								PrintSubHeader("extracting");
								pe.startExtractionRoutine();
								ConsoleInterface.WriteEmptyLine();
								break;
							case "3":
								ConsoleInterface.Write("Exiting application properly ...", ConsoleColor.Gray, false);
								ConsoleInterface.WriteEmptyLine();
								Environment.Exit(0);
								break;
							default:
								ConsoleInterface.Write("Invalid input!", ConsoleColor.Red, true);
								inputValid = false;
								continue;
						}
					} catch (System.Security.Cryptography.CryptographicException) {
						ConsoleInterface.Write("Returning back to the main menu.", ConsoleColor.Red, true);
						ConsoleInterface.WriteEmptyLine();
						break;
						//throw;
					} catch (ArgumentException) {
						ConsoleInterface.Write("Returning back to the main menu.", ConsoleColor.Red, true);
						ConsoleInterface.WriteEmptyLine();
						break;
						//throw;
					} catch (Exception) {
						ConsoleInterface.Write("An unknown critical error has occured! ", ConsoleColor.Red, false);
						ConsoleInterface.Write("Returning back to the main menu.", ConsoleColor.Red, true);
						ConsoleInterface.WriteEmptyLine();
						//break;
						throw;
					}
				} while (!inputValid);
			}
		}
		
		#region Helper methods for the main routine
		/// <summary>
		/// Print the header
		/// </summary>
		internal static void PrintHeader() {
			//Console.BackgroundColor = ConsoleColor.Black;
			//Console.ForegroundColor = ConsoleColor.DarkYellow;

			ConsoleInterface.Write("____________________________________________________________________________________________________", ConsoleColor.DarkYellow, true);
			ConsoleInterface.Write("#####                                     --- LsbStego ---                                    ######", ConsoleColor.DarkYellow, true);
			ConsoleInterface.Write("#####         Hiding arbitrary files securely by choosing well suitable carrier images        ######", ConsoleColor.DarkYellow, true);
			ConsoleInterface.Write("#####                         based on the amount of necessary LSB changes                    ######", ConsoleColor.DarkYellow, true);
			ConsoleInterface.Write("####################################################################################################", ConsoleColor.DarkYellow, true);

			ConsoleInterface.WriteEmptyLine();
		}

		/// <summary>
		/// Print the hiding sub header
		/// </summary>
		internal static void PrintSubHeader(string type) {
			//Console.BackgroundColor = ConsoleColor.Black;
			//Console.ForegroundColor = ConsoleColor.Gray;

			ConsoleInterface.WriteUnderscoreLine(ConsoleColor.Gray);
			switch(type) {
				case "hiding":
					ConsoleInterface.Write(
						"#####                                   " +
						"-- Hiding a file --" +
						"                                    #####",
						ConsoleColor.Gray, true);
					break;
				case "extracting":
					ConsoleInterface.Write(
						"#####                                 " +
						"-- Extract a message --" +
						"                                  #####",
						ConsoleColor.Gray, true);
					break;
				default:
					break;
			}
			ConsoleInterface.WriteSharpLine(ConsoleColor.Gray);
			ConsoleInterface.WriteEmptyLine();
		}
		#endregion
	}
}
