namespace LsbStego.StegoLogic.DataStructures {

	/// <summary>
	/// A Rating object stores a relative and an absolute rating.
	/// Both values are set to -1 in case a message is bigger than
	/// the capacity of a chosen carrier
	/// </summary>
	internal sealed class Rating {
		private float relativeRating;
		private float absoluteRating;

		public float RelativeRating {
			get	{
				return relativeRating;
			}
			set	{
				relativeRating = value;
			}
		}

		public float AbsoluteRating {
			get	{
				return absoluteRating;
			}
			set {
				absoluteRating = value;
			}
		}

		public Rating(float relativeRating, float absoluteRating) {
			this.RelativeRating = relativeRating;
			this.AbsoluteRating = absoluteRating;
		}
	}
}
