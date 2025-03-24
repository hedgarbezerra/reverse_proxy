
namespace Common
{
    public static partial class Constants
    {
        public static class Jwt
        {
            public const string Scheme = "default-authentication";
            public const string Issuer = "webapi01";
            public static byte[] ApiKey { get; } = @"MinhaChaveSuperSecretaCom32Caracteres@"u8.ToArray();

            public static class Claims
            {
                public const string System = "webapi01";
            }
        }
    }
}
