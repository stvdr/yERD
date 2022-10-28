using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using yERD.db;
using yERD.Printing.yworks;

/*
 * Print the database as an ERD to a graphml file that can be viewed in yEd. 
 * [ http://graphml.graphdrawing.org/specification/schema_element.xsd.htm ]
 * [ http://www.yworks.com/xml/schema/graphml/1.0/doc/http___www.yworks.com_xml_graphml/index.html ]
 */
namespace yERD.Printing.yworks {
	public class YWorksSchemaPrinter : ISchemaPrinter {
		#region static
		public static XNamespace _yWorks = XNamespace.Get("http://www.yworks.com/xml/graphml");
		static public XNamespace Namespace {
			get {
				return _yWorks;
			}
		}

		static int _lastEdgeId = 0;
		static YWorksFontInfo _currentFont = null;
		static YWorksFontInfo _defaultFont = null;
		static IDictionary<string, YWorksFontInfo> _fonts = new Dictionary<string, YWorksFontInfo>();
		static public YWorksFontInfo GetFont(string name) {
			YWorksFontInfo f;
			if (!_fonts.TryGetValue(name, out f)) {
				return _defaultFont;
			} else {
				return f;
			}
		}

		static public void AddFont(YWorksFontInfo font) {
			if (font == null) {
				throw new ArgumentNullException("font");
			}
			_fonts.Add(font.FontFamily, font);
		}

		static public void SetCurrentFont(string name) {
			var font = GetFont(name);
			_currentFont = font;
		}

		static YWorksSchemaPrinter() {
			_defaultFont = new YWorksFontInfo("Consolas", 6.75, 18.15);
			_currentFont = _defaultFont;
			_fonts.Add("Consolas", _defaultFont);
		}
		#endregion

		Database _database;

		//node id mappings, each node needs a unique id for the graphml file
		IDictionary<string, int> tableIds = new Dictionary<string, int>();

		private int GetTableId(Table table) {
			return tableIds[table.QualifiedName];
		}

		string GetPrimaryKeyCell(TableColumn col) {
			StringBuilder sb = new StringBuilder();
			sb.Append("<b><u>");
			sb.Append(col.Name);
			sb.Append("</b></u>");
			return sb.ToString();
		}

		string GetNonNullableCell(TableColumn col) {
			StringBuilder sb = new StringBuilder();
			sb.Append("<b>");
			sb.Append(col.Name);
			sb.Append("</b>");
			return sb.ToString();
		}

		XElement CreateERDNode(Table table, bool showRelation = true, bool showType = true) {
			//The height and width of each ERD node's header
			const double HEIGHT_HEADER = 10.0;
			const double WIDTH_HEADER = 16;

			//build the HTML that will be present in each ERD node
			StringBuilder sb = new StringBuilder();
			foreach (var attr in table.Attributes) {
				sb.Append("<tr>");

				sb.Append("<td>");
				if (attr.IsPartOfPrimaryKey) {
					sb.Append(GetPrimaryKeyCell(attr));
				} else if (!attr.IsNullable) {
					sb.Append(GetNonNullableCell(attr));
				} else {
					sb.Append(attr.Name);
				}
				sb.Append("</td>");

				sb.Append("<td>");
				sb.Append(attr.Type.ToString());
				sb.Append("</td>");

				sb.Append("</tr>");
			}

			//The maximum number of characters in a row
			int width = table.Attributes.Select(a => a.Name.Length).Max() + table.Attributes.Select(a => a.Type.ToString().Length).Max();

			//Does the header have the longest string?
			width = width > table.QualifiedName.Length ? width : table.QualifiedName.Length;

			//build the ERD node as an xml element
			return new XElement(GraphML.Namespace + "node",
					new XAttribute("id", "n" + GetTableId(table)),
					new XElement(GraphML.Namespace + "data",
					new XAttribute("key", "d6"),
					new XElement(Namespace + "GenericNode",
						new XAttribute("configuration", "EntityRelationship_DetailedEntity"),
						new XElement(Namespace + "Geometry",
							new XAttribute("height", HEIGHT_HEADER + (table.Attributes.Count() + 1) * _currentFont.FontHeight),
							new XAttribute("width", WIDTH_HEADER + width * _currentFont.FontWidth)),
						new XElement(Namespace + "Fill",
							new XAttribute("color", "#E8EEF7"),
							new XAttribute("color2", "#B7C9E3"),
							new XAttribute("transparent", "false")),
						new XElement(Namespace + "NodeLabel",
							new XAttribute("fontFamily", _currentFont.FontFamily),
							new XAttribute("autoSizePolicy", "content"),
							new XAttribute("alignment", "center"),
							new XAttribute("backgroundColor", "#B7C9E3"),
							new XAttribute("configuration", "DetailedEntity_NameLabelConfiguration"),
							new XAttribute("modelName", "internal"),
							new XAttribute("modelPosition", "t"), table.QualifiedName),
						new XElement(Namespace + "NodeLabel",
							new XAttribute("fontFamily", _currentFont.FontFamily),
							new XAttribute("autoSizePolicy", "content"),
							new XAttribute("hasBackgroundColor", "false"),
							new XAttribute("alignment", "left"),
							new XAttribute("configuration", "DetailedEntity_AttributeLabelConfiguration"),
							new XAttribute("modelName", "custom"),
							new XAttribute("modelPosition", "t"), "<html><table cellpadding=\"0\" cellspacing=\"2\">" + sb.ToString() + "</table></html>",
							new XElement(Namespace + "LabelModel",
								new XElement(Namespace + "ErdAttributesNodeLabelModel")),
							new XElement(Namespace + "ModelParameter",
								new XElement(Namespace + "ErdAttributesNodeLabelModelParameter"))),
						new XElement(Namespace + "StyleProperties",
							new XElement(Namespace + "Property",
								new XAttribute("class", "java.lang.Boolean"),
								new XAttribute("name", "shadow"),
								new XAttribute("value", "true"))))));
		}

		IEnumerable<XElement> GetEdgeElements(Database db, Table table) {
			IList<XElement> elements = new List<XElement>();

			var relationships = db.GetRelationshipsFrom(table);
			foreach (var ER in relationships) {

				string sourceArrow = "none";
				string targetArrow = "none";

				//Choose endpoints of the edge based upon the 
				if (ER.Type == RelationshipType.OneAndOnlyOneToOneAndOnlyOne) {
					sourceArrow = "crows_foot_one_mandatory";
					targetArrow = "crows_foot_one_mandatory";
				} else if (ER.Type == RelationshipType.ZeroOrOneToOneAndOnlyOne) {
					sourceArrow = "crows_foot_one_optional";
					targetArrow = "crows_foot_one_mandatory";
				} else if (ER.Type == RelationshipType.ZeroOrMoreToOneAndOnlyOne) {
					sourceArrow = "crows_foot_many_optional";
					targetArrow = "crows_foot_one_mandatory";
				} else if (ER.Type == RelationshipType.ZeroOrMoreToZeroOrOne) {
					sourceArrow = "crows_foot_many_optional";
					targetArrow = "crows_foot_one_optional";
				}

				elements.Add(new XElement(GraphML.Namespace + "edge",
					new XAttribute("id", "e" + ++_lastEdgeId),
					new XAttribute("source", "n" + GetTableId(ER.From)),
					new XAttribute("target", "n" + GetTableId(table)),
					new XElement(GraphML.Namespace + "data",
						new XAttribute("key", "d10"),
					new XElement(Namespace + "PolyLineEdge",
						new XElement(Namespace + "LineStyle",
							new XAttribute("type", (ER.IsIdentifying ? "solid" : "dashed"))),
						new XElement(Namespace + "Arrows",
							new XAttribute("source", sourceArrow),
							new XAttribute("target", targetArrow))))));
			}

			return elements;
		}

		public YWorksSchemaPrinter(Database database) {
			if (database == null) {
				throw new ArgumentNullException("tables");
			}

			_database = database;

			int curId = 0;
			//create unique node ids that will be specific to the graphml file
			foreach (var t in database.Tables) {
				tableIds.Add(t.QualifiedName, curId++);
			}
		}

		public void WriteFile(string path, ITableFilter filter) {
			if (string.IsNullOrEmpty(path)) {
				throw new ArgumentNullException("path");
			}

			IEnumerable<Table> tables;
			//If a filter exists, utilize only the tables that have not been filtered out
			if (filter != null) {
				tables = filter.GetTables();
			} else {
				tables = _database.Tables;
			}

			//For each table that exists in the database, create ERD XML nodes
			IEnumerable<XElement> elements = tables.Select(t => CreateERDNode(t, true, true));

			//Create edge elements for each table's relationship
			var edges = tables.Select(t => GetEdgeElements(_database, t)).ToArray();

			//Create the entire .graphml document
			XDocument doc = new XDocument(
				new XDeclaration("1.0", "utf-8", "no"),
				new XElement(GraphML.Namespace + "graphml",
					new XAttribute("xmlns", GraphML.Namespace),
					new XAttribute(XNamespace.Xmlns + "y", Namespace),
					new XComment("Created by yERD"),
					new XElement(GraphML.Namespace + "key",
						new XAttribute("for", "node"),
						new XAttribute("id", "d6"),
						new XAttribute("yfiles.type", "nodegraphics")),
						new XElement(GraphML.Namespace + "key",
						new XAttribute("for", "edge"),
						new XAttribute("id", "d10"),
						new XAttribute("yfiles.type", "edgegraphics")),
					new XElement(GraphML.Namespace + "graph",
						new XAttribute("edgedefault", "directed"),
						new XAttribute("id", "G"),
				//add all elements to the document
							elements,

							//add all edges to the document
							edges)));

			doc.Save(path);
		}
	}
}
