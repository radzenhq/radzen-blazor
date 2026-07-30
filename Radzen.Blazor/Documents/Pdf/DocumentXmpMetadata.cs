using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Radzen.Documents.Pdf;

/// <summary>Provides raw and simple-property access to a document XMP packet.</summary>
public sealed class DocumentXmpMetadata
{
    private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private byte[]? packet;
    private Action? changed;

    internal void OwnedBy(Action? owner) => changed = owner;

    private bool modified;

    internal bool IsModified
    {
        get => modified;
        private set
        {
            modified = value;
            if (value)
            {
                changed?.Invoke();
            }
        }
    }

    internal bool HasPacket => packet is not null;

    internal byte[] Packet => packet ?? [];

    /// <summary>Gets whether the document contains an XMP packet.</summary>
    public bool Exists => packet is not null;

    /// <summary>Returns a copy of the raw UTF-8 XMP packet, or <c>null</c> when absent.</summary>
    /// <returns>The packet bytes, or <c>null</c>.</returns>
    public byte[]? GetPacket() => packet is null ? null : (byte[])packet.Clone();

    /// <summary>Replaces the XMP packet after validating it as XML.</summary>
    /// <param name="value">The UTF-8 XMP packet.</param>
    /// <exception cref="InvalidDataException">The packet is not valid XML.</exception>
    public void SetPacket(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidatePacket(Parse(value));
        packet = (byte[])value.Clone();
        IsModified = true;
    }

    /// <summary>Gets a simple XMP property by XML namespace and local name.</summary>
    /// <param name="namespaceName">The property XML namespace URI.</param>
    /// <param name="name">The property local name.</param>
    /// <returns>The property value, or <c>null</c> when absent.</returns>
    /// <exception cref="InvalidDataException">The current packet is not valid XML.</exception>
    public string? GetProperty(string namespaceName, string name)
    {
        var propertyName = PropertyName(namespaceName, name);
        return packet is null ? null : Parse(packet).Descendants(propertyName).FirstOrDefault()?.Value;
    }

    /// <summary>Adds, replaces, or removes a simple XMP property.</summary>
    /// <param name="namespaceName">The property XML namespace URI.</param>
    /// <param name="name">The property local name.</param>
    /// <param name="value">The value, or <c>null</c> to remove the property.</param>
    /// <exception cref="InvalidDataException">The current packet is not valid XML.</exception>
    public void SetProperty(string namespaceName, string name, string? value)
    {
        var propertyName = PropertyName(namespaceName, name);
        var document = packet is null ? CreatePacket() : Parse(packet);
        ValidatePacket(document);
        var matches = document.Descendants(propertyName).ToList();
        if (value is null)
        {
            foreach (var match in matches)
            {
                match.Remove();
            }
        }
        else if (matches.Count > 0)
        {
            matches[0].Value = value;
            foreach (var duplicate in matches.Skip(1))
            {
                duplicate.Remove();
            }
        }
        else
        {
            var description = document.Descendants(XName.Get("Description", RdfNamespace)).First();
            description.Add(new XElement(propertyName, value));
        }

        packet = Serialize(document);
        IsModified = true;
    }

    /// <summary>Removes the XMP packet from the document.</summary>
    public void Clear()
    {
        packet = null;
        IsModified = true;
    }

    internal void LoadPacket(byte[] value) => packet = (byte[])value.Clone();

    private static XName PropertyName(string namespaceName, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespaceName);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return XName.Get(name, namespaceName);
    }

    private static XDocument CreatePacket()
    {
        XNamespace x = "adobe:ns:meta/";
        XNamespace rdf = RdfNamespace;
        return new XDocument(
            new XProcessingInstruction("xpacket", Write.XmpPacketFraming.BeginInstruction),
            new XText("\n"),
            new XElement(x + "xmpmeta",
                new XElement(rdf + "RDF",
                    new XElement(rdf + "Description", new XAttribute(rdf + "about", "")))),
            new XText("\n" + Write.XmpPacketFraming.Padding),
            new XProcessingInstruction("xpacket", Write.XmpPacketFraming.EndInstruction));
    }

    private static XDocument Parse(byte[] value)
    {
        try
        {
            using var input = new MemoryStream(value, writable: false);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });
            return XDocument.Load(reader, System.Xml.Linq.LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException("The XMP packet is not valid XML.", exception);
        }
    }

    private static void ValidatePacket(XDocument document)
    {
        XNamespace x = "adobe:ns:meta/";
        XNamespace rdf = RdfNamespace;
        if (document.Root?.Name != x + "xmpmeta"
            || !document.Descendants(rdf + "RDF").Any()
            || !document.Descendants(rdf + "Description").Any())
        {
            throw new InvalidDataException(
                "The XMP packet must contain x:xmpmeta, rdf:RDF, and rdf:Description elements in their standard namespaces.");
        }
    }

    private static byte[] Serialize(XDocument document)
    {
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = document.Declaration is null,
        }))
        {
            document.Save(writer);
        }

        return output.ToArray();
    }
}
