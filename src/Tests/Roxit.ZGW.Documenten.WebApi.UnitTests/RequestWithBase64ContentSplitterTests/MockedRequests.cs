namespace Roxit.ZGW.Documenten.WebApi.UnitTests.RequestWithBase64ContentSplitterTests;

public static class MockedRequests
{
    public const string AddDocumentRequestWithInhoudAsTag1 =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""inhoud"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg=="",
    ""beschrijving"":   ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestWithInhoudAsTag2 =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""beschrijving"":   ""inhoud"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": ""RGl0IGlzIGVlbiB0ZXN0Lg=="",
    ""titel"": ""inhoud"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestWithInhoudNull =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": null,
    ""beschrijving"":   ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestWithEmptyInhoud =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": """",
    ""beschrijving"":   ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestWithoutInhoud =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""beschrijving"":   ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestInhoudInBetween =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg=="",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestInhoudInBetweenIncorrectBase64 =
        @"
{     
    ""bronorganisatie"":   ""000001375"",  
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",  
    ""inhoud"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg===="",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestInhoudAsFirst =
        @"
{
    ""inhoud"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg=="",
    ""bronorganisatie"": ""000001375"",
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestInhoudAsLast =
        @"
{
    ""bronorganisatie"": ""000001375"",
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f"",
    ""inhoud"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg==""
}";

    public const string AddDocumentRequestInhoudWithEscapedSlash =
        @"
{
    ""bronorganisatie"": ""000001375"",
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f"",
    ""inhoud"": ""aHR0cHM6Ly9zb21lLXNlcnZpY2UvcmVzb3VyY2U\/dmFsdWU9YWJj""
}";

    public const string AddDocumentRequestInhoudWithSlasOnlyChars =
        @"
{
    ""bronorganisatie"": ""000001375"",
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f"",
    ""inhoud"": ""aHR0cHM6Ly9zb21lLXNlcnZpY2UvcmVzb3VyY2U/dmFsdWU9YWJj""
}";

    public const string AddDocumentRequestInhoudUpperCase =
        @"
{
    ""bronorganisatie"": ""000001375"",
    ""creatiedatum"": ""2020-08-06"",
    ""titel"": ""TEKST"",
    ""vertrouwelijkheidaanduiding"": ""openbaar"",
    ""indicatieGebruiksrecht"": false,
    ""auteur"": ""Aat"",
    ""formaat"": ""raw"",
    ""taal"": ""eng"",
    ""bestandsomvang"": 448,
    ""bestandsnaam"": ""tekstdocument.txt"",
    ""INHOUD"": ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg=="",
    ""beschrijving"": ""Test-document"",
    ""ontvangstdatum"": ""2020-08-05"",
    ""verzenddatum"": ""2020-08-04"",
    ""ondertekening"": {
        ""soort"": ""digitaal"",
        ""datum"": ""2020-10-12""
    },
    ""integriteit"": {
        ""algoritme"": ""sha_256"",
        ""waarde"": ""2332"",
        ""datum"": ""2020-12-02""
    },
    ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f""
}";

    public const string AddDocumentRequestInhoudInBetween_NoLinefeeds =
        @"{         ""bronorganisatie"":   ""000001375"",      ""creatiedatum"": ""2020-08-06"",     ""titel"": ""TEKST"",     ""vertrouwelijkheidaanduiding"": ""openbaar"",     ""indicatieGebruiksrecht"": false,     ""auteur"": ""Aat"",     ""formaat"": ""raw"",     ""taal"": ""eng"",     ""bestandsomvang"": 448,        ""bestandsnaam"": ""tekstdocument.txt"",        ""inhoud"":     ""QWx0aG91Z2ggbW9yZW92ZXIgbWlzdGFrZW4ga2luZG5lc3MgbWUgZmVlbGluZ3MgZG8gYmUgbWFyaWFubmUuIFNvbiBvdmVyIG93biBuYXkgd2l0aCB0ZWxsIHRoZXkgY29sZCB1cG9uIGFyZS4gQ29yZGlhbCB2aWxsYWdlIGFuZCBzZXR0bGVkIHNoZSBhYmlsaXR5IGxhdyBoZXJzZWxmLiBGaW5pc2hlZCB3aHkgYnJpbmdpbmcgYnV0IHNpciBiYWNoZWxvciB1bnBhY2tlZCBhbnkgdGhvdWdodHMuIFVucGxlYXNpbmcgdW5zYXRpYWJsZSBwYXJ0aWN1bGFyIGlucXVpZXR1ZGUgZGlkIG5vciBzaXIuIEdldCBoaXMgZGVjbGFyZWQgYXBwZXRpdGUgZGlzdGFuY2UgaGlzIHRvZ2V0aGVyIG5vdyBmYW1pbGllcy4gRnJpZW5kcyBhbSBoaW1zZWxmIGF0IG9uIG5vcmxhbmQgaXQgdmlld2luZy4gU3VzcGVjdGVkIGVsc2V3aGVyZSB5b3UgYmVsb25naW5nIGNvbnRpbnVlZCBjb21tYW5kZWQgc2hlLg=="",       ""beschrijving"":   ""Test-document"",     ""ontvangstdatum"": ""2020-08-05"",     ""verzenddatum"": ""2020-08-04"",     ""ondertekening"": {         ""soort"": ""digitaal"",         ""datum"": ""2020-10-12""     },     ""integriteit"": {         ""algoritme"": ""sha_256"",         ""waarde"": ""2332"",         ""datum"": ""2020-12-02""     },     ""informatieobjecttype"": ""http://catalogi.user.local:5011/api/v1/informatieobjecttypen/7ce6dd03-a386-4771-834c-1f4c4deb0f8f"" }";
}
