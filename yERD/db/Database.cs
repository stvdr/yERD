using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yERD.db {
	/// <summary>
	/// A graph where nodes are database tables and edges are the relationships between the tables
	/// </summary>
	public class Database {
		public IEnumerable<Table> Tables { get; private set; }

		IList<EntityRelationship> _relationships = new List<EntityRelationship>();
		IDictionary<Table, LinkedList<EntityRelationship>> _adjacency =
			new Dictionary<Table, LinkedList<EntityRelationship>>();

		public Database(IEnumerable<Table> tables) {
			if (tables == null) {
				throw new ArgumentNullException("tables");
			}

			Tables = tables;

			foreach (var t in Tables) {
				_adjacency.Add(t, new LinkedList<EntityRelationship>());
			}
		}

		/// <summary>
		/// Get all tables and the relationship definitions referenced by table t
		/// </summary>
		public IEnumerable<EntityRelationship> GetRelationshipsFrom(Table t) {
			if (t == null) {
				throw new ArgumentNullException("t");
			}

			LinkedList<EntityRelationship> adj;
			if (!_adjacency.TryGetValue(t, out adj)) {
				throw new InvalidOperationException("The table specified does not exist in the extracted database.");
			}

			return adj;
		}

		/// <summary>
		/// Add a relationship between two tables. The relationships are directed.
		/// </summary>
		public void AddRelationship(Table from, Table to, EntityRelationship relationship) {
			if (from == null) {
				throw new ArgumentNullException("from");
			} else if (to == null) {
				throw new ArgumentNullException("to");
			} else if (relationship == null) {
				throw new ArgumentNullException("relationship");
			}

			if (!_adjacency.ContainsKey(to)) {
				throw new InvalidOperationException("The table specified as reference does not exist in the extracted database schema!");
			}

			LinkedList<EntityRelationship> adj;
			if (!_adjacency.TryGetValue(from, out adj)) {
				throw new InvalidCastException("The specified 'from' table does not exist in the extracted database schema!");
			}

			adj.AddFirst(relationship);
			_relationships.Add(relationship);
		}
	}
}
