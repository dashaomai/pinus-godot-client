using Newtonsoft.Json.Linq;

namespace PinusClient.Protocol
{
    public class Message
    {
        public MessageType Type { get; }
        public string Route { get; }
        public uint Id { get; }
        public JObject Data { get; }

        public Message(MessageType type, uint id, string route, JObject data)
        {
            Type = type;
            Id = id;
            Route = route;
            Data = data;
        }
    }
}