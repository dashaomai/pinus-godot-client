using System;
using System.Text;
using Newtonsoft.Json;

namespace PinusClient.Protocol
{
    public class HandShakeService
    {
        private static readonly string Version = "0.3.0";
        private static readonly string Type = "godot-websocket";


        /// <summary>
        /// 获得握手数据包
        /// </summary>
        public static byte[] GetHandShakeBytes(in object user = null)
        {
            object user2 = user != null ? user : new object();
            return Encoding.UTF8.GetBytes(buildMsg(user2));
        }

        private static string buildMsg(in object user)
        {
            //Build sys option
            var sys = new { version = Version, type = Type };
            //Build handshake message
            var msg = new { sys = sys, user = user };

            return JsonConvert.SerializeObject(msg);
        }
    }
}