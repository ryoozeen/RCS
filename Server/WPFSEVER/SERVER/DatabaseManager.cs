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

        // 회원가입
        public async Task<int> EnrollUserAsync(EnrollReq message)
        {
            if (message.id == null || message.password == null) return 0;

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // users 테이블 삽입
                string sqlUsers = "INSERT INTO users (id, password, userName, carModel) VALUES (@id, @password, @userName, @carModel)";
                await using var cmdUsers = new MySqlCommand(sqlUsers, conn);
                cmdUsers.Parameters.AddWithValue("@id", message.id);
                cmdUsers.Parameters.AddWithValue("@password", message.password);
                cmdUsers.Parameters.AddWithValue("@userName", message.username ?? (object)DBNull.Value);
                cmdUsers.Parameters.AddWithValue("@carModel", message.car_model ?? (object)DBNull.Value);

                // [HEAD 선택] 올바른 변수명(cmdUsers) 사용 및 실행
                int rowsAffected = await cmdUsers.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    // 회원가입 성공 시 car_statrs 초기값 삽입 (로컬 로직 유지)
                    string sqlStatus = "INSERT INTO car_statrs (id, status, start, control) VALUES (@id, '주차중', 'off', '주차')";
                    await using var cmdStatus = new MySqlCommand(sqlStatus, conn);
                    cmdStatus.Parameters.AddWithValue("@id", message.id);
                    await cmdStatus.ExecuteNonQueryAsync();
                }

                Console.WriteLine($"[DB DEBUG] ENROLL: Success User: {message.id}");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                return 0;
            }
        }

        // 로그인
        public async Task<int> LoginUserAsync(LoginReq message)
        {
            if (message.id == null || message.password == null) return 0;

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