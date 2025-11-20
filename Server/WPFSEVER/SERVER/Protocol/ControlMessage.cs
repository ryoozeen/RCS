using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SERVER.Protocol
{
    // 메시지 타입
    public enum MsgType
    {
        CLIENT_IDENTIFY_REQ,
        CLIENT_IDENTIFY_RES,
        ENROLL_REQ,
        ENROLL_RES,
        LOGIN_REQ,
        LOGIN_RES,
        START_REQ,
        START_RES,
        DOOR_REQ,
        DOOR_RES,
        TRUNK_REQ,
        TRUNK_RES,
        AIR_REQ,
        AIR_RES,
        CLI_REQ,
        CLI_RES,
        HEAT_REQ,
        HEAT_RES,
        LIGHT_REQ,
        LIGHT_RES,
        CONTROL_REQ,
        CONTROL_RES,
        STATUS_REQ,
        STATUS_RES,
        STOP_CHARGING_REQ,
        STOP_CHARGING_RES
    }
    // 공통 메시지(기반 클래스)
    public class BaseMessage
    {
        public MsgType msg { get; set; }
        public string? reason { get; set; }

        // 직렬화/역직렬화를 위한 정적 메서드
        public static async Task<BaseMessage?> DeserializeMessageAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            const int MAX_MESSAGE_SIZE = 1024 * 100;

            // 1. 길이 헤더 읽기 (4바이트)
            byte[] lengthBytes = new byte[4];
            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            if (bytesRead != 4) return null;

            // 2. 메시지 길이 파싱
            int messageLength = BitConverter.ToInt32(lengthBytes, 0);
            if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE) return null;

            // 3. 메시지 바디 읽기
            byte[] messageBytes = new byte[messageLength];
            int totalBytesRead = 0;

            while (totalBytesRead < messageLength)
            {
                bytesRead = await stream.ReadAsync(
                    messageBytes,
                    totalBytesRead,
                    messageLength - totalBytesRead,
                    cancellationToken);

                if (bytesRead == 0 || cancellationToken.IsCancellationRequested) return null;
                totalBytesRead += bytesRead;
            }

            // JSON 변환
            string json = Encoding.UTF8.GetString(messageBytes);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // 대소문자 무시
                    Converters = { new JsonStringEnumConverter() }
                };

                // 먼저 BaseMessage로 파싱하여 msg 타입 확인
                using JsonDocument doc = JsonDocument.Parse(json);

                // "msg" 또는 "Msg" 속성 찾기
                JsonElement msgElement;
                if (!doc.RootElement.TryGetProperty("msg", out msgElement) &&
                    !doc.RootElement.TryGetProperty("Msg", out msgElement))
                {
                    return null;
                }

                string? msgString = msgElement.GetString();
                if (string.IsNullOrEmpty(msgString))
                {
                    return null;
                }

                if (!Enum.TryParse<MsgType>(msgString, out MsgType msgType))
                {
                    return null;
                }

                // msg 타입에 따라 적절한 클래스로 역직렬화
                BaseMessage? message = msgType switch
                {
                    MsgType.CLIENT_IDENTIFY_REQ => JsonSerializer.Deserialize<ClientIdentifyReq>(json, options),
                    MsgType.CLIENT_IDENTIFY_RES => JsonSerializer.Deserialize<ClientIdentifyRes>(json, options),
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
                    MsgType.CLI_REQ => JsonSerializer.Deserialize<CliReq>(json, options),
                    MsgType.CLI_RES => JsonSerializer.Deserialize<CliRes>(json, options),
                    MsgType.HEAT_REQ => JsonSerializer.Deserialize<HeatReq>(json, options),
                    MsgType.HEAT_RES => JsonSerializer.Deserialize<HeatRes>(json, options),
                    MsgType.LIGHT_REQ => JsonSerializer.Deserialize<LightReq>(json, options),
                    MsgType.LIGHT_RES => JsonSerializer.Deserialize<LightRes>(json, options),
                    MsgType.CONTROL_REQ => JsonSerializer.Deserialize<ControlReq>(json, options),
                    MsgType.CONTROL_RES => JsonSerializer.Deserialize<ControlRes>(json, options),
                    MsgType.STATUS_REQ => JsonSerializer.Deserialize<StatusReq>(json, options),
                    MsgType.STATUS_RES => JsonSerializer.Deserialize<StatusRes>(json, options),
                    MsgType.STOP_CHARGING_REQ => JsonSerializer.Deserialize<StopChargingReq>(json, options),
                    MsgType.STOP_CHARGING_RES => JsonSerializer.Deserialize<StopChargingRes>(json, options),
                    _ => null
                };
                return message;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] SerializeMessage(BaseMessage message)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // PropertyNamingPolicy 제거: client_name으로 통일 (snake_case 유지)
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(message, message.GetType(), options);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            byte[] result = new byte[4 + jsonBytes.Length];
            Array.Copy(lengthBytes, 0, result, 0, 4);
            Array.Copy(jsonBytes, 0, result, 4, jsonBytes.Length);
            return result;
        }
    }
    // 클라이언트 식별 요청
    public class ClientIdentifyReq : BaseMessage
    {
        public string? client_name { get; set; }
        public ClientIdentifyReq() { msg = MsgType.CLIENT_IDENTIFY_REQ; }
    }

    // 클라이언트 식별 응답
    public class ClientIdentifyRes : BaseMessage
    {
        public bool identified { get; set; }
        public ClientIdentifyRes() { msg = MsgType.CLIENT_IDENTIFY_RES; }
    }

    // 회원가입 요청
    public class EnrollReq : BaseMessage
    {
        public string? id { get; set; }
        public string? password { get; set; }
        public string? username { get; set; }
        public string? car_model { get; set; }

        public EnrollReq()
        {
            msg = MsgType.ENROLL_REQ;
        }
    }
    // 회원가입 응답
    public class EnrollRes : BaseMessage
    {
        public bool registered { get; set; }

        public EnrollRes()
        {
            msg = MsgType.ENROLL_RES;
        }
    }
    public class LoginReq : BaseMessage
    {
        public string? id { get; set; }
        public string? password { get; set; }

        public LoginReq()
        {
            msg = MsgType.LOGIN_REQ;
        }
    }
    public class LoginRes : BaseMessage
    {
        public bool logined { get; set; }

        public LoginRes()
        {
            msg = MsgType.LOGIN_RES;
        }
    }
    // 시동 제어
    public class StartReq : BaseMessage
    {
        public bool active { get; set; }
        public StartReq() { msg = MsgType.START_REQ; }
    }

    public class StartRes : BaseMessage
    {
        public bool active_status { get; set; }
        public StartRes() { msg = MsgType.START_RES; }
    }
    // 문 제어 요청
    public class DoorReq : BaseMessage
    {
        public bool door { get; set; }

        public DoorReq()
        {
            msg = MsgType.DOOR_REQ;
        }
    }
    // 문 제어 응답
    public class DoorRes : BaseMessage
    {
        public bool door_status { get; set; }

        public DoorRes()
        {
            msg = MsgType.DOOR_RES;
        }
    }
    // 트렁크 제어
    public class TrunkReq : BaseMessage
    {
        public bool trunk { get; set; }
        public TrunkReq() { msg = MsgType.TRUNK_REQ; }
    }

    public class TrunkRes : BaseMessage
    {
        public bool trunk_status { get; set; }
        public TrunkRes() { msg = MsgType.TRUNK_RES; }
    }
    // 에어컨 제어
    public class AirReq : BaseMessage
    {
        public bool air { get; set; }
        public AirReq() { msg = MsgType.AIR_REQ; }
    }

    public class AirRes : BaseMessage
    {
        public bool air_status { get; set; }
        public AirRes() { msg = MsgType.AIR_RES; }
    }
    // 온도 제어
    public class CliReq : BaseMessage
    {
        public int temp { get; set; }
        public CliReq() { msg = MsgType.CLI_REQ; }
    }

    public class CliRes : BaseMessage
    {
        public bool temp_status { get; set; }
        public CliRes() { msg = MsgType.CLI_RES; }
    }
    // 열선 제어
    public class HeatReq : BaseMessage
    {
        public bool heat { get; set; }
        public HeatReq() { msg = MsgType.HEAT_REQ; }
    }

    public class HeatRes : BaseMessage
    {
        public bool heat_status { get; set; }
        public HeatRes() { msg = MsgType.HEAT_RES; }
    }
    // 라이트 제어
    public class LightReq : BaseMessage
    {
        public bool light { get; set; }
        public LightReq() { msg = MsgType.LIGHT_REQ; }
    }

    public class LightRes : BaseMessage
    {
        public bool light_status { get; set; }
        public LightRes() { msg = MsgType.LIGHT_RES; }
    }
    // 원격주차 제어(UP/DOWN 또는 PARK/DRIVE)
    public class ControlReq : BaseMessage
    {
        public bool control { get; set; }
        public ControlReq() { msg = MsgType.CONTROL_REQ; }
    }

    public class ControlRes : BaseMessage
    {
        public bool control_status { get; set; }
        public bool parking { get; set; }
        public bool driving { get; set; }
        public ControlRes() { msg = MsgType.CONTROL_RES; }
    }
    // 배터리 충전 종료 제어
    public class StopChargingReq : BaseMessage
    {
        public bool stop { get; set; }
        public StopChargingReq() { msg = MsgType.STOP_CHARGING_REQ; }
    }
    public class StopChargingRes : BaseMessage
    {
        public bool stop_status { get; set; }
        public StopChargingRes() { msg = MsgType.STOP_CHARGING_RES; }
    }
    // 상태 제어
    public class StatusReq : BaseMessage
    {
        public bool car_status { get; set; }

        public StatusReq()
        {
            msg = MsgType.STATUS_REQ;
        }
    }
    public class StatusRes : BaseMessage
    {
        public bool charging { get; set; }
        public bool parking { get; set; }
        public bool driving { get; set; }
        public double battery { get; set; }

        public StatusRes()
        {
            msg = MsgType.STATUS_RES;
        }
    }
}
