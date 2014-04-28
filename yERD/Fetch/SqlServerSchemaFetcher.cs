using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using yERD.db;

namespace yERD.Fetch {
	public class SqlServerSchemaFetcher : ISchemaFetcher {
		static string _FETCH = @"
declare @tables table ( name nvarchar(max), id int, schema_id int )

insert @tables (name, id, schema_id)
select name, object_id, schema_id
from sys.tables

select c.object_id, c.column_id, c.name, t.name, c.max_length, c.is_nullable
from @tables ta join 
	sys.columns c on ta.id=c.object_id join
	sys.types t on c.system_type_id=t.system_type_id
where t.name!='sysname'
order by c.object_id

-- Grab a list of indices belonging to each table and the columns associated with each index
select distinct i.object_id parent_object_id, i.index_id, i.name, i.is_unique, i.is_unique_constraint, i.is_primary_key, si.rows
from @tables ta join
	sys.indexes i on i.object_id=ta.id join
	sys.sysindexes si on i.object_id=si.id and i.index_id=si.indid join
	sys.index_columns ic on i.object_id=ic.object_id and i.index_id=ic.index_id join
	sys.columns c on ic.object_id=c.object_id and ic.column_id=c.column_id join
	sys.types t on c.system_type_id=t.system_type_id
where t.name!='sysname'

select distinct i.object_id, i.index_id, ic.column_id
from @tables ta join
	sys.indexes i on ta.id=i.object_id join
	sys.index_columns ic on i.object_id=ic.object_id and i.index_id=ic.index_id join
	sys.columns c on ic.object_id=c.object_id and ic.column_id=c.column_id join
	sys.types t on c.system_type_id=t.system_type_id
where t.name!='sysname'

select schema_id, name, schema_id from sys.schemas

select * from @tables t

-- The column mappings belonging to foreign keys
select fk.Name, fkc.parent_column_id, fkc.referenced_column_id
from @tables ta join
	sys.foreign_keys fk on ta.id=fk.parent_object_id join
	sys.foreign_key_columns fkc on fk.object_id=fkc.constraint_object_id

-- Grab a list of foreign keys
select fk.parent_object_id, fk.referenced_object_id, fk.Name
from @tables ta join
	sys.foreign_keys fk on ta.id=fk.parent_object_id";

		public Database Fetch(string connectionString) {
			using (SqlConnection connection = new SqlConnection(connectionString)) {
				using (SqlCommand command = new SqlCommand(_FETCH, connection)) {

					connection.Open();
					using (SqlDataReader dr = command.ExecuteReader()) {

						//Group each table's set of attributes by table Id
						var attributes = dr.Cast<IDataRecord>().ToLookup(
						k => k.GetInt32(0),
						val => new TableColumn(val.GetInt32(1), val.GetString(2), new TableColumnType(val.GetString(3), val.GetInt16(4)), val.GetBoolean(5)));

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the second expected result set.");
						}

						//Group each table's set of indices by table Id
						var indices = dr.Cast<IDataRecord>().ToLookup(
							k => k.GetInt32(0),
							r => new TableIndex(r.GetInt32(1), r.GetString(2), r.GetBoolean(3), r.GetBoolean(4), r.GetBoolean(5), r.GetInt32(6)));

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the third expected result set.");
						}

						//Map our indices to attributes
						var indexColumns = dr.Cast<IDataRecord>().ToLookup(
							k => k.GetInt32(0).ToString() + "__" + k.GetInt32(1).ToString(),
							v => v.GetInt32(2));

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the fourth expected result set.");
						}

						//Fetch a dictionary of schemas present in the database
						var schemas = dr.Cast<IDataRecord>().ToDictionary(
								k => k.GetInt32(0),
								v => new Schema(v.GetString(1), v.GetInt32(2)));

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the fifth expected result set.");
						}

						//Create a dictionary that maps between objectid (SQL Server specific) and Table
						//objectId will be used below to link other information to each table
						IDictionary<int, Table> tables = dr.Cast<IDataRecord>().Select(
							(r) =>
							{
								string name = r.GetString(0);
								int objectId = r.GetInt32(1);
								var atts = attributes[objectId];
								var inds = indices[objectId];
								int schemaId = r.GetInt32(2);
								Schema schema = schemas[schemaId];

								//Get a list of the attributes connected to each index
								foreach (var ind in inds) {
									ind.AttachedAttributes = atts.Join(indexColumns[objectId.ToString() + "__" + ind.Id.ToString()],
										outerKey => outerKey.Id,
										innerKey => innerKey,
										(outer, inner) => outer);

									//Mark attributes as part of the primary key, if the index is the primary key
									if (ind.IsPrimaryKey) {
										foreach (var at in ind.AttachedAttributes) {
											at.IsPartOfPrimaryKey = true;
										}
									}
								}

								return new KeyValuePair<int, Table>(objectId, new Table(name, schema, atts, inds));
							}).ToDictionary(k => k.Key, v => v.Value);

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the sixth expected result set.");
						}

						//attribute mappings belonging to each foreign key
						var foreignKeyColumns = dr.Cast<IDataRecord>().ToLookup(k => k.GetString(0),
							v => new Tuple<int, int>(v.GetInt32(1), v.GetInt32(2)));

						if (!dr.NextResult()) {
							throw new InvalidOperationException("Could not fetch the seventh expected result set.");
						}

						//Get a list of relationships between tables (foreign keys)
						var relationships = dr.Cast<IDataRecord>().Select((r) =>
						{
							Table from = tables[r.GetInt32(0)];
							Table to = tables[r.GetInt32(1)];
							string name = r.GetString(2);

							var relationshipColumns = foreignKeyColumns[name].Select(t =>
								new Tuple<TableColumn, TableColumn>(from.Attributes.First(at => at.Id == t.Item1), to.Attributes.First(at => at.Id == t.Item2))).ToList();

							return new EntityRelationship(name, from, to, relationshipColumns);
						}).ToList();

						//Create the database and add all tables and the relationships between them
						Database db = new Database(tables.Values);
						foreach (var r in relationships) {
							db.AddRelationship(r.From, r.To, r);
						}

						return db;
					}
				}
			}
		}
	}
}
