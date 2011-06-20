namespace Mono.Security.Cryptography
{
    public enum PaddingMode
    {
        None = 0x1,
        PKCS7,
        Zeros,
        ANSIX923,
        ISO10126
    }
}
