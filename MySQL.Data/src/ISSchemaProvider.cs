// Copyright (c) 2004, 2023, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using MySql.Data.Common;
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
  internal class ISSchemaProvider : SchemaProvider
  {
    public ISSchemaProvider(MySqlConnection connection)
      : base(connection)
    {
    }

    protected override MySqlSchemaCollection GetCollections()
    {
      MySqlSchemaCollection dt = base.GetCollections();

      object[][] collections = {
                new object[] {"Views", 2, 3},
                new object[] {"ViewColumns", 3, 4},
                new object[] {"Procedure Parameters", 5, 1},
                new object[] {"Procedures", 4, 3},
                new object[] {"Triggers", 2, 4}
            };

      FillTable(dt, collections);
      return dt;
    }

    protected override MySqlSchemaCollection GetRestrictions()
    {
      MySqlSchemaCollection dt = base.GetRestrictions();

      object[][] restrictions = new object[][]
            {
                new object[] {"Procedure Parameters", "Database", "", 0},
                new object[] {"Procedure Parameters", "Schema", "", 1},
                new object[] {"Procedure Parameters", "Name", "", 2},
                new object[] {"Procedure Parameters", "Type", "", 3},
                new object[] {"Procedure Parameters", "Parameter", "", 4},
                new object[] {"Procedures", "Database", "", 0},
                new object[] {"Procedures", "Schema", "", 1},
                new object[] {"Procedures", "Name", "", 2},
                new object[] {"Procedures", "Type", "", 3},
                new object[] {"Views", "Database", "", 0},
                new object[] {"Views", "Schema", "", 1},
                new object[] {"Views", "Table", "", 2},
                new object[] {"ViewColumns", "Database", "", 0},
                new object[] {"ViewColumns", "Schema", "", 1},
                new object[] {"ViewColumns", "Table", "", 2},
                new object[] {"ViewColumns", "Column", "", 3},
                new object[] {"Triggers", "Database", "", 0},
                new object[] {"Triggers", "Schema", "", 1},
                new object[] {"Triggers", "Name", "", 2},
                new object[] {"Triggers", "EventObjectTable", "", 3},
            };
      FillTable(dt, restrictions);
      return dt;
    }

    public override async Task<MySqlSchemaCollection> GetDatabasesAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[1];
      keys[0] = "SCHEMA_NAME";
      MySqlSchemaCollection dt = await QueryAsync("SCHEMATA", "", keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Columns[1].Name = "database_name";
      dt.Name = "Databases";
      return dt;
    }

    public override async Task<MySqlSchemaCollection> GetTablesAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      keys[3] = "TABLE_TYPE";
      MySqlSchemaCollection dt = await QueryAsync("TABLES", "TABLE_TYPE != 'VIEW'", keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Tables";
      return dt;
    }

    public override async Task<MySqlSchemaCollection> GetColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      keys[3] = "COLUMN_NAME";
      MySqlSchemaCollection dt = await QueryAsync("COLUMNS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.RemoveColumn("CHARACTER_OCTET_LENGTH");
      dt.Name = "Columns";
      QuoteDefaultValues(dt);
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetViewsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[3];
      keys[0] = "TABLE_CATALOG";
      keys[1] = "TABLE_SCHEMA";
      keys[2] = "TABLE_NAME";
      MySqlSchemaCollection dt = await QueryAsync("VIEWS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Views";
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetViewColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      StringBuilder where = new StringBuilder();
      StringBuilder sql = new StringBuilder(
          "SELECT C.* FROM information_schema.columns C");
      sql.Append(" JOIN information_schema.views V ");
      sql.Append("ON C.table_schema=V.table_schema AND C.table_name=V.table_name ");
      if (restrictions != null && restrictions.Length >= 2 &&
          restrictions[1] != null)
        where.AppendFormat(CultureInfo.InvariantCulture, "C.table_schema='{0}' ", restrictions[1]);
      if (restrictions != null && restrictions.Length >= 3 &&
          restrictions[2] != null)
      {
        if (where.Length > 0)
          where.Append("AND ");
        where.AppendFormat(CultureInfo.InvariantCulture, "C.table_name='{0}' ", restrictions[2]);
      }
      if (restrictions != null && restrictions.Length == 4 &&
          restrictions[3] != null)
      {
        if (where.Length > 0)
          where.Append("AND ");
        where.AppendFormat(CultureInfo.InvariantCulture, "C.column_name='{0}' ", restrictions[3]);
      }
      if (where.Length > 0)
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);
      MySqlSchemaCollection dt = await GetTableAsync(sql.ToString(), execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "ViewColumns";
      dt.Columns[0].Name = "VIEW_CATALOG";
      dt.Columns[1].Name = "VIEW_SCHEMA";
      dt.Columns[2].Name = "VIEW_NAME";
      QuoteDefaultValues(dt);
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetTriggersAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[4];
      keys[0] = "TRIGGER_CATALOG";
      keys[1] = "TRIGGER_SCHEMA";
      keys[2] = "EVENT_OBJECT_TABLE";
      keys[3] = "TRIGGER_NAME";
      MySqlSchemaCollection dt = await QueryAsync("TRIGGERS", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Triggers";
      return dt;
    }

    /// <summary>
    /// Return schema information about procedures and functions
    /// Restrictions supported are:
    /// schema, name, type
    /// </summary>
    /// <param name="restrictions"></param>
    /// <param name="execAsync">Boolean that indicates if the function will be executed asynchronously.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public override async Task<MySqlSchemaCollection> GetProceduresAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      try
      {
        if (connection.Settings.HasProcAccess)
          return await base.GetProceduresAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      }
      catch (MySqlException ex)
      {
        if (ex.Number == (int)MySqlErrorCode.TableAccessDenied)
          connection.Settings.HasProcAccess = false;
        else
          throw;
      }

      string[] keys = new string[4];
      keys[0] = "ROUTINE_CATALOG";
      keys[1] = "ROUTINE_SCHEMA";
      keys[2] = "ROUTINE_NAME";
      keys[3] = "ROUTINE_TYPE";

      MySqlSchemaCollection dt = await QueryAsync("ROUTINES", null, keys, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.Name = "Procedures";
      return dt;
    }

    private async Task<MySqlSchemaCollection> GetProceduresWithParametersAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = await GetProceduresAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      dt.AddColumn("ParameterList", typeof(string));

      foreach (MySqlSchemaRow row in dt.Rows)
      {
        row["ParameterList"] = await GetProcedureParameterLineAsync(row, execAsync, cancellationToken).ConfigureAwait(false);
      }
      return dt;
    }

    private async Task<string> GetProcedureParameterLineAsync(MySqlSchemaRow isRow, bool execAsync, CancellationToken cancellationToken = default)
    {
      string sql = "SHOW CREATE {0} `{1}`.`{2}`";
      sql = String.Format(sql, isRow["ROUTINE_TYPE"], isRow["ROUTINE_SCHEMA"],
          isRow["ROUTINE_NAME"]);
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false))
      {
        await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false);

        // if we are not the owner of this proc or have permissions
        // then we will get null for the body
        if (reader.IsDBNull(2)) return null;

        string sqlMode = reader.GetString(1);

        string body = reader.GetString(2);
        MySqlTokenizer tokenizer = new MySqlTokenizer(body);
        tokenizer.AnsiQuotes = sqlMode.IndexOf("ANSI_QUOTES") != -1;
        tokenizer.BackslashEscapes = sqlMode.IndexOf("NO_BACKSLASH_ESCAPES") == -1;

        string token = tokenizer.NextToken();
        while (token != "(")
          token = tokenizer.NextToken();
        int start = tokenizer.StartIndex + 1;
        token = tokenizer.NextToken();
        while (token != ")" || tokenizer.Quoted)
        {
          token = tokenizer.NextToken();
          // if we see another ( and we are not quoted then we
          // are in a size element and we need to look for the closing paren
          if (token == "(" && !tokenizer.Quoted)
          {
            while (token != ")" || tokenizer.Quoted)
              token = tokenizer.NextToken();
            token = tokenizer.NextToken();
          }
        }
        return body.Substring(start, tokenizer.StartIndex - start);
      }
    }

    private async Task<MySqlSchemaCollection> GetParametersForRoutineFromexecAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      string[] keys = new string[5];
      keys[0] = "SPECIFIC_CATALOG";
      keys[1] = "SPECIFIC_SCHEMA";
      keys[2] = "SPECIFIC_NAME";
      keys[3] = "ROUTINE_TYPE";
      keys[4] = "PARAMETER_NAME";

      StringBuilder sql = new StringBuilder(@"SELECT * FROM INFORMATION_SCHEMA.PARAMETERS");
      // now get our where clause and append it if there is one
      string where = GetWhereClause(null, keys, restrictions);
      if (!String.IsNullOrEmpty(where))
        sql.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      MySqlSchemaCollection coll = await QueryCollectionAsync("parameters", sql.ToString(), execAsync, cancellationToken).ConfigureAwait(false);

      if ((coll.Rows.Count != 0) && ((string)coll.Rows[0]["routine_type"] == "FUNCTION"))
      {
        // update missing data for the first row (function return value).
        // (using sames valus than GetParametersFromShowCreate).
        coll.Rows[0]["parameter_mode"] = "IN";
        coll.Rows[0]["parameter_name"] = "return_value"; // "FUNCTION";
      }
      return coll;
    }

    private async Task<MySqlSchemaCollection> GetParametersFromexecAsync(string[] restrictions, MySqlSchemaCollection routines, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection parms = null;

      if (routines == null || routines.Rows.Count == 0)
      {
        if (restrictions == null)
        {
          parms = await QueryCollectionAsync("parameters", "SELECT * FROM INFORMATION_SCHEMA.PARAMETERS WHERE 1=2", execAsync, cancellationToken).ConfigureAwait(false);
        }
        else
          parms = await GetParametersForRoutineFromexecAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      }
      else foreach (MySqlSchemaRow routine in routines.Rows)
        {
          if (restrictions != null && restrictions.Length >= 3)
            restrictions[2] = routine["ROUTINE_NAME"].ToString();

          parms = await GetParametersForRoutineFromexecAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        }
      parms.Name = "Procedure Parameters";
      return parms;
    }

    internal MySqlSchemaCollection CreateParametersTable()
    {
      MySqlSchemaCollection dt = new MySqlSchemaCollection("Procedure Parameters");
      dt.AddColumn("SPECIFIC_CATALOG", typeof(string));
      dt.AddColumn("SPECIFIC_SCHEMA", typeof(string));
      dt.AddColumn("SPECIFIC_NAME", typeof(string));
      dt.AddColumn("ORDINAL_POSITION", typeof(Int32));
      dt.AddColumn("PARAMETER_MODE", typeof(string));
      dt.AddColumn("PARAMETER_NAME", typeof(string));
      dt.AddColumn("DATA_TYPE", typeof(string));
      dt.AddColumn("CHARACTER_MAXIMUM_LENGTH", typeof(Int32));
      dt.AddColumn("CHARACTER_OCTET_LENGTH", typeof(Int32));
      dt.AddColumn("NUMERIC_PRECISION", typeof(byte));
      dt.AddColumn("NUMERIC_SCALE", typeof(Int32));
      dt.AddColumn("CHARACTER_SET_NAME", typeof(string));
      dt.AddColumn("COLLATION_NAME", typeof(string));
      dt.AddColumn("DTD_IDENTIFIER", typeof(string));
      dt.AddColumn("ROUTINE_TYPE", typeof(string));
      return dt;
    }

    /// <summary>
    /// Return schema information about parameters for procedures and functions
    /// Restrictions supported are:
    /// schema, name, type, parameter name
    /// </summary>
    public virtual async Task<MySqlSchemaCollection> GetProcedureParametersAsync(string[] restrictions,
        MySqlSchemaCollection routines, bool execAsync, CancellationToken cancellationToken = default)
    {
      bool is55 = connection.driver.Version.isAtLeast(5, 5, 3);

      try
      {
        // we want to avoid using IS if  we can as it is painfully slow
        MySqlSchemaCollection dt = CreateParametersTable();
        await GetParametersFromShowCreateAsync(dt, restrictions, routines, execAsync, cancellationToken).ConfigureAwait(false);
        return dt;
      }
      catch (Exception)
      {
        if (!is55) throw;

        // we get here by not having access and we are on 5.5 or later so just use IS
        return await GetParametersFromexecAsync(restrictions, routines, execAsync, cancellationToken).ConfigureAwait(false);
      }
    }

    protected override async Task<MySqlSchemaCollection> GetSchemaInternalAsync(string collection, string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection dt = await base.GetSchemaInternalAsync(collection, restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      if (dt != null)
        return dt;

      switch (collection)
      {
        case "VIEWS":
          return await GetViewsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "PROCEDURES":
          return await GetProceduresAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "PROCEDURES WITH PARAMETERS":
          return await GetProceduresWithParametersAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "PROCEDURE PARAMETERS":
          return await GetProcedureParametersAsync(restrictions, null, execAsync, cancellationToken).ConfigureAwait(false);
        case "TRIGGERS":
          return await GetTriggersAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
        case "VIEWCOLUMNS":
          return await GetViewColumnsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);
      }
      return null;
    }

    private static string GetWhereClause(string initial_where, string[] keys, string[] values)
    {
      StringBuilder where = new StringBuilder(initial_where);
      if (values != null)
      {
        for (int i = 0; i < keys.Length; i++)
        {
          if (i >= values.Length) break;
          if (values[i] == null || values[i] == String.Empty) continue;
          if (where.Length > 0)
            where.Append(" AND ");
          where.AppendFormat(CultureInfo.InvariantCulture,
              "{0} LIKE '{1}'", keys[i], values[i]);
        }
      }
      return where.ToString();
    }

    private async Task<MySqlSchemaCollection> QueryAsync(string tableName, string initialWhere, string[] keys, string[] values, bool execAsync, CancellationToken cancellationToken = default)
    {
      StringBuilder query = new StringBuilder("SELECT * FROM INFORMATION_SCHEMA.");
      query.Append(tableName);

      string where = GetWhereClause(initialWhere, keys, values);

      if (where.Length > 0)
        query.AppendFormat(CultureInfo.InvariantCulture, " WHERE {0}", where);

      if (tableName.Equals("COLUMNS", StringComparison.OrdinalIgnoreCase))
        query.Append(" ORDER BY ORDINAL_POSITION");

      return await GetTableAsync(query.ToString(), execAsync, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MySqlSchemaCollection> GetTableAsync(string sql, bool execAsync, CancellationToken cancellationToken = default)
    {
      MySqlSchemaCollection c = new MySqlSchemaCollection();
      using MySqlCommand cmd = new MySqlCommand(sql, connection);
      MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false);

      // add columns
      for (int i = 0; i < reader.FieldCount; i++)
        c.AddColumn(reader.GetName(i), reader.GetFieldType(i));

      using (reader)
      {
        while (await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false))
        {
          MySqlSchemaRow row = c.AddRow();
          for (int i = 0; i < reader.FieldCount; i++)
            row[i] = reader.GetValue(i);
        }
      }

      return c;
    }

    public override async Task<MySqlSchemaCollection> GetForeignKeysAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 1, 16))
        return await base.GetForeignKeysAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);

      string sql = @"SELECT rc.constraint_catalog, rc.constraint_schema,
                rc.constraint_name, kcu.table_catalog, kcu.table_schema, rc.table_name,
                rc.match_option, rc.update_rule, rc.delete_rule, 
                NULL as referenced_table_catalog,
                kcu.referenced_table_schema, rc.referenced_table_name 
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON 
                kcu.constraint_catalog <=> rc.constraint_catalog AND
                kcu.constraint_schema <=> rc.constraint_schema AND 
                kcu.constraint_name <=> rc.constraint_name 
                WHERE 1=1 AND kcu.ORDINAL_POSITION=1";

      StringBuilder where = new StringBuilder();
      if (restrictions.Length >= 2 && !String.IsNullOrEmpty(restrictions[1]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.constraint_schema LIKE '{0}'", restrictions[1]);
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.table_name LIKE '{0}'", restrictions[2]);
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND rc.constraint_name LIKE '{0}'", restrictions[2]);

      sql += where.ToString();

      return await GetTableAsync(sql, execAsync, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<MySqlSchemaCollection> GetForeignKeyColumnsAsync(string[] restrictions, bool execAsync, CancellationToken cancellationToken = default)
    {
      if (!connection.driver.Version.isAtLeast(5, 0, 6))
        return await base.GetForeignKeyColumnsAsync(restrictions, execAsync, cancellationToken).ConfigureAwait(false);

      string sql = @"SELECT kcu.* FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                WHERE kcu.referenced_table_name IS NOT NULL";

      StringBuilder where = new StringBuilder();
      if (restrictions.Length >= 2 && !String.IsNullOrEmpty(restrictions[1]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_schema LIKE '{0}'", restrictions[1]);
      if (restrictions.Length >= 3 && !String.IsNullOrEmpty(restrictions[2]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.table_name LIKE '{0}'", restrictions[2]);
      if (restrictions.Length >= 4 && !String.IsNullOrEmpty(restrictions[3]))
        where.AppendFormat(CultureInfo.InvariantCulture,
            " AND kcu.constraint_name LIKE '{0}'", restrictions[3]);

      sql += where.ToString();

      return await GetTableAsync(sql, execAsync, cancellationToken).ConfigureAwait(false);
    }

    #region Procedures Support Routines
    internal async Task GetParametersFromShowCreateAsync(MySqlSchemaCollection parametersTable,
        string[] restrictions, MySqlSchemaCollection routines, bool execAsync, CancellationToken cancellationToken = default)
    {
      // this allows us to pass in a pre-populated routines table
      // and avoid the querying for them again.
      // we use this when calling a procedure or function
      if (routines == null)
        routines = await GetSchemaAsync("procedures", restrictions, execAsync, cancellationToken).ConfigureAwait(false);

      MySqlCommand cmd = connection.CreateCommand();

      foreach (MySqlSchemaRow routine in routines.Rows)
      {
        string showCreateSql = String.Format("SHOW CREATE {0} `{1}`.`{2}`",
            routine["ROUTINE_TYPE"], routine["ROUTINE_SCHEMA"],
            routine["ROUTINE_NAME"]);
        cmd.CommandText = showCreateSql;
        try
        {
          string nameToRestrict = null;
          string body;

          if (restrictions != null && restrictions.Length == 5 && restrictions[4] != null)
            nameToRestrict = restrictions[4];

          using (MySqlDataReader reader = await cmd.ExecuteReaderAsync(default, execAsync, cancellationToken).ConfigureAwait(false))
          {
            await reader.ReadAsync(execAsync, cancellationToken).ConfigureAwait(false);
            body = reader.GetString(2);
          }

          await ParseProcedureBodyAsync(parametersTable, body, routine, nameToRestrict, execAsync, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Data.SqlTypes.SqlNullValueException snex)
        {
          throw new InvalidOperationException(String.Format(Resources.UnableToRetrieveParameters, routine["ROUTINE_NAME"]), snex);
        }
      }
    }

    private async Task ParseProcedureBodyAsync(MySqlSchemaCollection parametersTable, string body, MySqlSchemaRow row, string nameToRestrict, bool execAsync, CancellationToken cancellationToken = default)
    {
      List<string> modes = new List<string>(new string[3] { "IN", "OUT", "INOUT" });

      string sqlMode = row["SQL_MODE"].ToString();

      int pos = 1;
      MySqlTokenizer tokenizer = new MySqlTokenizer(body);
      tokenizer.AnsiQuotes = sqlMode.IndexOf("ANSI_QUOTES") != -1;
      tokenizer.BackslashEscapes = sqlMode.IndexOf("NO_BACKSLASH_ESCAPES") == -1;
      tokenizer.ReturnComments = false;
      string token = tokenizer.NextToken();

      // this block will scan for the opening paren while also determining
      // if this routine is a function.  If so, then we need to add a
      // parameter row for the return parameter since it is ordinal position
      // 0 and should appear first.
      while (token != "(")
      {
        if (String.Compare(token, "FUNCTION", StringComparison.OrdinalIgnoreCase) == 0 &&
            nameToRestrict == null)
        {
          parametersTable.AddRow();
          InitParameterRow(row, parametersTable.Rows[0]);
        }
        token = tokenizer.NextToken();
      }
      token = tokenizer.NextToken();  // now move to the next token past the (

      while (token != ")")
      {
        MySqlSchemaRow parmRow = parametersTable.NewRow();
        InitParameterRow(row, parmRow);
        parmRow["ORDINAL_POSITION"] = pos++;

        // handle mode and name for the parameter
        string mode = StringUtility.ToUpperInvariant(token);
        if (!tokenizer.Quoted && modes.Contains(mode))
        {
          parmRow["PARAMETER_MODE"] = mode;
          token = tokenizer.NextToken();
        }
        if (tokenizer.Quoted)
          token = token.Substring(1, token.Length - 2);
        parmRow["PARAMETER_NAME"] = token;

        // now parse data type
        token = await ParseDataTypeAsync(parmRow, tokenizer, execAsync, cancellationToken).ConfigureAwait(false);
        if (token == ",")
          token = tokenizer.NextToken();

        // now determine if we should include this row after all
        // we need to parse it before this check so we are correctly
        // positioned for the next parameter
        if (nameToRestrict == null ||
            String.Compare(parmRow["PARAMETER_NAME"].ToString(), nameToRestrict, StringComparison.OrdinalIgnoreCase) == 0)
          parametersTable.Rows.Add(parmRow);
      }

      // now parse out the return parameter if there is one.
      token = StringUtility.ToUpperInvariant(tokenizer.NextToken());
      if (String.Compare(token, "RETURNS", StringComparison.OrdinalIgnoreCase) == 0)
      {
        MySqlSchemaRow parameterRow = parametersTable.Rows[0];
        parameterRow["PARAMETER_NAME"] = "RETURN_VALUE";
        await ParseDataTypeAsync(parameterRow, tokenizer, execAsync, cancellationToken).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Initializes a new row for the procedure parameters table.
    /// </summary>
    private static void InitParameterRow(MySqlSchemaRow procedure, MySqlSchemaRow parameter)
    {
      parameter["SPECIFIC_CATALOG"] = null;
      parameter["SPECIFIC_SCHEMA"] = procedure["ROUTINE_SCHEMA"];
      parameter["SPECIFIC_NAME"] = procedure["ROUTINE_NAME"];
      parameter["PARAMETER_MODE"] = "IN";
      parameter["ORDINAL_POSITION"] = 0;
      parameter["ROUTINE_TYPE"] = procedure["ROUTINE_TYPE"];
    }

    /// <summary>
    ///  Parses out the elements of a procedure parameter data type.
    /// </summary>
    private async Task<string> ParseDataTypeAsync(MySqlSchemaRow row, MySqlTokenizer tokenizer, bool execAsync, CancellationToken cancellationToken = default)
    {
      StringBuilder dtd = new StringBuilder(
          StringUtility.ToUpperInvariant(tokenizer.NextToken()));
      row["DATA_TYPE"] = dtd.ToString();
      string type = row["DATA_TYPE"].ToString();

      string token = tokenizer.NextToken();
      if (token == "(")
      {
        token = tokenizer.ReadParenthesis();
        dtd.AppendFormat(CultureInfo.InvariantCulture, "{0}", token);
        if (type != "ENUM" && type != "SET")
          ParseDataTypeSize(row, token);
        token = tokenizer.NextToken();
      }
      else
        dtd.Append(GetDataTypeDefaults(type, row));

      while (token != ")" &&
             token != "," &&
             String.Compare(token, "begin", StringComparison.OrdinalIgnoreCase) != 0 &&
             String.Compare(token, "return", StringComparison.OrdinalIgnoreCase) != 0)
      {
        if (String.Compare(token, "CHARACTER", StringComparison.OrdinalIgnoreCase) == 0 ||
            String.Compare(token, "BINARY", StringComparison.OrdinalIgnoreCase) == 0)
        { }  // we don't need to do anything with this
        else if (String.Compare(token, "SET", StringComparison.OrdinalIgnoreCase) == 0 ||
                 String.Compare(token, "CHARSET", StringComparison.OrdinalIgnoreCase) == 0)
          row["CHARACTER_SET_NAME"] = tokenizer.NextToken();
        else if (String.Compare(token, "ASCII", StringComparison.OrdinalIgnoreCase) == 0)
          row["CHARACTER_SET_NAME"] = "latin1";
        else if (String.Compare(token, "UNICODE", StringComparison.OrdinalIgnoreCase) == 0)
          row["CHARACTER_SET_NAME"] = "ucs2";
        else if (String.Compare(token, "COLLATE", StringComparison.OrdinalIgnoreCase) == 0)
          row["COLLATION_NAME"] = tokenizer.NextToken();
        else
          dtd.AppendFormat(CultureInfo.InvariantCulture, " {0}", token);
        token = tokenizer.NextToken();
      }

      if (dtd.Length > 0)
        row["DTD_IDENTIFIER"] = dtd.ToString();

      // now default the collation if one wasn't given
      if (string.IsNullOrEmpty((string)row["COLLATION_NAME"]) &&
          !string.IsNullOrEmpty((string)row["CHARACTER_SET_NAME"]))
        row["COLLATION_NAME"] = await CharSetMap.GetDefaultCollationAsync(
            row["CHARACTER_SET_NAME"].ToString(), connection, execAsync, cancellationToken).ConfigureAwait(false);

      // now set the octet length
      if (row["CHARACTER_MAXIMUM_LENGTH"] != null)
      {
        if (row["CHARACTER_SET_NAME"] == null)
          row["CHARACTER_SET_NAME"] = "";
        row["CHARACTER_OCTET_LENGTH"] =
            await CharSetMap.GetMaxLengthAsync((string)row["CHARACTER_SET_NAME"], connection, execAsync, cancellationToken).ConfigureAwait(false) *
            (int)row["CHARACTER_MAXIMUM_LENGTH"];
      }

      return token;
    }

    private static string GetDataTypeDefaults(string type, MySqlSchemaRow row)
    {
      string format = "({0},{1})";
      if (MetaData.IsNumericType(type) &&
      string.IsNullOrEmpty(Convert.ToString(row["NUMERIC_PRECISION"])))
      {
        row["NUMERIC_PRECISION"] = 10;
        row["NUMERIC_SCALE"] = 0;
        if (!MetaData.SupportScale(type))
          format = "({0})";
        return String.Format(format, row["NUMERIC_PRECISION"],
            row["NUMERIC_SCALE"]);
      }
      return String.Empty;
    }

    private static void ParseDataTypeSize(MySqlSchemaRow row, string size)
    {
      size = size.Trim('(', ')');
      string[] parts = size.Split(',');

      if (!MetaData.IsNumericType(row["DATA_TYPE"].ToString()))
      {
        row["CHARACTER_MAXIMUM_LENGTH"] = Int32.Parse(parts[0], CultureInfo.InvariantCulture);
        // will set octet length in a minute
      }
      else
      {
        row["NUMERIC_PRECISION"] = Int32.Parse(parts[0], CultureInfo.InvariantCulture);
        if (parts.Length == 2)
          row["NUMERIC_SCALE"] = Int32.Parse(parts[1], CultureInfo.InvariantCulture);
      }
    }
    #endregion
  }
}
