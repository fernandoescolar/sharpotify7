using System.Security.Cryptography;

namespace Mono.Security.Cryptography
{
    public class KeyBuilder
    {
        private static RandomNumberGenerator rng;

        private static RandomNumberGenerator Rng
        {
            get
            {
                if (KeyBuilder.rng == null)
                {
                    KeyBuilder.rng = new RNGCryptoServiceProvider();
                }
                return KeyBuilder.rng;
            }
        }

        private KeyBuilder()
            : base()
        {
        }

        public static byte[] IV(int size)
        {
            byte[] array = new byte[size];
            KeyBuilder.Rng.GetBytes(array);
            return array;
        }

        public static byte[] Key(int size)
        {
            byte[] array = new byte[size];
            KeyBuilder.Rng.GetBytes(array);
            return array;
        }
    }
}
