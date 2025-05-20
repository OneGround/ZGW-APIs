using System;

namespace OneGround.ZGW.Documenten.Services;

public class DocumentUrn
{
    public const string UrnPrefix = "urn";
    public const string UrnDocumentPrefix = "dms";

    public DocumentUrn(string urn)
    {
        if (!TryParse(urn))
            throw new InvalidOperationException(
                "Incorrect urn for documents. Should be format: urn:dms:<type>:<name>:<uuid> or urn:dms:<type>:<uuid>."
            );
    }

    public DocumentUrn(string type, string name, string objectId)
    {
        Root = UrnDocumentPrefix;
        Type = type;
        Name = name;
        ObjectId = objectId;
    }

    public string Root { get; internal set; }
    public string Type { get; internal set; }
    public string Name { get; internal set; }
    public string ObjectId { get; internal set; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? $"{UrnPrefix}:{Root}:{Type}:{ObjectId}" : $"{UrnPrefix}:{Root}:{Type}:{Name}:{ObjectId}";
    }

    private bool TryParse(string urn)
    {
        var parts = urn.Split(':');

        if (parts.Length < 4 || parts.Length > 5)
            return false;

        Root = parts[1];
        Type = parts[2];

        if (parts.Length == 5)
        {
            Name = parts[3];
            ObjectId = parts[4];
        }
        else
        {
            ObjectId = parts[3];
        }
        return true;
    }
}
