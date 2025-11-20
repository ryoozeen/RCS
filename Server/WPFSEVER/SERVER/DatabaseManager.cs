using MySql.Data.MySqlClient;
using SERVER.Protocol;
using System;
using System.Threading.Tasks;

namespace SERVER.Network
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> EnrollUserAsync(EnrollReq message)
        {
            if (message.id == null || message.password == null)
            {
                return 0;
            }

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "INSERT INTO users (id, password, userName, carModel) VALUES (@id, @password, @userName, @carModel)";

                await using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@id", message.id);
                cmd.Parameters.AddWithValue("@password", message.password);
                cmd.Parameters.AddWithValue("@userName", message.username ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carModel", message.car_model ?? (object)DBNull.Value);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                Console.WriteLine($"[DB DEBUG] ENROLL: {rowsAffected} rows affected. User: {message.id}");

                return rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                return 0;
            }
        }
        public async Task<int> LoginUserAsync(LoginReq message)
        {
            if (message.id == null || message.password == null)
            {
                return 0;
            }

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string sql = "SELECT COUNT(1) FROM users WHERE id = @id AND password = @password";

                await using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@id", message.id);
                cmd.Parameters.AddWithValue("@password", message.password);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                return 0;
            }
        }
    }
}