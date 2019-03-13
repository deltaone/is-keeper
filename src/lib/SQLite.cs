using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Core
{
	public class SQLite : IDisposable
	{
		private readonly SQLiteConnection _dbConnection;

		public SQLite()
		{
			_dbConnection = new SQLiteConnection("data source = " + Path.Combine(GM.assemblyFolder, "main.db"));
			_dbConnection.Open(); // if(_dbConnection.State == ConnectionState.Closed)
		}

		public SQLite(string db)
		{
			_dbConnection = new SQLiteConnection(string.Format("data source={0}", db));
			_dbConnection.Open(); 
		}

		public SQLite(Dictionary<string, string> connectionOptions)
		{
			string str = connectionOptions.Aggregate("", (current, row) => current + string.Format("{0}={1}; ", row.Key, row.Value));
			_dbConnection = new SQLiteConnection(str.Trim().Substring(0, str.Length - 1));
			_dbConnection.Open(); 
		}

		public void Dispose()
		{
			if(_dbConnection != null)
				_dbConnection.Dispose();
		}

		public DataTable GetDataTable(string commandText)
		{
			var table = new DataTable(); // disposable!!!!

			using(SQLiteCommand command = new SQLiteCommand(commandText, _dbConnection))
				using(SQLiteDataReader reader = command.ExecuteReader())
					table.Load(reader);

			return (table);
		}

		public int Insert(string table, Dictionary<string, object> data)
		{
			using(SQLiteCommand command = new SQLiteCommand(_dbConnection))
			{
				string columns = "", values = "";
				foreach(KeyValuePair<string, object> entry in data)
				{
					columns += entry.Key + ","; //columns += string.Format(" {0},", entry.Key);
					values += "@" + entry.Key + ","; //values += string.Format(" '{0}',", entry.Value);
					command.Parameters.AddWithValue("@" + entry.Key, entry.Value);
				}
				columns = columns.Substring(0, columns.Length - 1);
				values = values.Substring(0, values.Length - 1);
				command.CommandText = string.Format("INSERT INTO {0}({1}) values({2});", table, columns, values);
				return (command.ExecuteNonQuery());
			}
		}

		public int Update(string table, Dictionary<string, object> data, string where = null)
		{
			using(SQLiteCommand command = new SQLiteCommand(_dbConnection))
			{
				string values = "";
				foreach(KeyValuePair<string, object> entry in data)
				{						
					values += entry.Key + " = @" + entry.Key + ","; 
					command.Parameters.AddWithValue("@" + entry.Key, entry.Value);
				}					
				values = values.Substring(0, values.Length - 1);
				command.CommandText = string.Format("UPDATE {0} SET {1} {2};", table, values,  where != null ?  " WHERE " + where :  "");
				return (command.ExecuteNonQuery());
			}
		}

		public int Delete(string table, string where = null)
		{
			return (Execute(string.Format("DELETE FROM {0} {1};", table, where != null ? " WHERE " + where : "")));
		}


		public object ExecuteScalar(string commandText)
		{
			try
			{
				using(SQLiteCommand command = new SQLiteCommand(commandText, _dbConnection))
					return (command.ExecuteScalar());
			}
			catch(Exception e)
			{
				Console.WriteLine("SQLite Exception : {0}", e.Message);
			}

			return null;
		}

		public int Execute(string commandText)
		{
			try
			{
				using(SQLiteCommand command = new SQLiteCommand(commandText, _dbConnection))
					return (command.ExecuteNonQuery());
			}
			catch(Exception ex)
			{
				GM.Error("SQLite Exception: " + ex.Message);
				return (0);
			}
		}

		public SQLiteDataReader ExecuteReader(string commandText)
		{
			try
			{
				using(SQLiteCommand command = new SQLiteCommand(commandText, _dbConnection))
					return (command.ExecuteReader());
			}
			catch(Exception ex)
			{
				GM.Error("SQLite Exception: " + ex.Message);
				return (null);
			}
		}
	}
}
