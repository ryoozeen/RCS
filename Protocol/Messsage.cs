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
        public bool Success { get; set; }
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
    // 상태 전송
    public class StatusReq : BaseMessage
    {
        public double Charging { get; set; }

        public StatusReq()
        {
            Msg = MsgType.STATUS_REQ;
        }
    }
    // 상태 응답
    public class StatusRes : BaseMessage
    {
        public bool Level { get; set; }
        public StatusRes()
        {
            Msg = MsgType.STATUS_RES;
        }
    }
}