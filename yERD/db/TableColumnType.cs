using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	public class TableColumnType {
		public string TypeName { get; protected set; }
		public int MaxLength { get; protected set; }

		public TableColumnType(string typeName, int maxLength) {
			TypeName = typeName.ToLower();
			MaxLength = maxLength;
		}

		public override string ToString() {
			string s = TypeName;
			if (TypeName == "varchar" || TypeName == "nvarchar" || TypeName == "char" || TypeName == "varbinary" || TypeName == "binary" || TypeName == "nchar") {
				s += "(" + (MaxLength == -1 ? "max" : MaxLength.ToString()) + ")";
			}

			return s;
		}
	}
}
