using System;
using System.IO;
using System.Text.Json;

namespace EventViewerX.Exports;

internal sealed class EventJsonLinesWriter : IEventExportWriter {
    private static readonly byte[] NewLine = { (byte)'\n' };
    private readonly Stream _stream;
    private readonly Utf8JsonWriter _writer;

    internal EventJsonLinesWriter(Stream stream) {
        _stream = stream;
        _writer = new Utf8JsonWriter(stream, new JsonWriterOptions {
            Indented = false,
            SkipValidation = false
        });
    }

    public void Write(EventObject eventObject) {
        _writer.WriteStartObject();
        _writer.WriteString("timeCreated", eventObject.TimeCreated);
        if (eventObject.RecordId.HasValue) _writer.WriteNumber("recordId", eventObject.RecordId.Value);
        _writer.WriteNumber("id", eventObject.Id);
        _writer.WriteString("providerName", eventObject.ProviderName);
        _writer.WriteString("machineName", eventObject.MachineName);
        _writer.WriteString("logName", eventObject.LogName);
        if (eventObject.Level.HasValue) _writer.WriteNumber("level", eventObject.Level.Value);
        _writer.WriteString("levelDisplayName", eventObject.LevelDisplayName);
        if (eventObject.Task.HasValue) _writer.WriteNumber("task", eventObject.Task.Value);
        _writer.WriteString("taskDisplayName", eventObject.TaskDisplayName);
        if (eventObject.Opcode.HasValue) _writer.WriteNumber("opcode", eventObject.Opcode.Value);
        _writer.WriteString("opcodeDisplayName", eventObject.OpcodeDisplayName);
        if (eventObject.Keywords.HasValue) _writer.WriteNumber("keywords", eventObject.Keywords.Value);
        _writer.WritePropertyName("keywordDisplayNames");
        JsonSerializer.Serialize(_writer, eventObject.KeywordsDisplayNames);
        if (eventObject.ProcessId.HasValue) _writer.WriteNumber("processId", eventObject.ProcessId.Value);
        if (eventObject.ThreadId.HasValue) _writer.WriteNumber("threadId", eventObject.ThreadId.Value);
        if (eventObject.UserId != null) _writer.WriteString("userId", eventObject.UserId.Value);
        _writer.WriteString("messageCulture", eventObject.MessageCulture);
        _writer.WriteString("messageRenderStatus", eventObject.MessageRenderStatus.ToString());
        _writer.WriteNumber("messageRenderErrorCode", eventObject.MessageRenderErrorCode);
        _writer.WriteString("message", eventObject.Message);
        _writer.WritePropertyName("properties");
        _writer.WriteStartArray();
        foreach (EventPropertyValue property in eventObject.Properties) {
            if (property.Value == null) {
                _writer.WriteNullValue();
            } else {
                JsonSerializer.Serialize(_writer, property.Value, property.Value.GetType());
            }
        }
        _writer.WriteEndArray();
        _writer.WritePropertyName("data");
        JsonSerializer.Serialize(_writer, eventObject.Data);
        _writer.WritePropertyName("attachments");
        _writer.WriteStartArray();
        foreach (byte[] attachment in eventObject.Attachments) {
            _writer.WriteBase64StringValue(attachment);
        }
        _writer.WriteEndArray();
        _writer.WriteString("xml", eventObject.XMLData);
        _writer.WriteEndObject();
        _writer.Flush();
        _stream.Write(NewLine, 0, NewLine.Length);
        _writer.Reset(_stream);
    }

    public void Complete() {
        _writer.Flush();
        _stream.Flush();
    }

    public void Dispose() {
        _writer.Dispose();
    }
}
