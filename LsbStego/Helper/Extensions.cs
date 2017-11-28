using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LsbStego.Helper {
	internal static class Extensions {

		/// <summary>
		/// Extension method used to split a string into chunks of a specified length
		/// </summary>
		/// <param name="source"></param>
		/// <param name="maxLength"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns></returns>
		public static string[] SplitToChunks(this string source, int maxLength) {
			return source
				.Where((x, i) => i % maxLength == 0)
				.Select(
					(x, i) => new string(source
						.Skip(i * maxLength)
						.Take(maxLength)
						.ToArray()))
				.ToArray();
		}

		/// <summary>
		/// Extension method used to convert an arbitrary string into an array of bytes
		/// </summary>
		/// <param name="source"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static byte[] ConvertBitstringToByteArray(this string source) {
			byte[] data =
			  Regex.Matches(source, ".{8}").Cast<Match>()
			  .Select(m => Convert.ToByte(m.Groups[0].Value, 2))
			  .ToArray();
			return data;
		}
	}
}
