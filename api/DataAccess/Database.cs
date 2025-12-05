using MySqlConnector;
using System.Data;

namespace MyApp.Namespace.DataAccess
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, params MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new MySqlDataAdapter(command);
            var dataTable = new DataTable();
            await Task.Run(() => adapter.Fill(dataTable));

            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<object?> ExecuteScalarAsync(string query, params MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteScalarAsync();
        }

        public async Task<T?> ExecuteScalarAsync<T>(string query, params MySqlParameter[] parameters)
        {
            var result = await ExecuteScalarAsync(query, parameters);
            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<long> ExecuteInsertAndGetIdAsync(string insertQuery, params MySqlParameter[] parameters)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(insertQuery, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            await command.ExecuteNonQueryAsync();
            return command.LastInsertedId;
        }
    }
}

