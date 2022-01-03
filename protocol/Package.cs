namespace PinusClient.Protocol
{
    readonly public struct Package
    {
        public PackageType Type { get; }
        public int Length { get; }
        public byte[] Body { get; }

        public Package(PackageType type, byte[] body)
        {
            Type = type;
            Length = body.Length;
            Body = body;
        }
    }
}