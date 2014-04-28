using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	/// <summary>
	/// An attribute belonging to a table (column)
	/// </summary>
	public class TableColumn {
		public int Id { get; protected set; }
		public string Name { get; protected set; }
		public bool IsNullable { get; protected set; }
		public bool IsPartOfPrimaryKey { get; set; }

		public TableColumnType Type { get; protected set; }

		public TableColumn(int id, string name, TableColumnType type, bool isNullable) {
			Id = id;
			Name = name;
			Type = type;
			IsNullable = isNullable;
		}
	}
}
