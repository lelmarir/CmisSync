using System;
using System.Collections.Generic;

using System.Xml.Serialization;
using System.Xml;

namespace CmisSync.Lib
{
    /// <summary>
    /// XML URI.
    /// </summary>
    public class XmlUri : IXmlSerializable
    {
        private Uri _Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlUri() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlUri(Uri source) { _Value = source; }

        /// <summary>
        /// implicit.
        /// </summary>
        public static implicit operator Uri(XmlUri o)
        {
            return o == null ? null : o._Value;
        }

        /// <summary>
        /// implicit.
        /// </summary>
        public static implicit operator XmlUri(Uri o)
        {
            return o == null ? null : new XmlUri(o);
        }

        /// <summary>
        /// Get schema.
        /// </summary>
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Read XML.
        /// </summary>
        public void ReadXml(XmlReader reader)
        {
            _Value = new Uri(reader.ReadElementContentAsString());
        }

        /// <summary>
        /// Write XML.
        /// </summary>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteValue(_Value.ToString());
        }

        /// <summary>
        /// String representation of the URI.
        /// </summary>
        public string ToString()
        {
            return _Value.ToString();
        }

        //delegating methods

        // Summary:
        //     Gets the absolute path of the URI.
        //
        // Returns:
        //     A System.String containing the absolute path to the resource.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     This instance represents a relative URI, and this property is valid only
        //     for absolute URIs.
        public string AbsolutePath { get { return _Value.AbsolutePath; } }

        //
        // Summary:
        //     Gets the absolute URI.
        //
        // Returns:
        //     A System.String containing the entire URI.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     This instance represents a relative URI, and this property is valid only
        //     for absolute URIs.
        public string AbsoluteUri { get { return _Value.AbsoluteUri; } }
    }
}
