using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SERVER.Protocol
{
    // ============================
    // 1. 메시지 타입 정의 (서버 원본 유지)
    // ============================
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

    // ============================
    // 2. 공통 메시지 (기반 클래스)
    // ============================
    public class BaseMessage
    {
        public MsgType msg { get; set; }
        public string? reason { get; set; }

        // JSON 옵션: 대소문자 무시 및 Enum 문자열 변환 허용
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        // 🔴 역직렬화 (수신)
        public static async Task<BaseMessage?> DeserializeMessageAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            byte[] lengthBytes = new byte[4];

            int lenRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            if (lenRead != 4) return null;

            int bodyLength = BitConverter.ToInt32(lengthBytes, 0);
            if (bodyLength <= 0 || bodyLength > 1024 * 100) return null;

            byte[] body = new byte[bodyLength];
            int received = 0;

            while (received < bodyLength)
            {
                int chunk = await stream.ReadAsync(body, received, bodyLength - received, cancellationToken);
                if (chunk == 0) return null;
                received += chunk;
            }

            string json = Encoding.UTF8.GetString(body);

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("msg", out var msgProp) &&
                    !doc.RootElement.TryGetProperty("Msg", out msgProp))
                    return null;

                string? msgString = msgProp.GetString();
                if (!Enum.TryParse(msgString, true, out MsgType msgType))
                    return null;

                return msgType switch
                {
                    MsgType.CLIENT_IDENTIFY_REQ => JsonSerializer.Deserialize<ClientIdentifyReq>(json, JsonOptions),
                    MsgType.CLIENT_IDENTIFY_RES => JsonSerializer.Deserialize<ClientIdentifyRes>(json, JsonOptions),
                    MsgType.ENROLL_REQ => JsonSerializer.Deserialize<EnrollReq>(json, JsonOptions),
                    MsgType.ENROLL_RES => JsonSerializer.Deserialize<EnrollRes>(json, JsonOptions),
                    MsgType.LOGIN_REQ => JsonSerializer.Deserialize<LoginReq>(json, JsonOptions),
                    MsgType.LOGIN_RES => JsonSerializer.Deserialize<LoginRes>(json, JsonOptions),
                    MsgType.START_REQ => JsonSerializer.Deserialize<StartReq>(json, JsonOptions),
                    MsgType.START_RES => JsonSerializer.Deserialize<StartRes>(json, JsonOptions),
                    MsgType.DOOR_REQ => JsonSerializer.Deserialize<DoorReq>(json, JsonOptions),
                    MsgType.DOOR_RES => JsonSerializer.Deserialize<DoorRes>(json, JsonOptions),
                    MsgType.TRUNK_REQ => JsonSerializer.Deserialize<TrunkReq>(json, JsonOptions),
                    MsgType.TRUNK_RES => JsonSerializer.Deserialize<TrunkRes>(json, JsonOptions),
                    MsgType.AIR_REQ => JsonSerializer.Deserialize<AirReq>(json, JsonOptions),
                    MsgType.AIR_RES => JsonSerializer.Deserialize<AirRes>(json, JsonOptions),
                    MsgType.CLI_REQ => JsonSerializer.Deserialize<CliReq>(json, JsonOptions),
                    MsgType.CLI_RES => JsonSerializer.Deserialize<CliRes>(json, JsonOptions),
                    MsgType.HEAT_REQ => JsonSerializer.Deserialize<HeatReq>(json, JsonOptions),
                    MsgType.HEAT_RES => JsonSerializer.Deserialize<HeatRes>(json, JsonOptions),
                    MsgType.LIGHT_REQ => JsonSerializer.Deserialize<LightReq>(json, JsonOptions),
                    MsgType.LIGHT_RES => JsonSerializer.Deserialize<LightRes>(json, JsonOptions),
                    MsgType.CONTROL_REQ => JsonSerializer.Deserialize<ControlReq>(json, JsonOptions),
                    MsgType.CONTROL_RES => JsonSerializer.Deserialize<ControlRes>(json, JsonOptions),
                    MsgType.STATUS_REQ => JsonSerializer.Deserialize<StatusReq>(json, JsonOptions),
                    MsgType.STATUS_RES => JsonSerializer.Deserialize<StatusRes>(json, JsonOptions),
                    MsgType.STOP_CHARGING_REQ => JsonSerializer.Deserialize<StopChargingReq>(json, JsonOptions),
                    MsgType.STOP_CHARGING_RES => JsonSerializer.Deserialize<StopChargingRes>(json, JsonOptions),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        // 🔵 직렬화 (송신)
        public static byte[] SerializeMessage(BaseMessage message)
        {
            string json = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);
            byte[] body = Encoding.UTF8.GetBytes(json);

            byte[] header = BitConverter.GetBytes(body.Length);
            byte[] packet = new byte[header.Length + body.Length];

            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(body, 0, packet, header.Length, body.Length);

            return packet;
        }
    }


    // 식별
    public class ClientIdentifyReq : BaseMessage
    { 
        public string? client_name { get; set; } 
        public ClientIdentifyReq() { msg = MsgType.CLIENT_IDENTIFY_REQ; }
    }
    public class ClientIdentifyRes : BaseMessage 
    {
        public bool identified { get; set; } 
        public ClientIdentifyRes() { msg = MsgType.CLIENT_IDENTIFY_RES; }
    }

    // 회원가입 (DB용 필드 포함)
    public class EnrollReq : BaseMessage 
    {
        public string? id { get; set; }
        public string? password { get; set; } 
        public string? username { get; set; }
        public string? car_model { get; set; }
        public EnrollReq() { msg = MsgType.ENROLL_REQ; } 
    }
    public class EnrollRes : BaseMessage 
    {
        public bool registered { get; set; } 
        public EnrollRes() { msg = MsgType.ENROLL_RES; }
    }

    // 로그인 (DB용 필드 포함)
    public class LoginReq : BaseMessage 
    {
        public string? id { get; set; }
        public string? password { get; set; } 
        public LoginReq() { msg = MsgType.LOGIN_REQ; }
    }
    public class LoginRes : BaseMessage 
    {
        public bool logined { get; set; } 
        public LoginRes() { msg = MsgType.LOGIN_RES; }
    }

    // 시동
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

    // 도어
    public class DoorReq : BaseMessage 
    { 
        public bool door { get; set; }
        public DoorReq() { msg = MsgType.DOOR_REQ; }
    }
    public class DoorRes : BaseMessage 
    {
        public bool door_status { get; set; }
        public DoorRes() { msg = MsgType.DOOR_RES; }
    }

    // 트렁크
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

    // 에어컨
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

    // 온도조절
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

    // 열선
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

    // 라이트
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

    // 원격 주차 제어
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

    // 충전 중지
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

    // 상태 확인
    public class StatusReq : BaseMessage
    {
        public bool car_status { get; set; }
        public StatusReq() { msg = MsgType.STATUS_REQ; }
    }
    public class StatusRes : BaseMessage
    {
        public bool charging { get; set; }
        public bool parking { get; set; }
        public bool driving { get; set; }
        public double battery { get; set; }
        public StatusRes() { msg = MsgType.STATUS_RES; }
    }
}