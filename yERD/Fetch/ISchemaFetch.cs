using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using yERD.db;

namespace yERD.Fetch {
	public interface ISchemaFetcher {
		Database Fetch(string connectionString);
	}
}
