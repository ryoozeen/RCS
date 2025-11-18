using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotBotCarClient.Protocol
{
    public class ControlMessage
    {
        // ============================================================
        // 🔵 송신: Serialize<T>
        // ============================================================
        public static byte[] Serialize<T>(T message)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(message, message.GetType(), options);

            // 🔥 송신 디버깅
            Console.WriteLine("========================================");
            Console.WriteLine("[CLIENT → SERVER] SEND");
            Console.WriteLine($"[JSON] {json}");
            Console.WriteLine($"[Type] {message.GetType().Name}");

            byte[] body = Encoding.UTF8.GetBytes(json);
            Console.WriteLine($"[Body Length] {body.Length} bytes");

            byte[] lengthBytes = BitConverter.GetBytes(body.Length);
            byte[] packet = new byte[4 + body.Length];

            Array.Copy(lengthBytes, 0, packet, 0, 4);
            Array.Copy(body, 0, packet, 4, body.Length);

            Console.WriteLine("[Packet] " + BitConverter.ToString(packet));
            Console.WriteLine("========================================\n");

            return packet;
        }


        // ============================================================
        // 🔴 수신: DeserializeAsync
        // ============================================================
        public static async Task<BaseMessage?> DeserializeAsync(NetworkStream stream)
        {
            try
            {
                const int MAX_MESSAGE_SIZE = 1024 * 100;
                byte[] lengthBytes = new byte[4];

                // 💬 길이 읽기
                int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);
                if (bytesRead != 4)
                {
                    Console.WriteLine("[ERROR] Length header read failed.");
                    return null;
                }

                int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                Console.WriteLine("========================================");
                Console.WriteLine("[SERVER → CLIENT] RECV");
                Console.WriteLine($"[Length] {messageLength}");

                if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE)
                {
                    Console.WriteLine("[ERROR] Invalid messageLength");
                    return null;
                }

                byte[] messageBytes = new byte[messageLength];
                int totalRead = 0;

                // 💬 메시지 본문 읽기
                while (totalRead < messageLength)
                {
                    int rb = await stream.ReadAsync(
                        messageBytes,
                        totalRead,
                        messageLength - totalRead);

                    if (rb == 0)
                    {
                        Console.WriteLine("[ERROR] Stream closed while reading body");
                        return null;
                    }

                    totalRead += rb;
                }

                // 💬 JSON 출력
                string json = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine($"[JSON] {json}");

                BaseMessage? parsed = Parse(json);

                if (parsed == null)
                {
                    Console.WriteLine("[ERROR] Parse returned null");
                }
                else
                {
                    Console.WriteLine($"[Parsed Type] {parsed.GetType().Name}");
                }

                Console.WriteLine("========================================\n");

                return parsed;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EXCEPTION] DeserializeAsync : " + ex.Message);
                return null;
            }
        }


        // ============================================================
        // 🔍 Parse 메서드 (타입 선택)
        // ============================================================
        public static BaseMessage? Parse(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() },
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("msg", out var msgProp) &&
                    !root.TryGetProperty("Msg", out msgProp))
                {
                    Console.WriteLine("[Parse ERROR] msg field not found!");
                    return null;
                }

                string? msgValue = msgProp.GetString();
                Console.WriteLine($"[Parse] msg = {msgValue}");

                if (!Enum.TryParse(msgValue, out MsgType type))
                {
                    Console.WriteLine($"[Parse ERROR] Unknown msg type: {msgValue}");
                    return null;
                }

                BaseMessage? result = type switch
                {
                    MsgType.ENROLL_REQ => JsonSerializer.Deserialize<EnrollReq>(json, options),
                    MsgType.ENROLL_RES => JsonSerializer.Deserialize<EnrollRes>(json, options),

                    MsgType.LOGIN_REQ => JsonSerializer.Deserialize<LoginReq>(json, options),
                    MsgType.LOGIN_RES => JsonSerializer.Deserialize<LoginRes>(json, options),

                    MsgType.START_REQ => JsonSerializer.Deserialize<StartReq>(json, options),
                    MsgType.START_RES => JsonSerializer.Deserialize<StartRes>(json, options),

                    MsgType.DOOR_REQ => JsonSerializer.Deserialize<DoorReq>(json, options),
                    MsgType.DOOR_RES => JsonSerializer.Deserialize<DoorRes>(json, options),

                    MsgType.TRUNK_REQ => JsonSerializer.Deserialize<TrunkReq>(json, options),
                    MsgType.TRUNK_RES => JsonSerializer.Deserialize<TrunkRes>(json, options),

                    MsgType.AIR_REQ => JsonSerializer.Deserialize<AirReq>(json, options),
                    MsgType.AIR_RES => JsonSerializer.Deserialize<AirRes>(json, options),

                    MsgType.HEAT_REQ => JsonSerializer.Deserialize<HeatReq>(json, options),
                    MsgType.HEAT_RES => JsonSerializer.Deserialize<HeatRes>(json, options),

                    MsgType.LIGHT_REQ => JsonSerializer.Deserialize<LightReq>(json, options),
                    MsgType.LIGHT_RES => JsonSerializer.Deserialize<LightRes>(json, options),

                    MsgType.CONTROL_REQ => JsonSerializer.Deserialize<ControlReq>(json, options),
                    MsgType.CONTROL_RES => JsonSerializer.Deserialize<ControlRes>(json, options),

                    MsgType.STOP_CHARGING_REQ => JsonSerializer.Deserialize<StopChargingReq>(json, options),
                    MsgType.STOP_CHARGING_RES => JsonSerializer.Deserialize<StopChargingRes>(json, options),

                    MsgType.STATUS_REQ => JsonSerializer.Deserialize<StatusReq>(json, options),
                    MsgType.STATUS_RES => JsonSerializer.Deserialize<StatusRes>(json, options),

                    // 필요하면 추가
                    _ => JsonSerializer.Deserialize<BaseMessage>(json, options),
                };

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Parse Exception] " + ex.Message);
                return null;
            }
        }
    }
}