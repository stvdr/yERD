using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	/// <summary>
	/// A schema contained within the database (eg: dbo)
	/// </summary>
	public class Schema : IEquatable<Schema> {
		public string Name { get; private set; }

		public Schema(string name, int id) {
			Name = name;
		}

		public bool Equals(Schema other) {
			if (other == null) return false;

			return Name == other.Name;
		}

		public override bool Equals(object obj) {
			return Equals(obj as Schema);
		}

		public override string ToString() {
			return this.Name;
		}

		public override int GetHashCode() {
			return this.Name.GetHashCode();
		}
	}
}
