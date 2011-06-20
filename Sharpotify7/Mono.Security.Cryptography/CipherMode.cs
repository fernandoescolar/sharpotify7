namespace Mono.Security.Cryptography
{
    public enum CipherMode
    {
        CBC = 0x1, // Cipher Block Chaining
        ECB, // Electronic Codebook
        OFB, // Output Feedback
        CFB, // Cipher Feedback
        CTS, // Cipher Text Stealing
    }
}
