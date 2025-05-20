using OneGround.ZGW.Documenten.Services;

namespace OneGround.ZGW.Documenten.Web.Extensions;

public static class DocumentUrnExtension
{
    public static bool IsAnyDocumentUrn(this string urn)
    {
        if (urn == null)
            return false;

        // Check whichever it is a document urn (filesystem, eventually mongodb, ceph, etc)
        //  E.g. urn:dms:fs:202011:fb5da9c0-4ee7-4087-9f17-9ed3ed45a52c or urn:dms:ceph:202011:fb5da9c0-4ee7-4087-9f17-9ed3ed45a52c

        var result = urn.StartsWith($"{DocumentUrn.UrnPrefix}:{DocumentUrn.UrnDocumentPrefix}");

        return result;
    }
}
