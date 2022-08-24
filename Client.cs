using Godot;
using GodotLogger;
using Newtonsoft.Json.Linq;
using PinusClient.Protocol;
using PinusClient.Transporter;
using System;
using System.Text;

namespace PinusClient
{
    class Client : Node, IConnection
    {
        private static readonly Logger _log = LoggerHelper.GetLogger(typeof(Client));

        #region IObservable<NetworkStatus>

        public event Action<NetworkStatus> OnNetworkStatusChanged;
        public NetworkStatus _networkStatus = NetworkStatus.Unknown;

        public void NetworkStatusChanged(NetworkStatus status)
        {
            if (_networkStatus != status)
            {
                _networkStatus = status;

                OnNetworkStatusChanged?.Invoke(_networkStatus);

                if (status == NetworkStatus.Connected)
                {
                    // 连接已建立，开始握手
                    NetworkStatusChanged(NetworkStatus.Handshaking);

                    byte[] handshakeBytes = HandShakeService.GetHandShakeBytes();
                    byte[] handshakePackage = PackageProtocol.Encode(PackageType.PKG_HANDSHAKE, handshakeBytes);

                    _transporter.Send(handshakePackage);
                }
                else if (status == NetworkStatus.Ready)
                {
                }
            }
        }

        #endregion

        #region ITransporter

        private ITransporter _transporter = null;

        public void ConnectTo(IConnectionParameter param)
        {
            _log.Debug("connect to: " + param);

            DisconnectFrom();

            if (param is WebSocketParameter)
            {
                WebSocketTransporter ws = new WebSocketTransporter();
                ws.OnNetworkStatusChanged += NetworkStatusChanged;
                ws.OnPackageReceived += DataReceived;
                ws.ConnectTo(param);

                _transporter = ws;
            }
        }

        public void DisconnectFrom()
        {
            _log.Debug("disconnect");

            _transporter?.DisconnectFrom();
        }

        public override void _Process(float delta)
        {
            if (_networkStatus >= NetworkStatus.Connecting)
            {
                _transporter?.Process(delta);

                if (_networkStatus == NetworkStatus.Ready)
                {
                    _heartBeatService?.Process(delta);
                }
            }
        }

        #endregion

        #region Package

        public event Action<JObject> OnHandshakeCompleted;

        private HeartBeatService _heartBeatService;

        private MessageProtocol _messageProtocol;

        private void DataReceived(byte[] package)
        {
            // 收到来自网络的封包，进行处理
            Package pkg = PackageProtocol.Decode(package);

            //Ignore all the message except handshading at handshake stage
            if (pkg.Type == PackageType.PKG_HANDSHAKE && this._networkStatus == NetworkStatus.Handshaking)
            {
                //Ignore all the message except handshading
                //var data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(pkg.body));
                JObject data = JObject.Parse(Encoding.UTF8.GetString(pkg.Body));
                ProcessHandshakeData(data);
            }
            else if (pkg.Type == PackageType.PKG_HEARTBEAT && this._networkStatus == NetworkStatus.Ready)
            {
                _log.Debug("heartbeat received");

                this._heartBeatService.Reset();
            }
            else if (pkg.Type == PackageType.PKG_DATA && this._networkStatus == NetworkStatus.Ready)
            {
                this._heartBeatService.Reset();
                ProcessMessage(_messageProtocol.decode(pkg.Body));
            }
            else if (pkg.Type == PackageType.PKG_KICK)
            {
                DisconnectFrom();
            }
        }

        private void ProcessHandshakeData(JObject msg)
        {
            //Handshake error
            if (!msg.ContainsKey("code") || !msg.ContainsKey("sys") || Convert.ToInt32(msg["code"]) != 200)
            {
                throw new Exception("Handshake error! Please check your handshake config.");
            }

            //Set compress data
            JObject sys = (JObject)msg["sys"];

            JObject dict = new JObject();
            if (sys.ContainsKey("dict")) dict = (JObject)sys["dict"];

            JObject protos = new JObject();
            JObject serverProtos = new JObject();
            JObject clientProtos = new JObject();

            if (sys.ContainsKey("protos"))
            {
                protos = (JObject)sys["protos"];
                serverProtos = (JObject)protos["server"];
                clientProtos = (JObject)protos["client"];
            }

            _messageProtocol = new MessageProtocol(dict, serverProtos, clientProtos);

            //Init heartbeat service
            int interval = 0;
            if (sys.ContainsKey("heartbeat")) interval = Convert.ToInt32(sys["heartbeat"]);
            _heartBeatService = new HeartBeatService(interval);

            if (interval > 0)
            {
                _heartBeatService.Start();
                _heartBeatService.HeartBeat += _OnHeartbeat;
                _heartBeatService.Timeout += _OnHeartbeatTimeout;
            }

            //send ack and change protocol state
            HandshakeAck();

            //Invoke handshake callback
            JObject user = msg.ContainsKey("user") ? (JObject)msg["user"] : new JObject();
            OnHandshakeCompleted?.Invoke(user);

            NetworkStatusChanged(NetworkStatus.Ready);
        }

        private void HandshakeAck()
        {
            Send(PackageType.PKG_HANDSHAKE_ACK);
        }

        private void _OnHeartbeat()
        {
            _log.Debug("heartbeat send");

            Send(PackageType.PKG_HEARTBEAT);
        }

        private void _OnHeartbeatTimeout()
        {
            _log.Warn("heartbeat timeout!");
        }

        #endregion

        #region Message

        private readonly EventManager _eventManager = new EventManager();

        private void ProcessMessage(Message msg)
        {
            if (msg == null) return;

            if (msg.Type == MessageType.MSG_RESPONSE)
            {
                //msg.data["__route"] = msg.route;
                //msg.data["__type"] = "resp";
                _eventManager.InvokeCallBack(msg.Id, msg.Data);
            }
            else if (msg.Type == MessageType.MSG_PUSH)
            {
                //msg.data["__route"] = msg.route;
                //msg.data["__type"] = "push";
                _eventManager.InvokeOnEvent(msg.Route, msg.Data);
            }
        }

        private static readonly byte[] emptyPackage = new byte[0];
        private void Send(PackageType type)
        {
            Send(type, emptyPackage);
        }

        private void Send(PackageType type, byte[] data)
        {
            if (_networkStatus >= NetworkStatus.Connected)
            {
                byte[] pkg = PackageProtocol.Encode(type, data);
                _transporter?.Send(pkg);
            }
        }

        #endregion

        #region Client

        private static readonly object emptyMessage = new { };

        private uint requestId = 1;

        public void Request(string route, Action<JObject> callback)
        {
            Request(route, emptyMessage, callback);
        }

        public void Request(string route, in object payload, Action<JObject> callback)
        {
            // _log.Debug($"request to {route} with payload {payload} in requestId {requestId}");
            if (_networkStatus == NetworkStatus.Ready)
            {

                _eventManager.AddCallBack(requestId, callback);
                ProtocolSend(route, requestId, payload);

                requestId++;
                // 自己单独加回的请求 id 循环逻辑，和早期 Pomelo 协议基本吻合（早期 Pomelo 协议到 127 就循环）
                if (requestId == 0xFF)
                {
                    requestId = 1;
                }

            }
        }

        public void On(string route, Action<JObject> callback)
        {
            _eventManager.AddOnEvent(route, callback);
        }

        private void ProtocolSend(string route, object payload)
        {
            ProtocolSend(route, 0, payload);
        }

        private void ProtocolSend(string route, uint requestId, object payload)
        {
            byte[] package = _messageProtocol.encode(route, requestId, JObject.FromObject(payload));

            Send(PackageType.PKG_DATA, package);
        }

        #endregion

    }
}