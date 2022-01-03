namespace PinusClient.Protocol
{
    public enum ProtocolState
    {
        Unknown = 0,
        Start = 1,          // Just open, need to send handshaking
        Handshaking = 2,    // on handshaking process
        Working = 3,		// can receive and send data 
        Closed = 4,		    // on read body
    }
}