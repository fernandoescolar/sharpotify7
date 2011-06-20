namespace Mono.Security.Cryptography
{
    public sealed class AesCryptoServiceProvider : Aes
    {

        public AesCryptoServiceProvider()
        {
        }

        public override void GenerateIV()
        {
            IVValue = KeyBuilder.IV(BlockSizeValue >> 3);
        }

        public override void GenerateKey()
        {
            KeyValue = KeyBuilder.Key(KeySizeValue >> 3);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new AesTransform(this, false, rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return new AesTransform(this, true, rgbKey, rgbIV);
        }

        // I suppose some attributes differs ?!? because this does not look required

        public override byte[] IV
        {
            get { return base.IV; }
            set { base.IV = value; }
        }

        public override byte[] Key
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        public override int KeySize
        {
            get { return base.KeySize; }
            set { base.KeySize = value; }
        }
#if false
    public override int FeedbackSize {
      get { return base.FeedbackSize; }
      set { base.FeedbackSize = value; }
    }

    public override CipherMode Mode {
      get { return base.CipherMode; }
      set { base.CipherMode = value; }
    }

    public override PaddingMode Padding {
      get { return base.PaddingMode; }
      set { base.PaddingMode = value; }
    }
#endif
        public override ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(Key, IV);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(Key, IV);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
