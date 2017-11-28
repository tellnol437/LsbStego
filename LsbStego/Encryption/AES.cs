using System.Text;

namespace LsbStego.Encryption {

	using System;
	using System.IO;
	using System.Security.Cryptography;

	/// <summary>
	/// This class provides static methods for encrypting strings with supplied passwords
	/// The class and its methods were taken from a code project found at
	/// https://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
	/// although thry have been modified.
	/// </summary>
	public static class AES {
		
		// Specify salt. It must be at least 8 bytes.
		private static readonly byte[] saltBytes = new byte[64] {
			0xD0, 0xA1, 0xF2, 0x0A, 0xF9, 0xA8, 0x57, 0xC0,
			0xD4, 0xBE, 0xC7, 0x8B, 0xEE, 0x91, 0x4D, 0xB6,
			0x98, 0xAE, 0xA3, 0x2D, 0xD8, 0x23, 0x33, 0x38,
			0x30, 0x61, 0xC6, 0x42, 0x2C, 0x46, 0xE9, 0x8E,
			0xF8, 0x29, 0x67, 0xFA, 0x0A, 0xBC, 0xD0, 0x11,
			0x8E, 0x22, 0x54, 0xF2, 0x81, 0xBB, 0xDB, 0x9F,
			0x09, 0x11, 0x86, 0x23, 0x7B, 0x26, 0x57, 0xC4,
			0x0C, 0xFA, 0x66, 0xE7, 0x9D, 0xB2, 0x41, 0xC1
		};

		/// <summary>
		/// Callback method which uses the managed Rijndael class
		/// to encrypt arrays of bytes with a given password as array of bytes
		/// </summary>
		/// <param name="bytesToBeEncrypted"></param>
		/// <param name="passwordBytes"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		private static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes) {
			byte[] encryptedBytes = null;

			using (MemoryStream ms = new MemoryStream()) {
				using (RijndaelManaged AES = new RijndaelManaged()) {

					// Set the Rijndael parameters
					AES.KeySize = 256;
					AES.BlockSize = 128;

					var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
					AES.Key = key.GetBytes(AES.KeySize / 8);
					AES.IV = key.GetBytes(AES.BlockSize / 8);

					// Set the AES mode of operation
					AES.Mode = CipherMode.CBC;

					using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write)) {
						cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
						//cs.Close();
					}
					encryptedBytes = ms.ToArray();
				}
			}
			return encryptedBytes;
		}

		/// <summary>
		/// Callback method which uses the managed Rijndael class
		/// to decrypt arrays of bytes with a given password as array of bytes
		/// </summary>
		/// <param name="bytesToBeDecrypted"></param>
		/// <param name="passwordBytes"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		private static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes) {
			byte[] decryptedBytes = null;

			using (MemoryStream ms = new MemoryStream()) {
				using (RijndaelManaged AES = new RijndaelManaged()) {

					// Set the Rijndael parameters
					AES.KeySize = 256;
					AES.BlockSize = 128;

					var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
					AES.Key = key.GetBytes(AES.KeySize / 8);
					AES.IV = key.GetBytes(AES.BlockSize / 8);

					// Set the AES mode of operation
					AES.Mode = CipherMode.CBC;

					using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write)) {
						cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
						//cs.Close();
					}
					decryptedBytes = ms.ToArray();
				}
			}
			return decryptedBytes;
		}

		/// <summary>
		/// Encrypts a text string using a given password
		/// </summary>
		/// <param name="input"></param>
		/// <param name="password"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static string EncryptText(string input, string password) {
			// Get the bytes of the string
			byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

			string result = Convert.ToBase64String(bytesEncrypted);

			return result;
		}

		/// <summary>
		/// Decrypts a text string using a given password
		/// </summary>
		/// <param name="input"></param>
		/// <param name="password"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <returns></returns>
		public static string DecryptText(string input, string password) {
			// Get the bytes of the string
			byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

			string result = Encoding.UTF8.GetString(bytesDecrypted);

			return result;
		}

		/// <summary>
		/// Encrypts a byte array using a given password
		/// </summary>
		/// <param name="input"></param>
		/// <param name="password"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns></returns>
		public static byte[] Encrypt(byte[] input, string password) {
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			// Call encryption routine and return the result
			byte[] bytesEncrypted = AES_Encrypt(input, passwordBytes);
			return bytesEncrypted;
		}

		/// <summary>
		/// Decrypts a byte array using a given password
		/// </summary>
		/// <param name="input"></param>
		/// <param name="password"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		/// <exception cref="ObjectDisposedException"></exception>
		/// <returns></returns>
		public static byte[] Decrypt(byte[] input, string password) {
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			// Call decryption routine and return the result
			byte[] bytesDecrypted = AES_Decrypt(input, passwordBytes);
			return bytesDecrypted;
		}

		/// <summary>
		/// Encrypts a file read from a source path and writes it to a destination path
		/// </summary>
		/// <param name="originalFilePath"></param>
		/// <param name="password"></param>
		/// <param name="encrptedFilePath"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="IOException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="UnauthorizedAccessException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="OverflowException"></exception>
		public static void EncryptFile(string originalFilePath, string password, string encrptedFilePath) {
			byte[] bytesToBeEncrypted = File.ReadAllBytes(originalFilePath);
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

			File.WriteAllBytes(encrptedFilePath, bytesEncrypted);
		}

		/// <summary>
		/// Decrypts a file read from a source path and writes it to a destination path
		/// </summary>
		/// <param name="encryptedFilePath"></param>
		/// <param name="password"></param>
		/// <param name="decryptedFilePath"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="CryptographicException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="OverflowException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="IOException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="UnauthorizedAccessException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		public static void DecryptFile(string encryptedFilePath, string password, string decryptedFilePath) {
			byte[] bytesToBeDecrypted = File.ReadAllBytes(encryptedFilePath);
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			// Hash the password with SHA256 and a predefined salt
			passwordBytes = Hasher.HashSha256(passwordBytes);

			byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

			File.WriteAllBytes(decryptedFilePath, bytesDecrypted);
		}
	}
}
