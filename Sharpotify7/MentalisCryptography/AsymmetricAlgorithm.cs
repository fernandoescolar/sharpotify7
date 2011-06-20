using System;
using System.Security.Cryptography;

namespace Org.Mentalis.Security.Cryptography
{
    // Summary:
    //     Represents the abstract base class from which all implementations of asymmetric
    //     algorithms must inherit.
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        // Summary:
        //     Represents the size, in bits, of the key modulus used by the asymmetric algorithm.
        protected int KeySizeValue;
        //
        // Summary:
        //     Specifies the key sizes that are supported by the asymmetric algorithm.
        protected KeySizes[] LegalKeySizesValue;

        // Summary:
        //     Initializes a new instance of the System.Security.Cryptography.AsymmetricAlgorithm
        //     class.
        //
        // Exceptions:
        //   System.Security.Cryptography.CryptographicException:
        //     The implementation of the derived class is not valid.
        protected AsymmetricAlgorithm() { }

        // Summary:
        //     When overridden in a derived class, gets the name of the key exchange algorithm.
        //
        // Returns:
        //     The name of the key exchange algorithm.
        public abstract string KeyExchangeAlgorithm { get; }
        //
        // Summary:
        //     Gets or sets the size, in bits, of the key modulus used by the asymmetric
        //     algorithm.
        //
        // Returns:
        //     The size, in bits, of the key modulus used by the asymmetric algorithm.
        //
        // Exceptions:
        //   System.Security.Cryptography.CryptographicException:
        //     The key modulus size is invalid.
        public virtual int KeySize { get; set; }
        //
        // Summary:
        //     Gets the key sizes that are supported by the asymmetric algorithm.
        //
        // Returns:
        //     An array that contains the key sizes supported by the asymmetric algorithm.
        public virtual KeySizes[] LegalKeySizes { get; internal set; }
        //
        // Summary:
        //     Gets the name of the signature algorithm.
        //
        // Returns:
        //     The name of the signature algorithm.
        public abstract string SignatureAlgorithm { get; }

        // Summary:
        //     Releases all resources used by the System.Security.Cryptography.AsymmetricAlgorithm
        //     class.
        public void Clear()
        { 
        
        }

        //
        // Summary:
        //     Creates a default cryptographic object used to perform the asymmetric algorithm.
        //
        // Returns:
        //     The cryptographic object used to perform the asymmetric algorithm.
        public static AsymmetricAlgorithm Create() { return null; }
        //
        // Summary:
        //     Creates the specified cryptographic object used to perform the asymmetric
        //     algorithm.
        //
        // Parameters:
        //   algName:
        //     The name of the specific implementation of System.Security.Cryptography.AsymmetricAlgorithm
        //     to use.
        //
        // Returns:
        //     A cryptographic object used to perform the asymmetric algorithm.
        public static AsymmetricAlgorithm Create(string algName) { return null; }
        //
        // Summary:
        //     When overridden in a derived class, releases the unmanaged resources used
        //     by the System.Security.Cryptography.AsymmetricAlgorithm and optionally releases
        //     the managed resources.
        //
        // Parameters:
        //   disposing:
        //     true to release both managed and unmanaged resources; false to release only
        //     unmanaged resources.
        protected abstract void Dispose(bool disposing);
        //
        // Summary:
        //     When overridden in a derived class, reconstructs an System.Security.Cryptography.AsymmetricAlgorithm
        //     object from an XML string.
        //
        // Parameters:
        //   xmlString:
        //     The XML string to use to reconstruct the System.Security.Cryptography.AsymmetricAlgorithm
        //     object.
        public abstract void FromXmlString(string xmlString);
        //
        // Summary:
        //     When overridden in a derived class, creates and returns an XML string representation
        //     of the current System.Security.Cryptography.AsymmetricAlgorithm object.
        //
        // Parameters:
        //   includePrivateParameters:
        //     true to include private parameters; otherwise, false.
        //
        // Returns:
        //     An XML string encoding of the current System.Security.Cryptography.AsymmetricAlgorithm
        //     object.
        public abstract string ToXmlString(bool includePrivateParameters);

        public void Dispose()
        {
            this.Dispose(true);
        }
    }
}
