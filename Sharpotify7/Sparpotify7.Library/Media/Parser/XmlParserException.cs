using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sharpotify.Media.Parser
{
    /// <summary>
    /// An exception that is thrown if XML parsing failed.
    /// </summary>
    public class XmlParserException : Exception
    {
        public XmlParserException(string message) : base(message) { }
        public XmlParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}
