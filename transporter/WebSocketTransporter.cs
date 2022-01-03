using System;
using Godot;

namespace PinusClient.Transporter
{
    public class WebSocketTransporter : Reference, ITransporter
    {
        private static readonly Logger _log = LoggerHelper.GetLogger(typeof(WebSocketTransporter));

        public delegate void OnPackageReceivedHandler(byte[] package);
        public event OnPackageReceivedHandler OnPackageReceived;

        private readonly WebSocketClient _client = new WebSocketClient();

        internal WebSocketTransporter()
        {
            _client.Connect("connection_established", this, nameof(_OnConnected));
            _client.Connect("data_received", this, nameof(_OnDataReceived));
            _client.Connect("connection_error", this, nameof(_OnConnectError));
            _client.Connect("connection_closed", this, nameof(_OnConnectClosed));
            _client.Connect("server_close_request", this, nameof(_OnServerCloseRequest));
        }

        public event Action<NetworkStatus> OnNetworkStatusChanged;
        private NetworkStatus _networkStatus = NetworkStatus.Unknown;

        public void NetworkStatusChanged(NetworkStatus status)
        {
            if (_networkStatus != status)
            {
                _networkStatus = status;

                OnNetworkStatusChanged?.Invoke(_networkStatus);
            }
        }

        public void ConnectTo(IConnectionParameter param)
        {
            if (param is WebSocketParameter)
            {
                var param2 = (WebSocketParameter)param;
                _log.Debug("prepare to connect to server: " + param2.Url);

                var code = _client.ConnectToUrl(param2.Url);
                NetworkStatusChanged(code == Error.Ok ? NetworkStatus.Connecting : NetworkStatus.Closed);
            }
            else
            {
                _log.Warn("param type error");
            }
        }

        public void DisconnectFrom()
        {
            _log.Debug("disconnect");

            _client?.DisconnectFromHost();

            NetworkStatusChanged(NetworkStatus.Closed);
        }

        private void _OnConnected(string protocol = "")
        {
            _log.Debug("connected with protocol: " + protocol);

            NetworkStatusChanged(NetworkStatus.Connected);
        }

        private void _OnDataReceived()
        {
            _log.Debug("data received");

            byte[] package = _client.GetPeer(1).GetPacket();
            OnPackageReceived?.Invoke(package);
        }

        private void _OnConnectError()
        {
            _log.Debug("connection error");

            NetworkStatusChanged(NetworkStatus.Closed);
        }

        private void _OnConnectClosed(bool wasCleanClose)
        {
            _log.Debug("connection closed: " + wasCleanClose);

            NetworkStatusChanged(NetworkStatus.Closed);
        }

        private void _OnServerCloseRequest(int code, string reason)
        {
            _log.Debug($"server request close with code {code} and reason: {reason}");

            NetworkStatusChanged(NetworkStatus.Closed);
        }

        public void Process(float delta)
        {
            if (_networkStatus == NetworkStatus.Connected || _networkStatus == NetworkStatus.Connecting)
            {
                _client?.Poll();
            }
        }

        public void Send(in byte[] data)
        {
            if (_networkStatus == NetworkStatus.Connected)
            {
                _client?.GetPeer(1).PutPacket(data);
            }
        }
    }
}