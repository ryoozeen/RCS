namespace DotBotCarClient.Protocol
{
    // 메시지 타입
    public enum MsgType
    {
        CLIENT_IDENTIFY,
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
    }
    public class ClientIdentify : BaseMessage
    {
        public string? client_name { get; set; }
        public ClientIdentify()
        {
            msg = MsgType.CLIENT_IDENTIFY;
        }
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