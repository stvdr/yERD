using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using yERD.db;

namespace yERD.Printing {
	//Retrieve only tables that are descendents of a specific root table
	public class RootTableFilter : ITableFilter {
		HashSet<Table> _visited = new HashSet<Table>();

		public RootTableFilter(Database db, Table root) {
			if (root == null) {
				throw new ArgumentNullException("root");
			}

			Queue<Table> q = new Queue<Table>();
			q.Enqueue(root);
			while (q.Count > 0) {
				Table t = q.Dequeue();
				_visited.Add(t);
				var adj = db.GetRelationshipsFrom(t);
				foreach (var a in adj) {
					if (_visited.Add(a.From)) {
						q.Enqueue(a.From);
					}
				}
			}
		}

		public IEnumerable<Table> GetTables() {
			return _visited;
		}
	}
}
