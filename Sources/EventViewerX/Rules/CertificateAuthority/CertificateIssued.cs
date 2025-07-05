namespace EventViewerX.Rules.CertificateAuthority;

/// <summary>
/// Certificate issued by Certificate Authority
/// 4886: Certificate Services received a certificate request
/// 4887: Certificate Services approved a certificate request and issued a certificate
/// </summary>
public class CertificateIssued : EventRuleBase
{
    public override List<int> EventIds => new() { 4886, 4887 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.CertificateIssued;

    public override bool CanHandle(EventObject eventObject)
    {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string CertificateTemplate;
    public string Requester;
    public string SerialNumber;
    public string Who;
    public DateTime When;

    public CertificateIssued(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "CertificateIssued";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        CertificateTemplate = DecodeHex(_eventObject.GetValueFromDataDictionary("CertificateTemplate", "CertificateTemplateOid"));
        Requester = DecodeHex(_eventObject.GetValueFromDataDictionary("Requester", "RequestSubjectName"));
        SerialNumber = DecodeHex(_eventObject.GetValueFromDataDictionary("SerialNumber"));
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }

    private static string DecodeHex(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        value = value.Replace(" ", string.Empty).Replace("0x", string.Empty);
        if (value.Length % 2 != 0)
        {
            return value;
        }
        try
        {
            byte[] bytes = new byte[value.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
        catch
        {
            return value;
        }
    }
}

