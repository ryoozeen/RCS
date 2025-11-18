using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotBotCarClient.Protocol
{
    // 직렬화 + 역직렬화 + 파싱
    public class ControlMessage
    {
        // ------------------------------
        // 메시지 직렬화 (송신)
        // BaseMessage → JSON → byte[]
        // ------------------------------
        public static byte[] Serialize(object message)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(message, message.GetType(), options);
            byte[] body = Encoding.UTF8.GetBytes(json);

            byte[] lengthBytes = BitConverter.GetBytes(body.Length);
            byte[] packet = new byte[lengthBytes.Length + body.Length];

            Array.Copy(lengthBytes, 0, packet, 0, 4);
            Array.Copy(body, 0, packet, 4, body.Length);

            return packet;
        }

        // ------------------------------
        // 메시지 역직렬화 (수신)
        // byte[] → JSON → BaseMessage
        // ------------------------------
        public static async Task<BaseMessage?> DeserializeAsync(NetworkStream stream)
        {
            const int MAX_MESSAGE_SIZE = 1024 * 100;
            byte[] lengthBytes = new byte[4];

            // 1) 길이 헤더 읽기
            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);
            if (bytesRead != 4)
                return null;

            int messageLength = BitConverter.ToInt32(lengthBytes, 0);
            if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE)
                return null;

            byte[] messageBytes = new byte[messageLength];
            int totalBytesRead = 0;

            // 2) 바디 읽기
            while (totalBytesRead < messageLength)
            {
                bytesRead = await stream.ReadAsync(
                    messageBytes,
                    totalBytesRead,
                    messageLength - totalBytesRead);

                if (bytesRead == 0)
                    return null;

                totalBytesRead += bytesRead;
            }

            // 3) JSON 문자열 생성
            string json = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"[RAW JSON] {json}");

            // 4) Parse로 타입 분기 후 반환
            return Parse(json);
        }


        // ------------------------------
        // Parse: MsgType 보고 클래스로 분기
        // ------------------------------
        public static BaseMessage? Parse(string json)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // msg 또는 Msg 둘 다 체크하도록 수정
            if (!root.TryGetProperty("msg", out var msgProp) &&
                !root.TryGetProperty("Msg", out msgProp))
                return null;

            // Enum.TryParse 사용
            if (!Enum.TryParse(msgProp.GetString(), out MsgType type))
                return null;

            return type switch
            {
                MsgType.ENROLL_REQ => JsonSerializer.Deserialize<EnrollReq>(json, options),
                MsgType.ENROLL_RES => JsonSerializer.Deserialize<EnrollRes>(json, options),

                MsgType.LOGIN_REQ => JsonSerializer.Deserialize<LoginReq>(json, options),
                MsgType.LOGIN_RES => JsonSerializer.Deserialize<LoginRes>(json, options),

                MsgType.DOOR_REQ => JsonSerializer.Deserialize<DoorReq>(json, options),
                MsgType.DOOR_RES => JsonSerializer.Deserialize<DoorRes>(json, options),

                MsgType.STATUS_REQ => JsonSerializer.Deserialize<StatusReq>(json, options),
                MsgType.STATUS_RES => JsonSerializer.Deserialize<StatusRes>(json, options),

                _ => JsonSerializer.Deserialize<BaseMessage>(json, options)
            };
        }
    }
}
