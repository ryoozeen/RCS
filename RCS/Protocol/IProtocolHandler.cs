namespace DotBotCarClient.Protocol
{
    public interface IProtocolHandler
    {
        void HandleProtocolMessage(BaseMessage msg);
    }
}