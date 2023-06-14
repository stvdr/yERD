using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NDesk.Options;
using System.IO;
using yERD.db;
using yERD.Printing;
using yERD.Printing.yworks;

namespace yERD {
	class Program {
		static string CreateConnectionString(string datasource, string catalog) {
			string connectionString = "Application Name=yERD;Data Source={DS};Initial Catalog={IC};Integrated Security=True";
			connectionString = connectionString.Replace("{DS}", datasource);
			connectionString = connectionString.Replace("{IC}", catalog);

			return connectionString;
		}

		static void Main(string[] args) {

			string dataSource = null;
			string initialCatalog = null;
			string root = null;
			string schema = null;
			string outputFile = null;
			bool showType = true;
			string sType = "";
			bool showRelation = true;
			string sRelation = "";
			bool showHelp = false;

			var p = new OptionSet()
			{
				{ "ds=", "the {DATA SOURCE} of the database to fetch ERD from.",
						(string ds) => dataSource = ds},
					{ "ic=", "the {INITIAL CATALOG} of the database to fetch ERD from.",
						(string ic) => initialCatalog = ic},
					{"r|rootTable=", "a {ROOT TABLE} All tables output will have a descendent relation to this table.",
						(string r) => root = r},
					{"sc|schema=", "a {SCHEMA} All tables output will be members of this schema.",
						(string sc) => schema = sc},
					{"st|showType=", "{Y/N} to indicate whether or not a column displaying attribute type is shown.",
						(string st) => sType = st},
					{"sr|showRelation=", "{Y/N} to indicate whether or not PK/UX/IX/FK labels are displayed for each attribute.",
						(string sr)=>sRelation = sr},
					{"o|output=", "the {FILENAME} to output to.",
					(string o) => outputFile = o},
					{"h|help", "show help.",
						h => showHelp = (h!=null)}
			};

			List<string> extra;
			try {
				extra = p.Parse(args);
			} catch (OptionException e) {
				Console.Write("yERD: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try 'yERD --help' for more information.");
				return;
			}

			if (showHelp) {
				Console.WriteLine("Retrieve an ERD as a .graphml file!");
				p.WriteOptionDescriptions(Console.Out);
				return;
			}

			if (dataSource == null || initialCatalog == null) {
				Console.Error.WriteLine("Both a data source and initial catalog must be supplied!. Try 'yERD --help' for more information.");
				return;
			}

			if (outputFile == null) {
				Console.Error.WriteLine("An output file must be specified!. Try '-yERD --help' for more information.");
				return;
			}

			if (root != null && schema != null)
			{
				Console.Error.WriteLine("rootTable and schema are mutually exclusive");
				return;
			}

			showType = sType == "N" ? false : true;
			showRelation = sRelation == "N" ? false : true;

			Console.WriteLine("Fetching relationship data from the database...");
			var schemaFetcher = new Fetch.SqlServerSchemaFetcher();
			var connection = CreateConnectionString(dataSource, initialCatalog);
			var database = schemaFetcher.Fetch(connection);

			Console.WriteLine("Forming XML...");
			string output = Path.Combine(Directory.GetCurrentDirectory(), Path.ChangeExtension(outputFile, ".graphml"));
			Console.WriteLine("Saving: " + output);

			var printer = new YWorksSchemaPrinter(database);

			TableFilter tableFilter = new TableFilter();
			if (root != null) {
				var filterWithTable = database.Tables.FirstOrDefault(t => t.QualifiedName.ToLower() == root.ToLower());
				tableFilter.RootTableFilter(database, filterWithTable);
			}
			else if (schema != null)
			{
				tableFilter.SchemaFilter(database, schema);
			}
			printer.WriteFile(output, tableFilter, showRelation, showType);
			Console.WriteLine("File '" + output + "' saved successfully!");
		}
	}
}
