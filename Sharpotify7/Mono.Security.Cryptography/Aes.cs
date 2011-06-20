namespace Mono.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {

        public static new Aes Create()
        {
            return Create("System.Security.Cryptography.AesManaged");
        }

        public static new Aes Create(string algName)
        {
            return (Aes)new AesManaged();
        }

        protected Aes()
        {
            KeySizeValue = 256;
            BlockSizeValue = 128;
#if !NET_2_1
            // Silverlight 2.0 only supports CBC mode (i.e. no feedback)
            FeedbackSizeValue = 128;
#endif
            LegalKeySizesValue = new KeySizes[1];
            LegalKeySizesValue[0] = new KeySizes(128, 256, 64);

            LegalBlockSizesValue = new KeySizes[1];
            LegalBlockSizesValue[0] = new KeySizes(128, 128, 0);
        }
    }
}
