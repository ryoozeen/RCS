using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotBotCarClient.Protocol
{
    public class ControlMessage
    {
        // 1) 메시지 종류 (ENROLL_REQ, LOGIN_REQ, DOOR_REQ, ... )
        public string? Msg { get; set; }

        // 2) 사용자 정보 (회원가입/로그인에서 사용)
        public string? Id { get; set; }           // 로그인 아이디
        public string? Password { get; set; }     // 비밀번호
        public string? UserName { get; set; }     // 사용자 이름
        public string? CarInfo { get; set; }      // 차량 정보 (EV6 등)

        // 3) 차량/제어 공통
        public int VehicleId { get; set; }       // 차량 번호 (1, 2, 3...)
        public string? Command { get; set; }      // ON/OFF, OPEN/CLOSE, UP/DOWN 등

        // 4) 응답 공통
        public bool Success { get; set; }        // True / False
        public string? Reason { get; set; }       // 실패 이유나 상태 메시지
        public string? CarModel { get; set; }     // LOGIN_RES 에서 내려줄 차량 모델명

        // 5) 상태 응답용 (STATUS_RES 등에서 사용 가능)
        public string? DoorStatus { get; set; }   // OPEN/CLOSE
        public string? StartStatus { get; set; }  // ON/OFF
        public string? AirStatus { get; set; }    // ON/OFF
        public string? HeatStatus { get; set; }   // ON/OFF
        public string? ChargingStatus { get; set; } // ON/OFF
        public int BatteryLevel { get; set; }    // 배터리 잔량(0~100)

        // 메시지 직렬화 (송신용)
        // ControlMessage → JSON → 바이트 → 길이 헤더 + 바이트
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
        // 길이 헤더 읽기 → 바디 바이트 읽기 → JSON → ControlMessage
        public static async Task<ControlMessage?> DeserializeMessageAsync(NetworkStream stream)
        {
            // 최대 메시지 크기 제한 (100KB)
            const int MAX_MESSAGE_SIZE = 1024 * 100;

            // 1. 길이 헤더 읽기 (4바이트)
            byte[] lengthBytes = new byte[4];
            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);

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
                bytesRead = await stream.ReadAsync(
                    messageBytes,
                    totalBytesRead,
                    messageLength - totalBytesRead
                );

                if (bytesRead == 0)
                {
                    return null; // 연결 끊김
                }

                totalBytesRead += bytesRead;
            }

            // 5. 바이트 → JSON 문자열
            string json = Encoding.UTF8.GetString(messageBytes);

            // 6. JSON → ControlMessage
            ControlMessage? message = JsonSerializer.Deserialize<ControlMessage>(json);

            return message;
        }
    }
}
