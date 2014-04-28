using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace yERD.db {
	public class Table : IEquatable<Table> {
		/// <summary>
		/// The name of the Table
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// The name of the schema that this table belongs to
		/// </summary>
		public Schema Schema { get; protected set; }

		string _qualifiedName = null;
		/// <summary>
		/// The full schema.tablename
		/// </summary>
		public string QualifiedName {
			get {
				if (_qualifiedName == null) {
					_qualifiedName = Schema.Name + "." + Name;
				}

				return _qualifiedName;
			}
		}

		/// <summary>
		/// All of the table's attributes (columns)
		/// </summary>
		public IEnumerable<TableColumn> Attributes { get; protected set; }

		/// <summary>
		/// Indices that belong to this table
		/// </summary>
		public IEnumerable<TableIndex> Indices { get; protected set; }

		public Table(string name, Schema schema, IEnumerable<TableColumn> attributes, IEnumerable<TableIndex> indices) {
			if (string.IsNullOrEmpty(name)) {
				throw new ArgumentNullException("name");
			} else if (attributes == null) {
				throw new ArgumentNullException("attributes");
			} else if (attributes.FirstOrDefault() == null) {
				throw new ArgumentException("A table must have at least one attribute associated.", "attributes");
			} else if (schema == null) {
				throw new ArgumentNullException("schema", "A table must belong to a schema.");
			}

			Name = name;
			Schema = schema;
			Attributes = attributes.OrderByDescending(a => a.IsPartOfPrimaryKey);
			Indices = indices;

			//Assign nicknames!
			int UKCount = 1;
			int IXCount = 1;
			foreach (TableIndex ti in indices.OrderBy(i => i.Id)) {
				if (ti.IsPrimaryKey) {
					ti.IndexNumber = 1;
					ti.NickName = "PK";
				} else if (ti.IsUniqueConstraint || ti.IsUnique) {
					ti.IndexNumber = UKCount++;
					ti.NickName = "U" + ti.IndexNumber;
				} else {
					ti.IndexNumber = IXCount++;
					ti.NickName = "I" + ti.IndexNumber;
				}
			}
		}

		public override bool Equals(object obj) {
			return Equals(obj as Table);
		}

		public override string ToString() {
			return this.QualifiedName;
		}

		public override int GetHashCode() {
			return this.QualifiedName.GetHashCode();
		}

		public bool Equals(Table other) {
			if (other == null) return false;

			return this.QualifiedName == other.QualifiedName;
		}
	}
}
