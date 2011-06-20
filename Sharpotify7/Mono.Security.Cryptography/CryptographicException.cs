using System;

namespace Mono.Security.Cryptography
{
    public class CryptographicException : SystemException
    {
        public CryptographicException()
            : base("Error occured during a cryptographic operation.")
        {
            // default to CORSEC_E_CRYPTO
            // defined as EMAKEHR(0x1430) in CorError.h
            HResult = unchecked((int)0x80131430);
        }

        public CryptographicException(int hr)
        {
            HResult = hr;
        }

        public CryptographicException(string message)
            : base(message)
        {
            HResult = unchecked((int)0x80131430);
        }

        public CryptographicException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = unchecked((int)0x80131430);
        }

        public CryptographicException(string format, string insert)
            : base(String.Format(format, insert))
        {
            HResult = unchecked((int)0x80131430);
        }
    }
}
