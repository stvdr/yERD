using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	public interface ITableIndex {
		string Name { get; }
		bool IsUnique { get; }
		bool IsUniqueConstraint { get; }
		bool IsPrimaryKey { get; }
	}

	public class TableIndex : ITableIndex {
		public string Name { get; protected set; }
		public bool IsUnique { get; protected set; }
		public bool IsUniqueConstraint { get; protected set; }
		public bool IsPrimaryKey { get; protected set; }

		public int Id { get; protected set; }
		public int IndexNumber { get; set; }
		public string NickName { get; set; }
		public int Cardinality { get; protected set; }

		public IEnumerable<TableColumn> AttachedAttributes { get; set; }

		public TableIndex(int id, string name, bool isUnique, bool isUniqueConstraint, bool isPrimaryKey, int cardinality) {
			Id = id;
			Name = name;
			IsUnique = isUnique;
			IsUniqueConstraint = isUniqueConstraint;
			IsPrimaryKey = isPrimaryKey;
			Cardinality = cardinality;
		}
	}
}
