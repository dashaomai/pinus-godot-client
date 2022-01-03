using System;

namespace PinusClient.Protocol
{
    /// <summary>
    /// 掌管心跳节奏的服务
    /// </summary>
    public class HeartBeatService
    {
        private static readonly Logger _log = LoggerHelper.GetLogger(typeof(HeartBeatService));

        internal event Action Timeout;
        internal event Action HeartBeat;

        private int Interval { get; }

        /// <summary>
        /// 下次心跳前累积时间
        /// </summary>
        private float AccumulateDuration { get; set; }
        /// <summary>
        /// 超时时间戳
        /// </summary>
        private int TimeoutDuration { get; }

        private bool IsStarted { get; set; }

        public HeartBeatService(int seconds)
        {
            Interval = seconds;
            TimeoutDuration = Interval * 2;

            Reset();
        }

        /// <summary>
        /// 定时回调函数，由 Godot _Process 方法驱动 
        /// </summary>
        public void Process(float delta)
        {
            if (IsStarted)
            {
                AccumulateDuration += delta;

                if (AccumulateDuration >= Interval)
                {
                    if (AccumulateDuration < TimeoutDuration)
                    {
                        // 达到可心跳时间
                        HeartBeat?.Invoke();
                        AccumulateDuration = 0;
                    }
                    else
                    {
                        // 心跳超时
                        Timeout?.Invoke();
                    }
                }
            }
        }

        public void Start()
        {
            IsStarted = true;
        }

        public void Reset()
        {
            AccumulateDuration = 0;
        }
    }
}