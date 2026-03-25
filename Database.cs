using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace WeaponPaints
{
	public class Database(string dbConnectionString, string dbType)
	{
		private readonly string _dbType = dbType.ToLower();

		public async Task<IDbConnection> GetConnectionAsync()
		{
			try
			{
				DbConnection connection = _dbType switch
				{
					"sqlite" => new SqliteConnection(dbConnectionString),
					_ => new MySqlConnection(dbConnectionString)
				};

				await connection.OpenAsync();
				
				if (_dbType == "sqlite")
				{
					// Enable WAL mode for better concurrency in SQLite
					using var command = connection.CreateCommand();
					command.CommandText = "PRAGMA journal_mode=WAL;";
					await command.ExecuteNonQueryAsync();
				}

				return connection;
			}
			catch (Exception ex)
			{
				WeaponPaints.Instance.Logger.LogError($"Unable to connect to database ({dbType}): {ex.Message}");
				throw;
			}
		}

		public string GetDbType() => _dbType;
	}
}