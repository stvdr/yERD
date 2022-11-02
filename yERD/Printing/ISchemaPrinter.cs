using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using yERD.db;

namespace yERD.Printing {
	public interface ISchemaPrinter {
		void WriteFile(string path, ITableFilter filter, bool showRelation, bool showType);
	}
}
