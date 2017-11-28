using LsbStego.Helper;
using LsbStego.StegoLogic.DataStructures;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using LsbStego.Exceptions;
using LsbStego.Encryption;

namespace LsbStego.StegoLogic {
	internal class LsbEmbedder {
		
		#region Singleton definition
		private static LsbEmbedder instance;

		private LsbEmbedder() {}

		public static LsbEmbedder Instance {
			get {
				if (instance == null) {
					instance = new LsbEmbedder();
				}
				return instance;
			}
		}
		#endregion

		#region Methods used to rate a carrier
		/// <summary>
		/// Calculates the Hamming distance between a full message (not only the payload)
		/// and the LSBs of an image and returns them as integer
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <param name="message"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="MessageTooBigException"></exception>
		/// <returns></returns>
		public int CalculateHammingDistance(Bitmap carrierImage, string completeMessage) {

			// Get all necessary information about the carrier
			uint carrierWidth = (uint) carrierImage.Width;
			uint carrierHeight = (uint) carrierImage.Height;
			uint maxCarrierPixels = (carrierWidth * carrierHeight);
			uint capacity = (3 * maxCarrierPixels);

			// Pipe the exception handling a level higher
			if (completeMessage.Length > capacity) {
				throw new MessageTooBigException();
			}

			// Collect the LSBs of the scrambled carrier
			string carrierLsbs = CollectCarrierLsbs(carrierImage);

			// Calculate the Hamming distance
			int hammingDistance = 0;
			int lsbCounter = 0;
			foreach (char bit in completeMessage) {

				// If the index of an array reaches 2^31 it needs to be resetted
				// This is because an array is accessible only by int values
				if (lsbCounter==int.MaxValue) {
					carrierLsbs = carrierLsbs.Substring(lsbCounter);
					lsbCounter = 0;
				}

				// Increase Hamming distance if message bit and LSB do not match
				if (!Char.Equals(bit, carrierLsbs[lsbCounter])) {
					hammingDistance++;
				}
				lsbCounter++;
			}
			return hammingDistance;
		}

		/// <summary>
		/// Calculates the Hamming distance between a full message (not only the payload)
		/// and the LSBs of an image and returns them as integer
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <param name="message"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="MessageTooBigException"></exception>
		/// <returns></returns>
		public int CalculateHammingDistance(Bitmap carrierImage, string completeMessage, string password) {
			
			// If no password is specified, the default routine should be used
			if (password == null || password.Equals("")) {
				return CalculateHammingDistance(carrierImage, completeMessage);
			}

			// Get all necessary information about the carrier
			uint carrierWidth = (uint) carrierImage.Width;
			uint carrierHeight = (uint) carrierImage.Height;
			uint maxCarrierPixels = (carrierWidth * carrierHeight);
			uint capacity = (3 * maxCarrierPixels);

			// Pipe the exception handling a level higher
			if (completeMessage.Length > capacity) {
				throw new MessageTooBigException();
			}

			// Scramble image
			carrierImage = ImageScrambler.ScrambleOne(carrierImage);
			carrierImage = ImageScrambler.ScrambleThree(carrierImage);
			carrierImage = ImageScrambler.ScrambleTwo(carrierImage);

			// Transform the password into a value defining the distance between pixels
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
			passwordBytes = Hasher.HashSha256(passwordBytes);
			uint pixelDistance = (uint) ((ulong) BitConverter.ToInt64(passwordBytes, 0) % maxCarrierPixels);
			//uint pixelDistance = 0;
			//foreach (byte by in passwordBytes) {
			//	pixelDistance += by;
			//}
			//pixelDistance = (uint) (pixelDistance % maxCarrierPixels);

			// Variables
			int hammingDistance = 0;    // Current hamming distance
			Color pixel;                // Pixel object used to generate the new color
			int currentPixelX;          // x coordinate of current pixel
			int currentPixelY;          // y coordinate of current pixel
			uint currentPixel = 0;      // Variable storing the currently considered pixel
			uint restClassCounter = 0;  // Counter iterating over all rest classes
			int messageBitCounter = 0;  // Counter iterating over all message bits

			foreach (char bit in completeMessage) {

				// Get current pixel
				currentPixelX = (int) (currentPixel % carrierWidth);
				currentPixelY = (int) (currentPixel / carrierWidth);
				pixel = carrierImage.GetPixel(currentPixelX, currentPixelY);

				// Define which of the three LSBs of a pixel should be checked
				char extractedLsb = 'F';
				switch (messageBitCounter % 3) {
					case 0:
						extractedLsb = ExtractLsbAsChar(pixel.R);
						break;
					case 1:
						extractedLsb = ExtractLsbAsChar(pixel.G);
						break;
					case 2:
						extractedLsb = ExtractLsbAsChar(pixel.B);

						// Go to the next pixel
						currentPixel += pixelDistance;
						if (currentPixel >= maxCarrierPixels) {
							currentPixel = ++restClassCounter;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;

				// Increase Hamming distance if message bit and LSB do not match
				if (!Char.Equals(bit, extractedLsb)) {
					hammingDistance++;
				}
			}
			return hammingDistance;
		}
		#endregion

		#region Hiding and extracting a message
		/// <summary>
		/// Hides a given message (file) inside a given carrier (image)
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <param name="payload"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="EncoderFallbackException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="MessageNameTooBigException"></exception>
		/// <exception cref="Exception"></exception>
		/// <returns></returns>
		public Bitmap HideMessage(Bitmap carrierImage, Message message) {

			// Base variable declaration
			Scanner scanner = Scanner.Instance;
			Bitmap stegoImage = carrierImage;
			int carrierWidth = carrierImage.Width;
			int carrierHeight = carrierImage.Height;

			// Get the binary string of a message object and its length
			string completeMessage = GenerateMessageBitPattern(message);
			uint completeMessageLength = (uint) completeMessage.Length;

			// Throw exception if the message is too big
			uint carrierCapacity = scanner.CalculateCapacity(carrierImage, "bits");
			if (completeMessageLength > carrierCapacity) {
				throw new MessageTooBigException();
			}

			// Hiding variables
			Color pixel;
			int pixelX = 0;
			int pixelY = 0;
			int messageBitCounter = 0;
			byte color = 0x00;

			// While there is something left to hide
			while (messageBitCounter < completeMessageLength) {
				
				// Get current pixel
				pixel = stegoImage.GetPixel(pixelX, pixelY);

				// Define which of the three LSBs of a pixel should be used
				switch (messageBitCounter % 3) {
					case 0:
						color = InsertMessageBit(pixel.R, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(pixelX, pixelY, Color.FromArgb(color, pixel.G, pixel.B));
						break;
					case 1:
						color = InsertMessageBit(pixel.G, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(pixelX, pixelY, Color.FromArgb(pixel.R, color, pixel.B));
						break;
					case 2:
						color = InsertMessageBit(pixel.B, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(pixelX, pixelY, Color.FromArgb(pixel.R, pixel.G, color));

						// Get next pixel
						pixelX++;
						if (pixelX >= carrierWidth) {
							pixelX = 0;
							pixelY++;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}
			return stegoImage;
		}
		
		/// <summary>
		/// Hides a given message inside a given carrier (image).
		/// Thereby, a password indicates the distance between used pixels
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <param name="payload"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="EncoderFallbackException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="MessageNameTooBigException"></exception>
		/// <exception cref="Exception"></exception>
		/// <returns></returns>
		public Bitmap HideMessage(Bitmap carrierImage, Message message, string password) {

			// If no password is specified, the default routine should be used
			// because it has been well tested and should be more robust
			if (password == null || password.Equals("")) {
				return HideMessage(carrierImage, message);
			}

			// Scramble image
			carrierImage = ImageScrambler.ScrambleOne(carrierImage);
			carrierImage = ImageScrambler.ScrambleThree(carrierImage);
			carrierImage = ImageScrambler.ScrambleTwo(carrierImage);

			// Base variable declaration
			Scanner scanner = Scanner.Instance;
			Bitmap stegoImage = carrierImage;
			uint carrierWidth = (uint) carrierImage.Width;
			uint carrierHeight = (uint) carrierImage.Height;
			ulong maxCarrierPixels = (carrierWidth * carrierHeight);

			// Get the binary string of a message object and get its length
			string completeMessage = GenerateMessageBitPattern(message);
			uint completeMessageLength = (uint) completeMessage.Length;

			// Throw exception if the message is too big
			uint carrierCapacity = scanner.CalculateCapacity(carrierImage, "bits");
			if (completeMessageLength > carrierCapacity) {
				throw new MessageTooBigException();
			}

			// Transform the password into a value defining the distance between pixels
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
			passwordBytes = Hasher.HashSha256(passwordBytes);
			uint pixelDistance = (uint) ((ulong) BitConverter.ToInt64(passwordBytes, 0) % maxCarrierPixels);
			//uint pixelDistance = 1;
			//foreach (byte by in passwordBytes) {
			//	pixelDistance += (uint) (by * by);
			//	//pixelDistance += by;
			//}
			//pixelDistance = (uint) (pixelDistance % maxCarrierPixels);

			// Hiding variables
			int messageBitCounter = 0;	// Counter iterating over all message bits
			Color pixel;				// Pixel object used to generate the new color
			byte color = 0x00;			// Variable storing an RGB color value
			int currentPixelX;			// x coordinate of current pixel
			int currentPixelY;			// y coordinate of current pixel
			uint currentPixel = 0;		// Variable storing the currently considered pixel
			uint restClassCounter = 0;	// Counter iterating over all rest classes

			// While there is something left to hide
			while (messageBitCounter < completeMessageLength) {

				// Get current pixel
				currentPixelX = (int) (currentPixel % carrierWidth);
				currentPixelY = (int) (currentPixel / carrierWidth);
				pixel = stegoImage.GetPixel(currentPixelX, currentPixelY);

				// Define which of the three LSBs of a pixel should be used
				switch (messageBitCounter % 3) {
					case 0:
						color = InsertMessageBit(pixel.R, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(currentPixelX, currentPixelY, Color.FromArgb(color, pixel.G, pixel.B));
						break;
					case 1:
						color = InsertMessageBit(pixel.G, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(currentPixelX, currentPixelY, Color.FromArgb(pixel.R, color, pixel.B));
						break;
					case 2:
						color = InsertMessageBit(pixel.B, completeMessage[messageBitCounter].ToString());
						stegoImage.SetPixel(currentPixelX, currentPixelY, Color.FromArgb(pixel.R, pixel.G, color));

						// Go to the next pixel
						currentPixel += pixelDistance;
						if (currentPixel >= maxCarrierPixels) {
							currentPixel = ++restClassCounter;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}
			stegoImage = ImageScrambler.ScrambleOne(ImageScrambler.ScrambleThree(ImageScrambler.ScrambleTwo(stegoImage)));
			//stegoImage = ImageScrambler.ScrambleThree(ImageScrambler.ScrambleOne(stegoImage));
			//stegoImage = ImageScrambler.ScrambleThree(stegoImage);
			return stegoImage;
		}
		
		/// <summary>
		/// Extracts a message from a stego image and returns it as byte array
		/// </summary>
		/// <param name="stegoImage"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="Exception"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="DecoderFallbackException"></exception>
		/// <returns></returns>
		public Message ExtractMessage(Bitmap stegoImage, string password) {

			// If no password is specified, the default routine should be used
			// because it has been well tested and should be more robust
			if (password == null || password.Equals("")) {
				return ExtractMessage(stegoImage);
			}

			// Scramble image
			stegoImage = ImageScrambler.ScrambleOne(stegoImage);
			stegoImage = ImageScrambler.ScrambleThree(stegoImage);
			stegoImage = ImageScrambler.ScrambleTwo(stegoImage);

			// Base variable declaration
			StringBuilder messageNameBuilder = new StringBuilder();
			StringBuilder payloadSizeBuilder = new StringBuilder();
			StringBuilder payloadBuilder = new StringBuilder();
			int stegoWidth = stegoImage.Width;
			int stegoHeight = stegoImage.Height;
			long maxStegoPixels = (stegoWidth * stegoHeight);

			// Transform the password into a value defining the distance between pixels
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
			passwordBytes = Hasher.HashSha256(passwordBytes);
			uint pixelDistance = (uint) ((ulong) BitConverter.ToInt64(passwordBytes, 0) % (uint) maxStegoPixels);
			//uint pixelDistance = 42;
			//foreach (byte by in passwordBytes) {
			//	pixelDistance += (uint)(by*by);
			//	//pixelDistance += by;
			//}
			//pixelDistance = (uint) (pixelDistance % maxStegoPixels);

			// Variables for LSB extraction
			int messageBitCounter = 0;  // Counter iterating over all message bits
			Color pixel;				// Pixel object used to generate the new color
			int currentPixelX;          // x coordinate of current pixel
			int currentPixelY;          // y coordinate of current pixel
			uint currentPixel = 0;      // Variable storing the currently considered pixel
			uint restClassCounter = 0;  // Counter iterating over all rest classes
			uint payloadSize = 0;       // Variable indicating the size of the payload
			string messageName = "";	// String storing the name of the message
			
			// Extract the first 512 bits which encode
			// the message's payload size and the message's name
			while (messageBitCounter < 512) {

				// Get current pixel
				currentPixelX = (int) currentPixel % stegoWidth;
				currentPixelY = (int) currentPixel / stegoWidth;
				pixel = stegoImage.GetPixel(currentPixelX, currentPixelY);

				switch (messageBitCounter % 3) {
					case 0:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.B));

						// Go to the next pixel
						currentPixel += pixelDistance;
						if (currentPixel >= maxStegoPixels) {
							currentPixel = ++restClassCounter;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the message's name
			string messageNameString = messageNameBuilder.ToString();
			messageName = Converter.BinaryToString(messageNameString).Replace("\0", "");

			// Extract the payload's size
			while (messageBitCounter < 536) {

				// Get current pixel
				currentPixelX = (int) currentPixel % stegoWidth;
				currentPixelY = (int) currentPixel / stegoWidth;
				pixel = stegoImage.GetPixel(currentPixelX, currentPixelY);

				switch (messageBitCounter % 3) {
					case 0:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.B));

						// Go to the next pixel
						currentPixel += pixelDistance;
						if (currentPixel >= maxStegoPixels) {
							currentPixel = ++restClassCounter;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the payloads's size
			string payloadSizeString = payloadSizeBuilder.ToString();
			payloadSize = Converter.BinaryToUint(payloadSizeString);

			// Extract the payload
			while (messageBitCounter < payloadSize + 536) {

				// Get current pixel
				currentPixelX = (int) currentPixel % stegoWidth;
				currentPixelY = (int) currentPixel / stegoWidth;
				pixel = stegoImage.GetPixel(currentPixelX, currentPixelY);

				switch (messageBitCounter % 3) {
					case 0:
						payloadBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						payloadBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						payloadBuilder.Append(ExtractLsbAsString(pixel.B));

						// Go to the next pixel
						currentPixel += pixelDistance;
						if (currentPixel >= maxStegoPixels) {
							currentPixel = ++restClassCounter;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the message object
			string payloadString = payloadBuilder.ToString();
			byte[] payload = Extensions.ConvertBitstringToByteArray(payloadString);
			Message message = new Message(messageName, payload, payloadSize);
			return message;
		}

		/// <summary>
		/// Extracts a message from a stego image and returns it as byte array
		/// </summary>
		/// <param name="stegoImage"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="Exception"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="DecoderFallbackException"></exception>
		/// <returns></returns>
		public Message ExtractMessage(Bitmap stegoImage) {

			// Base variable declaration
			StringBuilder messageNameBuilder = new StringBuilder();
			StringBuilder payloadSizeBuilder = new StringBuilder();
			StringBuilder payloadBuilder = new StringBuilder();
			int stegoWidth = stegoImage.Width;
			int stegoHeight = stegoImage.Height;

			// Variables for LSB extraction
			Color pixel;
			int pixelX = 0;
			int pixelY = 0;
			int messageBitCounter = 0;
			uint payloadSize = 0;
			string messageName = "";

			// Extract the first 512 bits which encode
			// the message's payload size and the message's name
			while (messageBitCounter < 512) {
				pixel = stegoImage.GetPixel(pixelX, pixelY);
				switch (messageBitCounter % 3) {
					case 0:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						messageNameBuilder.Append(ExtractLsbAsString(pixel.B));
						pixelX++;
						if (pixelX >= stegoImage.Width) {
							pixelX = 0;
							pixelY++;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the message's name
			string messageNameString = messageNameBuilder.ToString();
			messageName = Converter.BinaryToString(messageNameString).Replace("\0", "");

			// Extract the payload's size
			while (messageBitCounter < 536) {
				pixel = stegoImage.GetPixel(pixelX, pixelY);
				switch (messageBitCounter % 3) {
					case 0:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						payloadSizeBuilder.Append(ExtractLsbAsString(pixel.B));
						pixelX++;
						if (pixelX >= stegoImage.Width) {
							pixelX = 0;
							pixelY++;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the payloads's size
			string payloadSizeString = payloadSizeBuilder.ToString();
			payloadSize = Converter.BinaryToUint(payloadSizeString);

			// Extract the payload
			while (messageBitCounter < payloadSize+536) {
				pixel = stegoImage.GetPixel(pixelX, pixelY);
				switch (messageBitCounter % 3) {
					case 0:
						payloadBuilder.Append(ExtractLsbAsString(pixel.R));
						break;
					case 1:
						payloadBuilder.Append(ExtractLsbAsString(pixel.G));
						break;
					case 2:
						payloadBuilder.Append(ExtractLsbAsString(pixel.B));
						pixelX++;
						if (pixelX >= stegoImage.Width) {
							pixelX = 0;
							pixelY++;
						}
						break;
					default:
						break;
				}
				messageBitCounter++;
			}

			// Compose the message object
			string payloadString = payloadBuilder.ToString();
			byte[] payload = Extensions.ConvertBitstringToByteArray(payloadString);
			Message message = new Message(messageName, payload, payloadSize);
			return message;
		}

		/// <summary>
		/// Generates a binary bitstring from a message object
		/// </summary>
		/// <param name="message"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public string GenerateMessageBitPattern(Message message) {

			// Extract data from the message object
			string messageName = message.Name;
			byte[] payload = message.Payload;
			uint payloadSize = message.PayloadSize;

			// Convert data to binary strings
			string payloadNameBinary = Converter.StringToBinary(messageName, 64);
			string payloadSizeBinary = Converter.DecimalToBinary(payloadSize, 24);
			string payloadBinary = Converter.ByteArrayToBinary(payload);

			// Generate complete message and split it to 3 bits per pixel
			StringBuilder sb = new StringBuilder();
			sb.Append(payloadNameBinary);
			sb.Append(payloadSizeBinary);
			sb.Append(payloadBinary);
			return sb.ToString();
		}

		/// <summary>
		/// Inserts a new LSB into an arbitrary byte value
		/// </summary>
		/// <param name="carrierByte"></param>
		/// <param name="messsageBit"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		private byte InsertMessageBit(byte carrierByte, string messsageBit) {
			StringBuilder sb = new StringBuilder(Converter.DecimalToBinary(carrierByte, 8));
			sb.Remove(sb.Length - 1, 1);
			sb.Append(messsageBit);
			byte stegoByte = Converter.BinaryToByte(sb.ToString());
			return stegoByte;
		}

		/// <summary>
		/// Extracts the LSB from a byte and returns it as string
		/// </summary>
		/// <param name="inputByte"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		private string ExtractLsbAsString(byte inputByte) {
			return ((inputByte % 2) == 0) ? "0" : "1";
		}

		/// <summary>
		/// Extracts the LSB from a byte and returns it as char
		/// </summary>
		/// <param name="inputByte"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <returns></returns>
		private char ExtractLsbAsChar(byte inputByte) {
			string bitPattern = Converter.DecimalToBinary(inputByte, 8);
			string lsb = bitPattern.Substring(7, 1);
			return Convert.ToChar(lsb);
		}

		/// <summary>
		/// Collects all LSBs of a given carrier image ordered from pixel
		/// (0, 0) to (xMax, yMax) and from color R over G to B and returns them as string
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public string CollectCarrierLsbs(Bitmap carrierImage) {
			StringBuilder sb = new StringBuilder();
			int width = carrierImage.Width;
			int height = carrierImage.Height;
			Color pixel;

			// Iterate over the whole image
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					pixel = carrierImage.GetPixel(x, y);
					sb.Append(ExtractLsbAsString(pixel.R));
					sb.Append(ExtractLsbAsString(pixel.G));
					sb.Append(ExtractLsbAsString(pixel.B));
				}
			}
			return sb.ToString();
		}
		#endregion

		#region Methods that access the file system
		/// <summary>
		/// Writes the stego image to the given path
		/// </summary>
		/// <param name="stegoImage"></param>
		/// <param name="path"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="IOException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="System.Runtime.InteropServices.ExternalException"></exception>
		public void WriteStegoImage(Bitmap stegoImage, string path) {

			// Retrieve the image extension from the destination path
			string extension = Path.GetExtension(path);

			// Set the image format used to write the stego file
			ImageFormat format;
			switch (extension) {
				case ".png":
					format = ImageFormat.Png;
					break;
				case ".bmp":
					format = ImageFormat.Bmp;
					break;
				case ".gif":
					format = ImageFormat.Gif;
					break;
				default:
					format = ImageFormat.Png;
					break;
			}
			
			using (FileStream fs = new FileStream(path, FileMode.Create)) {
				stegoImage.Save(fs, format);
				//fs.Dispose();
				//fs.Close();
			}
		}

		/// <summary>
		/// Writes an arbitrary message to the drive
		/// </summary>
		/// <param name="message"></param>
		/// <param name="path"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="IOException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		public void WriteMessage(byte[] message, string path) {
			//var fs = new FileStream(path, FileMode.Create);
			//fs.Write(message, 0, message.Length);
			//fs.Dispose();
			//fs.Close();
			using (var fs = new FileStream(path, FileMode.Create)) {
				fs.Write(message, 0, message.Length);
			}
		}
		#endregion
	}
}
