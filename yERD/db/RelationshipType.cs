namespace yERD.db {
	/// <summary>
	/// The type of a relationship between two database tables
	/// </summary>
	public enum RelationshipType {
		OneAndOnlyOneToOneAndOnlyOne,
		ZeroOrOneToOneAndOnlyOne,
		ZeroOrMoreToZeroOrOne,
		ZeroOrMoreToOneAndOnlyOne,
		Unknown
	}
}