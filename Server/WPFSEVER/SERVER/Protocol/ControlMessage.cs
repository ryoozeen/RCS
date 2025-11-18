using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;         
using System.Threading.Tasks;

namespace SERVER.Protocol
{
    public class ControlMessage
    {
        // 1) 메시지 종류
        // ENROLL_REQ, LOGIN_REQ, CLIENT_IDENTIFY, DOOR_REQ, STATUS_RES 
        public string? Msg { get; set; }

        // 2) 식별 및 인증 (CLIENT_IDENTIFY, ENROLL_REQ, LOGIN_REQ)
        public string? Id { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
        public string? CarModel { get; set; }    

        // 3) 제어 요청용 (REQ)
        //  True/False (ON/OFF, OPEN/CLOSE) 요청용
        public bool CommandValue { get; set; }
        // 온도(int) 요청용 (CLI_REQ)
        public int CommandTemp { get; set; }
      
        public string? Command { get; set; }

        // 4) 응답 공통 (RES)
        public bool Success { get; set; }       // True / False
        public string? Reason { get; set; }      // 실패 이유

        // 5) 상태 응답용 (STATUS_RES)
        public string? DoorStatus { get; set; }    // OPEN/CLOSE
        public string? StartStatus { get; set; }   // ON/OFF
        public string? AirStatus { get; set; }     // ON/OFF
        public string? HeatStatus { get; set; }    // ON/OFF
        public string? ChargingStatus { get; set; } // ON/OFF
        public int BatteryLevel { get; set; }      // 배터리 잔량(0~100)

        //  추가된 상태값
        public string? TrunkStatus { get; set; }   // OPEN/CLOSE
        public string? LightStatus { get; set; }   // ON/OFF
        public int CliTemp { get; set; }           // 현재 설정 온도 (CLI_RES)


        // 메시지 직렬화 (송신용)
        // (이 메서드는 수정할 필요가 없습니다)
        public static byte[] SerializeMessage(ControlMessage message)
        {
            // 1. JSON 직렬화
            string json = JsonSerializer.Serialize(message);

            // 2. UTF-8 바이트 변환
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // 3. 길이 헤더 (4바이트) + JSON 바이트
            byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            byte[] result = new byte[4 + jsonBytes.Length];

            // 4. 길이 헤더 복사
            Array.Copy(lengthBytes, 0, result, 0, 4);

            // 5. JSON 바이트 복사
            Array.Copy(jsonBytes, 0, result, 4, jsonBytes.Length);

            return result;
        }

        //  메시지 역직렬화 (수신용)
        // CancellationToken 매개변수 추가
        public static async Task<ControlMessage?> DeserializeMessageAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            // 최대 메시지 크기 제한 (100KB)
            const int MAX_MESSAGE_SIZE = 1024 * 100;

            // 1. 길이 헤더 읽기 (4바이트)
            byte[] lengthBytes = new byte[4];

            // CancellationToken 전달
            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);

            if (bytesRead != 4)
            {
                return null; // 연결 끊김
            }

            // 2. 길이 파싱
            int messageLength = BitConverter.ToInt32(lengthBytes, 0);

            // 3. 메시지 크기 검증
            if (messageLength < 0)
            {
                return null; // 음수 길이는 잘못된 메시지
            }

            if (messageLength > MAX_MESSAGE_SIZE)
            {
                return null; // 너무 큰 메시지는 거부 (보안/DoS 방지)
            }

            if (messageLength == 0)
            {
                return null; // 빈 메시지는 무효
            }

            // 4. 메시지 바디 읽기 (길이만큼)
            byte[] messageBytes = new byte[messageLength];
            int totalBytesRead = 0;

            while (totalBytesRead < messageLength)
            {
                // CancellationToken 전달
                bytesRead = await stream.ReadAsync(
                    messageBytes,
                    totalBytesRead,
                    messageLength - totalBytesRead,
                    cancellationToken // CancellationToken 전달
                );

                if (bytesRead == 0 || cancellationToken.IsCancellationRequested)
                {
                    return null; // 연결 끊김 또는 취소됨
                }

                totalBytesRead += bytesRead;
            }

            // 5. 바이트 → JSON 문자열
            string json = Encoding.UTF8.GetString(messageBytes);

            // 6. JSON → ControlMessage
            try
            {
                ControlMessage? message = JsonSerializer.Deserialize<ControlMessage>(json);
                return message;
            }
            catch (JsonException)
            {
                // JSON 파싱 실패
                return null;
            }
        }
    }
}