﻿using System;
using System.IO;
using System.Xml;
using System.Text;

namespace Sharpotify.Media.Parser
{
    public class XmlParser
    {
        private bool hasJustRead = false;
        protected XmlReader reader;
        protected string myXml = string.Empty;

        /// <summary>
        /// Create a new stream parser from the given input stream.
        /// </summary>
        /// <param name="stream">An <see cref="StreamReader"/> stream to parse.</param>
        protected XmlParser(Stream stream)
        {
            
            StreamReader r = new StreamReader(stream);
            myXml = r.ReadToEnd();
            //byte[] buffer = Encoding.UTF8.GetBytes(myXml);
            //MemoryStream ms = new MemoryStream(buffer);
            reader = XmlReader.Create(new StringReader(myXml));
	    }
        /// <summary>
        /// Get an attributes value.
        /// </summary>
        /// <param name="attribute">An attribute name.</param>
        /// <returns></returns>
        protected string GetAttributeString(string attribute)
        {
            return this.reader.GetAttribute(attribute);
        }
        /// <summary>
        /// Get the current elements contents as string.
        /// </summary>
        /// <returns>A string.</returns>
        protected string GetElementString()
        {
            //if (reader.NodeType == XmlNodeType.Whitespace) reader.Read();
            string aux = reader.ReadElementContentAsString();
            //while (reader.NodeType != XmlNodeType.EndElement) { reader.Read(); }
            //Next();
            hasJustRead = true;
            return aux;
        }
        /// <summary>
        /// Get the current elements contents an integer.
        /// </summary>
        /// <returns>An integer.</returns>
        protected int GetElementInteger()
        {
            try
            {
                return int.Parse(GetElementString());
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        /// <summary>
        /// Get the current elements contents a floating-point number.
        /// </summary>
        /// <returns>A float.</returns>
        protected float GetElementFloat()
        {
            try
            {
                string aux = GetElementString();
                //aux = aux.Replace(".", ",");
                return float.Parse(aux, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        /// <summary>
        /// Get the current elements contents a boolean.
        /// </summary>
        /// <returns>A bool.</returns>
        protected bool GetElementBoolean()
        {
            try
            {
                return bool.Parse(GetElementString());
            }
            catch (Exception e)
            {
                return false;
            }
        }

        protected bool Next()
        {
            if (reader.EOF)
                return false;

            if (hasJustRead)
            {
                hasJustRead = false;
            }
            else
            {
                reader.Read();
            }

            while (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.EOF)
                    return false;
                reader.Read();
            }
            return true;
        }
    }
}
