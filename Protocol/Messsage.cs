namespace DotBotCarClient.Protocol
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
        STATUS_RES
    }
    // 공통 메시지(기반 클래스)
    public class BaseMessage
    {
        public MsgType Msg { get; set; }
        public string? Reason { get; set; }
    }
    // 회원가입 요청
    public class EnrollReq : BaseMessage
    {
        public string? Id { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
        public string? CarModel { get; set; }

        public EnrollReq()
        {
            Msg = MsgType.ENROLL_REQ;
        }
    }
    // 회원가입 응답
    public class EnrollRes : BaseMessage
    {
        public bool Registered { get; set; }

        public EnrollRes()
        {
            Msg = MsgType.ENROLL_RES;
        }
    }
    public class LoginReq : BaseMessage
    {
        public string? Id { get; set; }
        public string? Password { get; set; }

        public LoginReq()
        {
            Msg = MsgType.LOGIN_REQ;
        }
    }
    public class LoginRes : BaseMessage
    {
        public bool Logined { get; set; }

        public LoginRes()
        {
            Msg = MsgType.LOGIN_RES;
        }
    }
    // 문 제어 요청
    public class DoorReq : BaseMessage
    {
        public bool Open { get; set; }

        public DoorReq()
        {
            Msg = MsgType.DOOR_REQ;
        }
    }
    // 문 제어 응답
    public class DoorRes : BaseMessage
    {
        public string? DoorStatus { get; set; }

        public DoorRes()
        {
            Msg = MsgType.DOOR_RES;
        }
    }

    // 상태 요청 (클라이언트 → 서버)
    public class StatusReq : BaseMessage
    {
        public bool resulted { get; set; }  // 서버가 필요하면 보냄
        public StatusReq()
        {
            Msg = MsgType.STATUS_REQ;
        }
    }

    // 상태 응답 (서버 → 클라이언트)
    public class StatusRes : BaseMessage
    {
        public bool Charging { get; set; }
        public bool Parking { get; set; }
        public bool Driving { get; set; }
        public double Battery { get; set; }

        public StatusRes()
        {
            Msg = MsgType.STATUS_RES;
        }
    }

    // 시동 제어
    public class StartReq : BaseMessage
    {
        public bool Active { get; set; }
        public StartReq() { Msg = MsgType.START_REQ; }
    }

    public class StartRes : BaseMessage
    {
        public bool Active { get; set; }
        public StartRes() { Msg = MsgType.START_RES; }
    }

    // 트렁크 제어
    public class TrunkReq : BaseMessage
    {
        public bool Open { get; set; }
        public TrunkReq() { Msg = MsgType.TRUNK_REQ; }
    }

    public class TrunkRes : BaseMessage
    {
        public bool Open { get; set; }
        public TrunkRes() { Msg = MsgType.TRUNK_RES; }
    }

    // 에어컨 제어
    public class AirReq : BaseMessage
    {
        public bool On { get; set; }
        public AirReq() { Msg = MsgType.AIR_REQ; }
    }

    public class AirRes : BaseMessage
    {
        public bool On { get; set; }
        public AirRes() { Msg = MsgType.AIR_RES; }
    }

    // 온도 제어
    public class CliReq : BaseMessage
    {
        public int Temp { get; set; }
        public CliReq() { Msg = MsgType.CLI_REQ; }
    }

    public class CliRes : BaseMessage
    {
        public bool Result { get; set; }
        public CliRes() { Msg = MsgType.CLI_RES; }
    }

    // 열선 제어
    public class HeatReq : BaseMessage
    {
        public bool On { get; set; }
        public HeatReq() { Msg = MsgType.HEAT_REQ; }
    }

    public class HeatRes : BaseMessage
    {
        public bool On { get; set; }
        public HeatRes() { Msg = MsgType.HEAT_RES; }
    }

    // 라이트 제어
    public class LightReq : BaseMessage
    {
        public bool On { get; set; }
        public LightReq() { Msg = MsgType.LIGHT_REQ; }
    }

    public class LightRes : BaseMessage
    {
        public bool On { get; set; }
        public LightRes() { Msg = MsgType.LIGHT_RES; }
    }

    // 원격주차 제어(UP/DOWN 또는 PARK/DRIVE)
    public class ControlReq : BaseMessage
    {
        public bool Active { get; set; }
        public ControlReq() { Msg = MsgType.CONTROL_REQ; }
    }

    public class ControlRes : BaseMessage
    {
        public bool Active { get; set; }
        public ControlRes() { Msg = MsgType.CONTROL_RES; }
    }

  
}