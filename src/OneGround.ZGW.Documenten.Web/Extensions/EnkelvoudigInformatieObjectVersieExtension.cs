using System;
using System.IO;
using System.Linq;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Extensions;

public static class EnkelvoudigInformatieObjectVersieExtension
{
    public static void SetLinkToNullWhenInvalid(this EnkelvoudigInformatieObjectVersie versie)
    {
        if (!string.IsNullOrEmpty(versie.Link) && !Uri.IsWellFormedUriString(versie.Link, UriKind.Absolute))
        {
            versie.Link = null;
        }
    }

    public static void EscapeBestandsNaamWhenInvalid(this EnkelvoudigInformatieObjectVersie versie)
    {
        if (versie.Bestandsnaam != null)
        {
            // Note: Under Linux these characters are correct when used in a filename but fails when we added these names in Ceph metadata
            char[] additionalInvalidChars = ['\r', '\n', '\t', '\b'];

            var escapedBestandsnaam = versie.Bestandsnaam;
            foreach (var invalidChar in Path.GetInvalidFileNameChars().Concat(additionalInvalidChars))
            {
                escapedBestandsnaam = escapedBestandsnaam.Replace(invalidChar, '_');
            }
            versie.Bestandsnaam = escapedBestandsnaam;
        }
    }
}
