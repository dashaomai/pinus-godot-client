namespace PinusClient
{
    public interface IConnection
    {
        void ConnectTo(IConnectionParameter parameter);
        void DisconnectFrom();

        void NetworkStatusChanged(NetworkStatus status);
    }
}