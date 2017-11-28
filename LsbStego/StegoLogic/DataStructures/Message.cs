using System;

namespace LsbStego.StegoLogic.DataStructures {
	internal sealed class Message {

		private string name;
		private uint payloadSize;
		private byte[] payload;

		public string Name {
			get {
				return name;
			}

			set {
				name = value;
			}
		}

		public uint PayloadSize {
			get {
				return payloadSize;
			}

			set {
				payloadSize = value;
			}
		}

		public byte[] Payload {
			get {
				return payload;
			}

			set {
				payload = value;
			}
		}

		public Message(string name, Byte[] payload, uint payloadSize) {
			this.Name = name;
			this.Payload = payload;
			this.PayloadSize = payloadSize;
		}
	}
}
