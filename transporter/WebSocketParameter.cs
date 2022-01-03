namespace PinusClient.Transporter
{
    readonly public struct WebSocketParameter : IConnectionParameter
    {
        public string Url { get; }

        public WebSocketParameter(string url)
        {
            Url = url;
        }
    }
}