using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AwsLogDataLoader
{

    public abstract class DataLoaderBase
    {
        protected Func<SqlConnection> _connectionFactory;

        public async Task<bool> ValidateCredentials()
        {
            using (var connection = _connectionFactory())
            {
                try
                {
                    await connection.OpenAsync();
                    return true;
                }
                catch (SqlException)
                {
                    return false;
                }
            }
        }

        public async Task<string[]> GetTableNames()
        {

            using (var connection = _connectionFactory())
            { 
                await connection.OpenAsync();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT [name] from sys.tables WHERE is_ms_shipped = 0";
                    var tableNames = new List<string>();
                    var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    return tableNames.ToArray();
                }
            }
        }

        public async Task PrepareTable(string tableName)
        {
            var tables = await GetTableNames();
            if (!tables.Any(t => string.Equals(tableName, t, StringComparison.OrdinalIgnoreCase)))
            {
                await CreateTable(tableName);
            }
        }

        public async Task CreateTable(string tableName)
        {
            using (var connection = _connectionFactory())
            {
                using (var command = new SqlCommand(String.Format(Resources.TableCreation_sql, tableName), connection))
                {
                    await connection.OpenAsync();
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<string> GetPathToFormatFile()
        {
            var path = Path.GetTempFileName();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(Resources.FormatFile);
                }
            }
            return path;
        }

        public async Task LoadFile(string tableName, string formatFile, string file)
        {
            using (var connection = _connectionFactory())
            {
                await connection.OpenAsync();
                var commandText = string.Format(Resources.LoadScript, tableName, file, formatFile);
                using (var command = new SqlCommand(commandText, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }

    public sealed class SqlAuthenticatedLoader : DataLoaderBase
    {
        public SqlAuthenticatedLoader(string instance, string database, string username, string password)
        {
            var sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.IntegratedSecurity = false;
            sqlBuilder.DataSource = instance;
            sqlBuilder.InitialCatalog = database;
            sqlBuilder.UserID = username;
            sqlBuilder.Password = password;
            _connectionFactory = () => new SqlConnection(sqlBuilder.ToString());
        }
    }

    public sealed class WindowsAuthenticatedLoader : DataLoaderBase
    {
        public WindowsAuthenticatedLoader(string instance, string database)
        {
            var sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.IntegratedSecurity = true;
            sqlBuilder.DataSource = instance;
            sqlBuilder.InitialCatalog = database;
            _connectionFactory = () => new SqlConnection(sqlBuilder.ToString());
        }
    }
}
