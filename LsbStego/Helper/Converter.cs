using System;
using System.Text;
using LsbStego.Exceptions;

namespace LsbStego.Helper {
	internal static class Converter {

		#region Conversions into binary string patterns
		/// <summary>
		/// Convert a text string to a binary string pattern
		/// </summary>
		/// <param name="str"></param>
		/// <param name="zeroPadding"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="EncoderFallbackException"></exception>
		/// <exception cref="MessageNameTooBigException"></exception>
		/// <returns></returns>
		public static string StringToBinary(string str, int zeroPadding) {
			UTF8Encoding encoding = new UTF8Encoding();
			byte[] buffer = encoding.GetBytes(str);
			if (buffer.Length > 64) {
				throw new MessageNameTooBigException();
			} else if (buffer.Length < 64) {
				Array.Resize<byte>(ref buffer, zeroPadding);
			}
			StringBuilder sb = new StringBuilder();
			foreach (byte element in buffer) {
				sb.Append(Convert.ToString(element, 2).PadLeft(8, '0'));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Converts an int object to it's binary string representation and fills it with a
		/// specified amount of zeros to the left
		/// </summary>
		/// <param name="decimalNumber"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public static string DecimalToBinary(int decimalNumber, int zeroPadding) {
			if (zeroPadding != 0) {
				return Convert.ToString(decimalNumber, 2).PadLeft(zeroPadding, '0');
			} else {
				return Convert.ToString(decimalNumber, 2);
			}
		}

		/// <summary>
		/// Converts a uint object to it's binary string representation and fills it with a
		/// specified amount of zeros to the left
		/// </summary>
		/// <param name="decimalNumber"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public static string DecimalToBinary(uint decimalNumber, int zeroPadding) {
			if (zeroPadding != 0) {
				return Convert.ToString(decimalNumber, 2).PadLeft(zeroPadding, '0');
			} else {
				return Convert.ToString(decimalNumber, 2);
			}
		}

		/// <summary>
		/// Converts a byte object to it's binary string representation and fills it with a
		/// specified amount of zeros to the left
		/// </summary>
		/// <param name="decimalNumber"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public static string DecimalToBinary(Byte decimalNumber, int zeroPadding) {
			if (zeroPadding != 0) {
				return Convert.ToString(decimalNumber, 2).PadLeft(zeroPadding, '0');
			} else {
				return Convert.ToString(decimalNumber, 2);
			}
		}

		/// <summary>
		/// Converts a byte array to it's binary string representation
		/// </summary>
		/// <param name="decimalNumber"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public static string ByteArrayToBinary(Byte[] array) {
			StringBuilder sb = new StringBuilder();
			foreach (byte by in array) {
				sb.Append(DecimalToBinary(by, 8));
			}
			return sb.ToString();
		}
		#endregion


		#region Conversions from binary representations into variables or objects
		/// <summary>
		/// Converts a binary string pattern into its respective UTF8 text representation
		/// </summary>
		/// <param name="str"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="DecoderFallbackException"></exception>
		/// <returns></returns>
		public static string BinaryToString(string str) {
			byte[] temp = Extensions.ConvertBitstringToByteArray(str);
			string val = Encoding.UTF8.GetString(temp);
			return val;
		}

		/// <summary>
		/// Converts a binary string pattern to a byte variable
		/// </summary>
		/// <param name="binary"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static byte BinaryToByte(string binary) {
			byte by = Convert.ToByte(binary.ToString(), 2);
			return by;
		}

		/// <summary>
		/// Calls an extension method to convert a binary string pattern to a byte array
		/// </summary>
		/// <param name="source"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static byte[] BinaryToByteArray(string binary) {
			return Extensions.ConvertBitstringToByteArray(binary);
		}

		/// <summary>
		/// Converts a binary string pattern to a uint variable
		/// </summary>
		/// <param name="binary"></param>
		/// /// <exception cref="ArgumentException"></exception>
		/// /// <exception cref="ArgumentOutOfRangeException"></exception>
		/// /// <exception cref="FormatException"></exception>
		/// /// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static uint BinaryToUint(string binary) {
			uint unsignedInt = Convert.ToUInt32(binary, 2);
			return unsignedInt;
		}
		#endregion


		/// <summary>
		/// Converts a uint variable storing a bit value
		/// into a double variable storing the value in kB
		/// </summary>
		/// <param name="bits"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <returns></returns>
		public static double BitsToKiloBytes(uint bits) {
			float bytes = bits / 8;
			float kiloBytes = bytes / 1024;
			return Math.Round(kiloBytes, 5);
		}
	}
}
