namespace PinusClient.Transporter
{
    public interface ITransporter : IConnection, IProcessor
    {
        void Send(in byte[] data);
    }
}