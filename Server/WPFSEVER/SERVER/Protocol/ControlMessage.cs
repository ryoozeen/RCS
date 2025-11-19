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
            byte[] lengthBytes = new byte[4];

            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            if (bytesRead != 4) return null;

            int messageLength = BitConverter.ToInt32(lengthBytes, 0);
            if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE) return null;

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
                // 먼저 BaseMessage로 파싱하여 msg 타입 확인
                using JsonDocument doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("msg", out JsonElement msgElement))
                    return null;

                MsgType msgType = (MsgType)Enum.Parse(typeof(MsgType), msgElement.GetString() ?? "");

                // msg 타입에 따라 적절한 클래스로 역직렬화
                BaseMessage? message = msgType switch
                {
                    MsgType.ENROLL_REQ => JsonSerializer.Deserialize<EnrollReq>(json),
                    MsgType.ENROLL_RES => JsonSerializer.Deserialize<EnrollRes>(json),
                    MsgType.LOGIN_REQ => JsonSerializer.Deserialize<LoginReq>(json),
                    MsgType.LOGIN_RES => JsonSerializer.Deserialize<LoginRes>(json),
                    MsgType.START_REQ => JsonSerializer.Deserialize<StartReq>(json),
                    MsgType.START_RES => JsonSerializer.Deserialize<StartRes>(json),
                    MsgType.DOOR_REQ => JsonSerializer.Deserialize<DoorReq>(json),
                    MsgType.DOOR_RES => JsonSerializer.Deserialize<DoorRes>(json),
                    MsgType.TRUNK_REQ => JsonSerializer.Deserialize<TrunkReq>(json),
                    MsgType.TRUNK_RES => JsonSerializer.Deserialize<TrunkRes>(json),
                    MsgType.AIR_REQ => JsonSerializer.Deserialize<AirReq>(json),
                    MsgType.AIR_RES => JsonSerializer.Deserialize<AirRes>(json),
                    MsgType.CLI_REQ => JsonSerializer.Deserialize<CliReq>(json),
                    MsgType.CLI_RES => JsonSerializer.Deserialize<CliRes>(json),
                    MsgType.HEAT_REQ => JsonSerializer.Deserialize<HeatReq>(json),
                    MsgType.HEAT_RES => JsonSerializer.Deserialize<HeatRes>(json),
                    MsgType.LIGHT_REQ => JsonSerializer.Deserialize<LightReq>(json),
                    MsgType.LIGHT_RES => JsonSerializer.Deserialize<LightRes>(json),
                    MsgType.CONTROL_REQ => JsonSerializer.Deserialize<ControlReq>(json),
                    MsgType.CONTROL_RES => JsonSerializer.Deserialize<ControlRes>(json),
                    MsgType.STATUS_REQ => JsonSerializer.Deserialize<StatusReq>(json),
                    MsgType.STATUS_RES => JsonSerializer.Deserialize<StatusRes>(json),
                    MsgType.STOP_CHARGING_REQ => JsonSerializer.Deserialize<StopChargingReq>(json),
                    MsgType.STOP_CHARGING_RES => JsonSerializer.Deserialize<StopChargingRes>(json),
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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

    // 회원가입 요청
    public class EnrollReq : BaseMessage
    {
        public string? id { get; set; }
        public string? password { get; set; }
        public string? username { get; set; }
        public string? carmodel { get; set; }

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

    // 문 제어 요청
    public class DoorReq : BaseMessage
    {
        public bool open { get; set; }

        public DoorReq()
        {
            msg = MsgType.DOOR_REQ;
        }
    }

    // 문 제어 응답
    public class DoorRes : BaseMessage
    {
        public bool doorstatus { get; set; }

        public DoorRes()
        {
            msg = MsgType.DOOR_RES;
        }
    }

    // 상태 요청 (클라이언트 → 서버)
    public class StatusReq : BaseMessage
    {
        public bool carstatus { get; set; }

        public StatusReq()
        {
            msg = MsgType.STATUS_REQ;
        }
    }

    // 상태 응답 (서버 → 클라이언트)
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

    // 시동 제어
    public class StartReq : BaseMessage
    {
        public bool active { get; set; }
        public StartReq() { msg = MsgType.START_REQ; }
    }

    public class StartRes : BaseMessage
    {
        public bool activestatus { get; set; }
        public StartRes() { msg = MsgType.START_RES; }
    }

    // 트렁크 제어
    public class TrunkReq : BaseMessage
    {
        public bool open { get; set; }
        public TrunkReq() { msg = MsgType.TRUNK_REQ; }
    }

    public class TrunkRes : BaseMessage
    {
        public bool open { get; set; }
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
        public bool airstatus { get; set; }
        public AirRes() { msg = MsgType.AIR_RES; }
    }

    // 온도 제어
    public class CliReq : BaseMessage
    {
        public int Temp { get; set; }
        public CliReq() { msg = MsgType.CLI_REQ; }
    }

    public class CliRes : BaseMessage
    {
        public bool tempresult { get; set; }
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
        public bool heatstatus { get; set; }
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
        public bool lightstatus { get; set; }
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
        public bool controlstatus { get; set; }
        public ControlRes() { msg = MsgType.CONTROL_RES; }
    }

    public class StopChargingReq : BaseMessage
    {
        public bool stop { get; set; }
        public StopChargingReq() { msg = MsgType.STOP_CHARGING_REQ; }
    }

    public class StopChargingRes : BaseMessage
    {
        public bool stopstatus { get; set; }
        public StopChargingRes() { msg = MsgType.STOP_CHARGING_RES; }
    }
}
