using LsbStego.Exceptions;
using LsbStego.StegoLogic.DataStructures;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace LsbStego.StegoLogic {
	internal class Scanner {

		#region Singleton definition
		private static Scanner instance;

		private Scanner() {

		}

		public static Scanner Instance {
			get	{
				if (instance == null) {
					instance = new Scanner();
				}
				return instance;
			}
		}
		#endregion

		#region Methods for detecting and rating carriers
		/// <summary>
		/// Scans all available cariers in a specified directory and returns them in a FileInfo array
		/// </summary>
		/// <param name="path"></param>
		/// <param name="message"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <returns></returns>
		public FileInfo[] ScanCarriers(string path, byte[] message) {
			string[] scannedExtensions = new[] { ".bmp", ".png" };
			DirectoryInfo dinfo = new DirectoryInfo(path);
			FileInfo[] files =
				dinfo.EnumerateFiles()
					 .Where(f => scannedExtensions.Contains(f.Extension.ToLower()))
					 .ToArray();
			return files;
		}

		/// <summary>
		/// Gets the dimensions of a specific carrier image
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public string getDimensionsString(Bitmap carrierImage) {
			StringBuilder sb = new StringBuilder();
			sb.Append(carrierImage.Width);
			sb.Append(" x ");
			sb.Append(carrierImage.Height);
			return sb.ToString();
		}

		/// <summary>
		/// Rates a carrier for a specific message based on the Hamming distance
		/// between the message and the carrier's LSBs
		/// </summary>
		/// <param name="carrierImage"></param>
		/// <param name="message"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="MessageTooBigException"></exception>
		/// <returns></returns>
		public Rating RateCarrier(Bitmap carrierImage, Message message, string hidingPassword) {
			LsbEmbedder lsbEmbedder = LsbEmbedder.Instance;

			// Append the file name and the messageSize to the payload itself
			string completeMessage = lsbEmbedder.GenerateMessageBitPattern(message);

			// Get the size of the complete message
			uint messageSize = (uint) completeMessage.Length;

			// Calculate the capacity the carrier provides
			uint carrierCapacity = CalculateCapacity(carrierImage, "bit");

			// Calculate the Hamming distance between the message and the LSBs of the carrier
			int hammingDistance = lsbEmbedder.CalculateHammingDistance(carrierImage, completeMessage, hidingPassword);

			// Calculate relative rating
			double relativeRating = (double) (messageSize - hammingDistance) / messageSize;
			float relativeRatingPercent = (float) Math.Round((relativeRating * 100), 3);

			// Calculate absolute rating
			double absoluteRating = (double) (carrierCapacity - hammingDistance) / carrierCapacity;
			float absoluteRatingPercent = (float) Math.Round((absoluteRating * 100), 3);

			// Generate rating object
			Rating rating = new Rating(relativeRatingPercent, absoluteRatingPercent);
			return rating;
		}

		/// <summary>
		/// Calculates the hiding capacity of a given carrier and returns it
		/// in the specified unit of the SI system
		/// </summary>
		/// <param name="carrier"></param>
		/// <param name="unit"></param>
		/// <returns></returns>
		public uint CalculateCapacity(Bitmap carrier, string unit) {
			uint width = (uint) carrier.Width;
			uint height = (uint) carrier.Height;
			switch(unit) {
				case "bit":
					return (3 * width * height);
				case "byte":
					return ((3 * width * height) / 8);
				case "kB":
					return ((3 * width * height) / 8192);
				case "mB":
					return (((3 * width * height) / 8192) / 1024);
				case null:
					return (3 * width * height);
				default:
					return (3 * width * height);
			}
		}
		#endregion

		#region Methods that access the file system
		/// <summary>
		/// Read a message file and return it using an array of bytes
		/// </summary>
		/// <param name="path"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="IOException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns></returns>
		public byte[] ReadMessage(string path) {
			//var fs = new FileStream(path, FileMode.Open);
			//var messageLength = (int) fs.Length;
			//var messageBytes = new byte[messageLength];
			//fs.Read(messageBytes, 0, messageLength);
			//fs.Close();
			//return messageBytes;
			return File.ReadAllBytes(path);
		}

		/// <summary>
		/// Reads the carrier image file from the given path
		/// </summary>
		/// <param name="path"></param>
		/// <exception cref="FileNotFoundException"></exception>
		/// <returns></returns>
		private Bitmap LoadImageFile(string path) {
			return new Bitmap(path);
		}

		/// <summary>
		/// Loads an image without creating a memory-mapped file
		/// which creates a lock to the file referenced to by the path argument
		/// </summary>
		/// <param name="path"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="OutOfMemoryException"></exception>
		/// <exception cref="WrongPixelFormatException"></exception>
		/// <returns></returns>
		public Bitmap LoadImageFileWithoutLock(string path, bool forceTrueColor) {
			using (var img = Image.FromFile(path)) {
				PixelFormat pf = img.PixelFormat;
				bool wrongPixelFormat = false;
				switch(pf) {
					//case PixelFormat.Alpha:
					//	break;
					//case PixelFormat.PAlpha:
					//	break;
					//case PixelFormat.Canonical:
					//	break;
					case PixelFormat.Undefined:
						wrongPixelFormat = true;
						break;
					case PixelFormat.Indexed:
						wrongPixelFormat = true;
						break;
					case PixelFormat.Format8bppIndexed:
						wrongPixelFormat = true;
						break;
					case PixelFormat.Format4bppIndexed:
						wrongPixelFormat = true;
						break;
					case PixelFormat.Format1bppIndexed:
						wrongPixelFormat = true;
						break;
					default:
						break;
				}

				if (wrongPixelFormat) {
					if (forceTrueColor) {
						Bitmap bp = new Bitmap(img);
						Bitmap newImg = new Bitmap(img.Width, img.Height);
						newImg = bp;
						return newImg;
					} else {
						throw new WrongPixelFormatException();
					}
				} else {
					return new Bitmap(img);
				}
			} // using
		} // method
		#endregion
	} // class
} // namespace