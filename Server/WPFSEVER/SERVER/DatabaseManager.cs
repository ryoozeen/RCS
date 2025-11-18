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

        // [핵심 수정] 회원가입: Task<int>를 반환합니다. (디버그 정보)
        public async Task<int> EnrollUserAsync(ControlMessage message)
        {
            if (message.id == null || message.password == null) return 0; // 실패 시 0 반환

            try
            {
                await using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // 테이블/컬럼 이름은 'users', 'id', 'password' 등으로 가정합니다.
                string sql = "INSERT INTO users (id, password, userName, carModel) VALUES (@id, @password, @userName, @carModel)";

                await using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@id", message.id);
                cmd.Parameters.AddWithValue("@password", message.password);
                cmd.Parameters.AddWithValue("@userName", message.userName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carModel", message.carModel ?? (object)DBNull.Value);

                // 결과를 int로 받아서 반환합니다. (0이면 실패, 1이면 성공)
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected;
            }
            catch (Exception ex)
            {
                // [로그 확인 필요] 디버거가 여기서 멈출 수 있습니다.
                Console.WriteLine($"DB Error: {ex.Message}");
                return 0; // 오류 시 0 반환
            }
        }

        // [수정] 로그인: Task<int>를 반환합니다.
        public async Task<int> LoginUserAsync(ControlMessage message)
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
                return Convert.ToInt32(result); // 0 또는 1 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                return 0; // 오류 시 0 반환
            }
        }
    }
}