using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	public class EntityRelationship : IEquatable<EntityRelationship> {
		public Table From { get; protected set; }
		public Table To { get; protected set; }
		public string Name { get; protected set; }
		public int Id { get; set; }

		public IList<Tuple<TableColumn, TableColumn>> Attributes {
			get;
			private set;
		}

		RelationshipType _type = RelationshipType.Unknown;
		public RelationshipType Type {
			get {
				if (_type == RelationshipType.Unknown) {
					_type = DetermineRelationshipType();
				}

				return _type;
			}
		}

		private RelationshipType DetermineRelationshipType() {
			RelationshipType t;
			if (Attributes.FirstOrDefault(c => c.Item1.IsNullable) != null) {
				//At least one of the attributes defining the relationship is nullable
				t = RelationshipType.ZeroOrMoreToZeroOrOne;
			} else {
				//None of the attributes are nullable
				t = RelationshipType.ZeroOrMoreToOneAndOnlyOne;
			}

			//for each unique index
			foreach (TableIndex index in From.Indices.Where(i => i.IsUnique == true)) {
				//if the attributes in this relationship map 1:1 into a unique index,
				//then the relationship type can only be pointing to one&only-one
				int intersectCount = index.AttachedAttributes.Intersect(Attributes.Select(a => a.Item1)).Count();
				if (intersectCount == Attributes.Count() && intersectCount == index.AttachedAttributes.Count()) {
					TableIndex u = To.Indices.FirstOrDefault(i => i.IsPrimaryKey || i.IsUnique);

					if (u != null && (index.Cardinality == To.Indices.FirstOrDefault(i => i.IsPrimaryKey || i.IsUnique).Cardinality)) {
						t = RelationshipType.OneAndOnlyOneToOneAndOnlyOne;
					} else {
						t = RelationshipType.ZeroOrOneToOneAndOnlyOne;
					}

					break;
				}
			}

			return t;
		}

		//a relationship is identifying if the entirety of the relationship
		//is contained within the primary key of the table
		bool? _isIdentifying = null;
		public bool IsIdentifying {
			get {
				if (!_isIdentifying.HasValue) {
					_isIdentifying = true;

					if (To == From) {
						_isIdentifying = false;
					} else {
						foreach (var a in Attributes.Select(at => at.Item1)) {
							if (!a.IsPartOfPrimaryKey) {
								_isIdentifying = false;
								break;
							}
						}
					}
				}

				return _isIdentifying.Value;
			}
		}

		public EntityRelationship(string name, Table from, Table to, IList<Tuple<TableColumn, TableColumn>> attributes) {
			if (from == null) {
				throw new ArgumentNullException("from");
			} else if (to == null) {
				throw new ArgumentNullException("to");
			} else if (string.IsNullOrEmpty(name)) {
				throw new ArgumentNullException("name");
			}

			From = from;
			To = to;
			Name = name;
			Attributes = attributes;
		}

		public override bool Equals(object obj) {
			return Equals(obj as EntityRelationship);
		}

		public override int GetHashCode() {
			return Name.GetHashCode();
		}

		public override string ToString() {
			return Name;
		}

		public bool Equals(EntityRelationship other) {
			if (other == null) return false;

			if (this.Name == other.Name) return true;

			return false;
		}
	}
}
