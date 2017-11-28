using System;
using LsbStego.StegoLogic;
using LsbStego.Helper;
using System.IO;
using System.Drawing;
using LsbStego.StegoLogic.DataStructures;
using LsbStego.Exceptions;
using LsbStego.Encryption;
using System.Security.Cryptography;

namespace LsbStego {
	internal class ProgramExecutor {

		// Helper instances
		private LsbEmbedder lsbEmbedder;
		private Scanner scanner;

		// Root directory of the application
		private string rootDirectory;

		public ProgramExecutor() {
			// Assign helper instances
			lsbEmbedder = LsbEmbedder.Instance;
			scanner = Scanner.Instance;

			// Assign root directory
			rootDirectory = Directory.GetCurrentDirectory();
		}

		/// <summary>
		/// Invoke the routine to hide a message inside an image
		/// </summary>
		public void startHidingRoutine() {
			bool success;					// Variable used to check whether an operation is successful or should be repeated.
			string tempPath;				// Temporary path variable used to generate absolute paths
			string carrierDirectory = "";   // Directory storing all potential carriers
			Message message = null;         // The message object storing file name, payload and the payload size
			byte[] payload = null;          // The message's payload
			string messageName = "";        // The message's name
			string messagePath = "";        // The message's path where it is read from
			uint payloadSize = 0;           // The size of the payload in bits
			float payloadSizeKb = 0f;       // The size of the payload in kB
			uint messageSize = 0;			// The size of the complete message

			// -------------------- Section: Selecting the message -------------------------------------------------------------------------

			// Make the user select a message
			ConsoleInterface.Write("Select the message file which is to be hidden.", ConsoleColor.DarkGray, true);

			// Do this while there is no message specified
			do {
				success = true;
				ConsoleInterface.Write("File path: ", ConsoleColor.Gray, false);

				try {
					// Check whether path is relative or absolute
					tempPath = ConsoleInterface.ReadLine();
					ConsoleInterface.Write("Checking path ... ", ConsoleColor.Gray, false);
					if (IsPathRelative(tempPath)) {
						messagePath = rootDirectory + Path.DirectorySeparatorChar + tempPath;
					} else {
						messagePath = tempPath;
					}

					// Check whether there is such a file
					if (File.Exists(messagePath)) {
						ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
					} else {
						ConsoleInterface.Write("failed; could not find the file.", ConsoleColor.Red, true);
						success = false;
						continue;
					}
				} catch (Exception) {
					ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
					success = false;
					continue;
				}

				// -------------------- Section: Reading the message file -------------------------------------------------------------------------

				// Try to read the message file
				ConsoleInterface.Write("Reading message file ... ", ConsoleColor.Gray, false);
				try {
					payload = scanner.ReadMessage(messagePath);
					messageName = Path.GetFileName(messagePath);
					ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
				} catch (ArgumentException) {
					ConsoleInterface.Write("failed; no valid path has been entered.", ConsoleColor.Red, true);
					success = false;
					continue;
				} catch (FileNotFoundException) {
					ConsoleInterface.Write("failed; could not find file.", ConsoleColor.Red, true);
					success = false;
					continue;
				} catch (Exception) {
					ConsoleInterface.Write("failed; something went wrong.", ConsoleColor.Red, true);
					success = false;
					continue;
				}

				// Set the payload's size
				payloadSize = (uint) (payload.Length * 8);
				payloadSizeKb = (float) Converter.BitsToKiloBytes(payloadSize);

				// Print information on what kind of carrier is needed
				ConsoleInterface.Write("The message's size is " + payloadSizeKb + " kB. ", ConsoleColor.Gray, false);
				if (payloadSizeKb <= 16) {
					ConsoleInterface.Write("It can be hidden in nearly every image.", ConsoleColor.Gray, true);
				} else if (payloadSizeKb <= 64) {
					ConsoleInterface.Write("A small image should be enough to hide it.", ConsoleColor.Gray, true);
				} else if (payloadSizeKb <= 96) {
					ConsoleInterface.Write("A medium-sized image should be enough to hide it.", ConsoleColor.Gray, true);
				} else if (payloadSizeKb <= 512) {
					ConsoleInterface.Write("A large image is necessary to hide it.", ConsoleColor.Gray, true);
				} else if (payloadSizeKb <= 1024) {
					ConsoleInterface.Write("A very large image is necessary to hide it.", ConsoleColor.Gray, true);
				} else {
					ConsoleInterface.WriteEmptyLine();
					ConsoleInterface.Write("Therefore, even with large images, it cannot be hidden as it is too large.", ConsoleColor.Gray, true);
					ConsoleInterface.Write("Please compress the message before trying to hide it!", ConsoleColor.Gray, true);
					success = false;
					continue;
				}
			} while (!success);

			// -------------------- Section: Encrypting the message -------------------------------------------------------------------------

			// Ask whether the message should be encrypted before it is hidden
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Should the message be encrypted before it is hidden?", ConsoleColor.DarkGray, true);
			ConsoleInterface.Write("This operation might enlarge the message by up to 15 bytes due to the AES padding.", ConsoleColor.Gray, true);
			ConsoleInterface.Write("If yes, you will be asked to provide an encryption key.", ConsoleColor.Gray, true);

			// Check whether encryption is desired or not
			sbyte encryptionDesired = 0;
			do {
				ConsoleInterface.Write("Yes (y) / No (n): ", ConsoleColor.Gray, false);
				string encDesString = ConsoleInterface.ReadLine();
				if (encDesString.Equals("yes") || encDesString.Equals("y")) {
					encryptionDesired = 1;
				} else if (encDesString.Equals("no") || encDesString.Equals("n")) {
					encryptionDesired = -1;
				} else {
					ConsoleInterface.Write("Please answer the question with \"yes\" or \"no\"!", ConsoleColor.Red, true);
				}
			} while (encryptionDesired == 0);

			// Encrypt the message if desired
			if (encryptionDesired == 1) {
				do {
					success = true;

					// Make the user provide a key
					ConsoleInterface.Write("Please provide an encryption key.", ConsoleColor.DarkGray, true);
					ConsoleInterface.Write("Key: ", ConsoleColor.Gray, false);
					string first = ConsoleInterface.ReadPassword();
					ConsoleInterface.Write("Please re-enter your encryption key.", ConsoleColor.DarkGray, true);
					ConsoleInterface.Write("Key: ", ConsoleColor.Gray, false);
					string second = ConsoleInterface.ReadPassword();

					ConsoleInterface.Write("Checking keys ... ", ConsoleColor.Gray, false);
					if (first.Equals("")) {
						ConsoleInterface.Write("failed; a key must not be empty!", ConsoleColor.Red, true);
						//ConsoleInterface.WriteEmptyLine();
						success = false;
						continue;
					} else if (!first.Equals(second)) {
						ConsoleInterface.Write("failed; the provided keys do not match!", ConsoleColor.Red, true);
						//ConsoleInterface.WriteEmptyLine();
						success = false;
						continue;
					} else {
						ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
						ConsoleInterface.Write("Encrypting message using AES256-CBC ... ", ConsoleColor.Gray, false);
						try {
							payload = AES.Encrypt(payload, first);
							ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);

							// Reset the payload size due to padding of the AES encryption
							payloadSize = (uint) (payload.Length * 8);
							payloadSizeKb = (float) Converter.BitsToKiloBytes(payloadSize);
						} catch (Exception) {
							ConsoleInterface.Write("failed; continuing WITHOUT encryption ...", ConsoleColor.Red, true);
						} finally {
							// Set the size of the message
							messageSize = payloadSize + 536;
						}
					}
				} while (success == false);
			}
			
			// Create the message object
			message = new Message(messageName, payload, payloadSize);

			// -------------------- Section: Selecting the passphrase -------------------------------------------------------------------------

			// Ask whether a passphrase should be used to hide the message
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Should a password be used to hide the message?", ConsoleColor.DarkGray, true);
			ConsoleInterface.Write("This most likely precents visual attacks against the stego image and", ConsoleColor.Gray, true);
			ConsoleInterface.Write("ensures that only instances who know the password may extract the message.", ConsoleColor.Gray, true);

			// Check whether a passphrase is desired or not
			sbyte passwordDesired = 0;
			do {
				ConsoleInterface.Write("Yes (y) / No (n): ", ConsoleColor.Gray, false);
				string passDesString = ConsoleInterface.ReadLine();
				if (passDesString.Equals("yes") || passDesString.Equals("y")) {
					passwordDesired = 1;
				} else if (passDesString.Equals("no") || passDesString.Equals("n")) {
					passwordDesired = -1;
				} else {
					ConsoleInterface.Write("Please answer the question with \"yes\" or \"no\"!", ConsoleColor.Red, true);
				}
			} while (passwordDesired == 0);

			// Declare the hiding password and initialize it to null
			string hidingPassword = null;

			// If a passphrase is desired
			if (passwordDesired == 1) {
				do {
					success = true;

					// Make the user provide a passphrase
					ConsoleInterface.Write("Please provide a hiding password.", ConsoleColor.DarkGray, true);
					ConsoleInterface.Write("Password: ", ConsoleColor.Gray, false);
					string first = ConsoleInterface.ReadPassword();
					ConsoleInterface.Write("Please re-enter your password.", ConsoleColor.DarkGray, true);
					ConsoleInterface.Write("Password: ", ConsoleColor.Gray, false);
					string second = ConsoleInterface.ReadPassword();

					ConsoleInterface.Write("Checking passwords ... ", ConsoleColor.Gray, false);
					if (first.Equals("")) {
						ConsoleInterface.Write("failed; a password must not be empty!", ConsoleColor.Red, true);
						//ConsoleInterface.WriteEmptyLine();
						success = false;
						continue;
					} else if (!first.Equals(second)) {
						ConsoleInterface.Write("failed; the provided passwords do not match!", ConsoleColor.Red, true);
						//ConsoleInterface.WriteEmptyLine();
						success = false;
						continue;
					} else {
						ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
						hidingPassword = first;
					}
				} while (success == false);
			}

			// -------------------- Section: Carrier selection -------------------------------------------------------------------------

			// Make the user select a directory storing carrier images
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Select a directory to scan for potential carriers.", ConsoleColor.DarkGray, true);
			ConsoleInterface.Write("A carrier needs a capacity of at least " + payloadSizeKb + " kB.", ConsoleColor.Gray, true);

			// Do this while there are no carriers usable
			int usableCarriers = 0;
			do {

				// Do this while the selected path is incorrect
				do {
					success = true;
					ConsoleInterface.Write("Carrier directory path: ", ConsoleColor.Gray, false);
					tempPath = ConsoleInterface.ReadLine();
					ConsoleInterface.Write("Checking path ... ", ConsoleColor.Gray, false);

					try {
						// Check whether path is relative or absolute
						if (IsPathRelative(tempPath)) {
							carrierDirectory = rootDirectory + Path.DirectorySeparatorChar + tempPath;
						} else {
							carrierDirectory = tempPath;
						}

						// Check whether there is such a directory
						if (Directory.Exists(carrierDirectory)) {
							ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
						} else {
							ConsoleInterface.Write("failed; could not find directory.", ConsoleColor.Red, true);
							success = false;
							continue;
						}
					} catch (Exception) {
						ConsoleInterface.Write("failed; the is something wrong with the path.", ConsoleColor.Red, true);
						success = false;
						continue;
					}
				} while (!success);

				// Scan carriers
				ConsoleInterface.Write("Scanning and rating potential carrier files ... ", ConsoleColor.Gray, false);
				FileInfo[] availableCarriers;
				try {
					availableCarriers = scanner.ScanCarriers(carrierDirectory, payload);
				} catch (Exception) {
					ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
					availableCarriers = null;
					continue;
				}

				// Rate carrier(s) only if there is at least one and print the result(s)
				if (availableCarriers.Length != 0) {
					ConsoleInterface.WriteEmptyLine();
					usableCarriers = InvokeCarrierRating(availableCarriers, message, hidingPassword);

					// If there are carriers but none is usable
					if (usableCarriers <= 0) {
						ConsoleInterface.Write("No carrier is usable as the message is too big.", ConsoleColor.Gray, true);
						ConsoleInterface.Write("Please choose a new directory with larger carrier images.", ConsoleColor.DarkGray, true);
						continue;
					}
				
				// If there is no carrier in the directory
				} else {
					ConsoleInterface.Write("failed; no carriers found in the specified directory.", ConsoleColor.Red, true);
					continue;
				}
			} while (usableCarriers == 0);

			// Make a user choose a carrier
			ConsoleInterface.Write("Select the carrier file for the specific image.", ConsoleColor.DarkGray, true);
			Bitmap carrierImage = null;
			string carrierName = "";

			// Do this while no suitable carrier has been chosen by the user
			do {
				success = true;
				ConsoleInterface.Write("Carrier name: ", ConsoleColor.Gray, false);
				carrierName = ConsoleInterface.ReadLine();
				ConsoleInterface.Write("Reading carrier image ... ", ConsoleColor.Gray, false);

				// Try to read the carrier image
				try {
					carrierImage = scanner.LoadImageFileWithoutLock(carrierDirectory +
						Path.DirectorySeparatorChar + carrierName, false);
				} catch (FileNotFoundException) {
					ConsoleInterface.Write("failed; could not find the file.", ConsoleColor.Red, true);
					success = false;
					continue;
				} catch (ArgumentException) {
					ConsoleInterface.Write("failed; there is something wrong with the path.", ConsoleColor.Red, true);
					success = false;
					continue;
				} catch (WrongPixelFormatException) {
					ConsoleInterface.Write("failed; the carrier is an indexed image.", ConsoleColor.Red, true);
					ConsoleInterface.Write("Do you wish it to be transformed to a 24-bit RGB image?", ConsoleColor.DarkGray, true);

					// Continue the loop until a proper answer has been provided
					sbyte rgbDesired = 0;
					do {
						ConsoleInterface.Write("Yes (y) / No (n): ", ConsoleColor.Gray, false);
						string encDesString = ConsoleInterface.ReadLine();
						if (encDesString.Equals("yes") || encDesString.Equals("y")) {
							rgbDesired = 1;
						} else if (encDesString.Equals("no") || encDesString.Equals("n")) {
							rgbDesired = -1;
						} else {
							ConsoleInterface.Write("Answer the question with \"yes\" or \"no\"!", ConsoleColor.Red, true);
						}
					} while (rgbDesired == 0);

					if (rgbDesired == 1) {
						ConsoleInterface.Write("Converting image ... ", ConsoleColor.Gray, false);
						try {
							carrierImage = scanner.LoadImageFileWithoutLock(carrierDirectory +
								Path.DirectorySeparatorChar + carrierName, true);
						} catch (Exception) {
							ConsoleInterface.Write("failed; please choose another image.", ConsoleColor.Red, true);
							success = false;
							continue;
						}
					} else {
						success = false;
						continue;
					}
				} catch (Exception) {
					ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
					success = false;
					continue;
				}

				// If the carrier provides enough space to hide the message
				uint carrierCapacity = scanner.CalculateCapacity(carrierImage, "bits");
				if (messageSize <= carrierCapacity) {
					ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
				} else {
					ConsoleInterface.Write("failed; the carrier is not large enough.", ConsoleColor.Red, true);
					success = false;
					continue;
				}
			} while (!success);

			// -------------------- Section: Hiding the message -------------------------------------------------------------------------

			// Hide the message inside the carrier image
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Hiding message ... ", ConsoleColor.Gray, false);
			Bitmap stegoImage = null;
			try {
				stegoImage = lsbEmbedder.HideMessage(carrierImage, message, hidingPassword);
				ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
			} catch (MessageNameTooBigException) {
				ConsoleInterface.Write("failed; the message name is too big.", ConsoleColor.Red, true);
			} catch (InvalidOperationException) {
				ConsoleInterface.Write("failed; maybe the image has a wrong pixel format", ConsoleColor.Red, true);
			} catch (Exception) {
				ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
				//throw;
			}

			// -------------------- Section: Writing the message -------------------------------------------------------------------------

			// Write the stego image to the disk
			ConsoleInterface.Write("Writing stego image ... ", ConsoleColor.Gray, false);
			string stegoPath = rootDirectory +
				Path.DirectorySeparatorChar +
				"stegged_"+
				carrierName;

			try {
				lsbEmbedder.WriteStegoImage(stegoImage, stegoPath);
				ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
				ConsoleInterface.Write("The stego image has been saved to\n" + stegoPath, ConsoleColor.Gray, true);
			} catch (Exception) {
				ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
			}

			// End routine
			ConsoleInterface.WriteHyphenLine(ConsoleColor.Gray);
			return;
		}

		/// <summary>
		/// Invoke the routine to extract a hidden message from a stego image
		/// </summary>
		public void startExtractionRoutine() {
			bool success;               // Variable used to check whether an operation is successful or should be repeated.
			string tempPath;            // Temporary path variable used to generate absolute paths
			Message message;			// The message object storing file name, payload and the payload size
			Bitmap stegoImage = null;

			// Make the user select a stego image file
			ConsoleInterface.Write("Select the stego image to extract the message.", ConsoleColor.DarkGray, true);

			// Do this while the selected path is incorrect
			do {
				success = true;
				ConsoleInterface.Write("File path: ", ConsoleColor.Gray, false);
				tempPath = ConsoleInterface.ReadLine();
				ConsoleInterface.Write("Loading stego image file ... ", ConsoleColor.Gray, false);

				// Try to check the path and read the stego image
				string stegoPath;
				try {
					if (IsPathRelative(tempPath)) {
						stegoPath = rootDirectory + Path.DirectorySeparatorChar + tempPath;
					} else {
						stegoPath = tempPath;
					}

					// Read the user-selected message file
					stegoImage = scanner.LoadImageFileWithoutLock(stegoPath, false);
					ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
				} catch (ArgumentException) {
					ConsoleInterface.Write("failed; no valid path has been entered.", ConsoleColor.Red, true);
					success = false;
				} catch (FileNotFoundException) {
					ConsoleInterface.Write("failed; could not find file.", ConsoleColor.Red, true);
					success = false;
				} catch (WrongPixelFormatException) {
					ConsoleInterface.Write("failed; the image is indexed!", ConsoleColor.Red, true);

					// Get the user back to the main menu
					throw;
				} catch (Exception) {
					ConsoleInterface.Write("failed; something went wrong.", ConsoleColor.Red, true);
					success = false;
				}
			} while (!success);

			// Ask whether a password has been used
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Has the message been hidden using a password?", ConsoleColor.DarkGray, true);
			ConsoleInterface.Write("If yes and the provided password is wrong, the extraction routine may succeed or fail.", ConsoleColor.Gray, true);
			ConsoleInterface.Write("However, a definitely faulty message will be extracted.", ConsoleColor.Gray, true);
			ConsoleInterface.Write("If the message has additionally been encrypted, all decryption attempts will definitely fail.", ConsoleColor.Gray, true);

			sbyte passwordUsed = 0;
			do {
				ConsoleInterface.Write("Yes (y) / No (n): ", ConsoleColor.Gray, false);
				string encDesString = ConsoleInterface.ReadLine();
				if (encDesString.Equals("yes") || encDesString.Equals("y")) {
					passwordUsed = 1;
				} else if (encDesString.Equals("no") || encDesString.Equals("n")) {
					passwordUsed = -1;
				} else {
					ConsoleInterface.Write("Answer the question with \"yes\" or \"no\"!", ConsoleColor.Red, true);
				}
			} while (passwordUsed == 0);

			// Declare the hiding password
			string extractionPassword;

			// If a password has been used
			if (passwordUsed == 1) {
				// Make the user provide a password
				ConsoleInterface.Write("Provide the password to extract the message.", ConsoleColor.DarkGray, true);
				ConsoleInterface.Write("Password: ", ConsoleColor.Gray, false);
				extractionPassword = ConsoleInterface.ReadPassword();
			} else {
				extractionPassword = null;
			}

			// Try to extract the message from the stego image
			try {
				ConsoleInterface.Write("Extracting message ... ", ConsoleColor.Gray, false);
				message = lsbEmbedder.ExtractMessage(stegoImage, extractionPassword);
				//message = lsbEmbedder.ExtractMessage(stegoImage);
				ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
			} catch (Exception) {
				ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
				throw;
			}

			// Extract the payload from the message object
			byte[] payload = message.Payload;

			// Ask whether the message has been encrypted
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Has the message been encrypted?", ConsoleColor.DarkGray, true);
			ConsoleInterface.Write("If yes, you will be asked to provide a password.", ConsoleColor.Gray, true);

			sbyte isEncrypted = 0;
			do {
				ConsoleInterface.Write("Yes (y) / No (n): ", ConsoleColor.Gray, false);
				string encDesString = ConsoleInterface.ReadLine();
				if (encDesString.Equals("yes") || encDesString.Equals("y")) {
					isEncrypted = 1;
				} else if (encDesString.Equals("no") || encDesString.Equals("n")) {
					isEncrypted = -1;
				} else {
					ConsoleInterface.Write("Please answer the question with \"yes\" or \"no\"!", ConsoleColor.Red, true);
				}
			} while (isEncrypted == 0);

			// Decrypt message if necessary
			if (isEncrypted == 1) {
				int cryptoExceptionCounter = 0;
				do {
					success = true;
					
					// Make the user provide a key
					ConsoleInterface.Write("Please provide the key to decrypt the message.", ConsoleColor.Gray, true);
					ConsoleInterface.Write("Key: ", ConsoleColor.Gray, false);
					string key = ConsoleInterface.ReadPassword();
					ConsoleInterface.Write("Decrypting message ... ", ConsoleColor.Gray, false);

					// Try to decrypt the extracted message
					try {
						payload = AES.Decrypt(payload, key);
						ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
					} catch (CryptographicException) {
						ConsoleInterface.Write("failed; wrong key or faulty message.", ConsoleColor.Red, true);
						success = false;
						if (++cryptoExceptionCounter == 3) {
							throw;
						}
						continue;
					} catch (Exception) {
						ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
						cryptoExceptionCounter++;
						success = false;
						continue;
					}
				} while (success == false);
			}

			// Specify where the message is to be stored
			string messageName = message.Name;
			string messagePath = rootDirectory + Path.DirectorySeparatorChar + messageName;

			// Try to write the extracted message to the disk
			ConsoleInterface.WriteEmptyLine();
			ConsoleInterface.Write("Writing message file ... ", ConsoleColor.Gray, false);
			try {
				lsbEmbedder.WriteMessage(payload, messagePath);
				ConsoleInterface.Write("succeeded.", ConsoleColor.Green, true);
				ConsoleInterface.Write("The message has been saved to\n" + messagePath, ConsoleColor.Gray, true);
			} catch (ArgumentException) {
				ConsoleInterface.Write("failed; the extracted message is defect.", ConsoleColor.Red, true);
				throw;
			} catch (Exception) {
				ConsoleInterface.Write("failed; an unknown error occurred.", ConsoleColor.Red, true);
				throw;
			}

			// End routine
			ConsoleInterface.WriteHyphenLine(ConsoleColor.Gray);				
				return;
		}
		#region Helper methods
		/// <summary>
		/// Prints all the scanned carrier files with their respective rating to the console
		/// and returns the amount of usable ones.
		/// </summary>
		/// <param name="availableCarriers"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="IOException"></exception>
		private int InvokeCarrierRating(FileInfo[] availableCarriers, Message message, string hidingPassword) {
			ConsoleInterface.Write("____________________________________________________________________________________________________", ConsoleColor.Gray, true);
			ConsoleInterface.Write("|                                      Available carrier files                                     |", ConsoleColor.Gray, true);
			ConsoleInterface.Write("|--------------------------------------------------------------------------------------------------|", ConsoleColor.Gray, true);
			ConsoleInterface.Write("|   # | Name                   | Dimensions     |    Size |   Capacity |  Rating Rl  |  Rating Rc  |", ConsoleColor.Gray, true);
			ConsoleInterface.Write("|--------------------------------------------------------------------------------------------------|", ConsoleColor.Gray, true);

			int usableCarriers = 0;
			int carrierNo = 0;
			foreach (FileInfo file in availableCarriers) {
				string imageName = file.Name;
				string absoluteImagePath = file.DirectoryName + Path.DirectorySeparatorChar + imageName;
				Bitmap image = scanner.LoadImageFileWithoutLock(absoluteImagePath, true);

				// Carrier information which is going to be printed
				string carrierAbsoluteRating;
				string carrierRelativeRating;
				string carrierDimensions = scanner.getDimensionsString(image);
				int carrierSizeInKb = (int) (file.Length / 1024);
				uint carrierCapacityInKb = scanner.CalculateCapacity(image, "kB");

				try {
					Rating rating = scanner.RateCarrier(image, message, hidingPassword);
					carrierAbsoluteRating = rating.AbsoluteRating.ToString() + " %";
					carrierRelativeRating = rating.RelativeRating.ToString() + " %";
					usableCarriers++;
				} catch (MessageTooBigException) {
					carrierAbsoluteRating = "--";
					carrierRelativeRating = "--";
				}
				
				ConsoleInterface.Write(String.Format("| {0,3} | {1,-22} | {2,-14} | {3,7} | {4,10} | {5,11} | {6,11} |",
					++carrierNo, imageName, carrierDimensions, carrierSizeInKb + " kB", carrierCapacityInKb + " kB",
					carrierRelativeRating, carrierAbsoluteRating), ConsoleColor.Gray, true);
			}
			ConsoleInterface.Write("|--------------------------------------------------------------------------------------------------|", ConsoleColor.Gray, true);
			return usableCarriers;
		}

		/// <summary>
		/// Check whether a path is relative or not
		/// </summary>
		/// <param name="path"></param>
		/// <exception cref="ArgumentException">Invalid path</exception>
		/// <exception cref="System.Security.SecurityException">Invalid path</exception>
		/// <exception cref="ArgumentNullException">Invalid path</exception>
		/// <exception cref="NotSupportedException">Invalid path</exception>
		/// <exception cref="PathTooLongException">Invalid path</exception>
		/// <returns></returns>
		private bool IsPathRelative(string path) {
			if (path == Path.GetFullPath(path)) {
				return false;
			} else {
				return true;
			}
		}
		#endregion
	}
}
