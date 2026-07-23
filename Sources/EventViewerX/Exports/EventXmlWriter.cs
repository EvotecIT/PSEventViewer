using System;
using System.IO;
using System.Text;
using System.Xml;

namespace EventViewerX.Exports;

internal sealed class EventXmlWriter : IEventExportWriter {
    private readonly XmlWriter _writer;

    internal EventXmlWriter(Stream stream) {
        _writer = XmlWriter.Create(stream, new XmlWriterSettings {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });
        _writer.WriteStartDocument();
        _writer.WriteStartElement("Events");
    }

    public void Write(EventObject eventObject) {
        if (string.IsNullOrEmpty(eventObject.XMLData)) {
            throw new InvalidOperationException(
                "XML export requires StructuredData or Full read mode.");
        }
        _writer.WriteRaw(eventObject.XMLData);
    }

    public void Complete() {
        _writer.WriteEndElement();
        _writer.WriteEndDocument();
        _writer.Flush();
    }

    public void Dispose() {
        _writer.Dispose();
    }
}
