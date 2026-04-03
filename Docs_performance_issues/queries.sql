--Current queries
SELECT count(*)::int
FROM enkelvoudiginformatieobjecten AS e
INNER JOIN "TempInformatieObjectAuthorization" AS t ON e.informatieobjecttype = t."InformatieObjectType"
LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
WHERE e.owner = 'todo' AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding"

SELECT e.id, e.catalogus_id, e.createdby, e.creationtime, e.indicatiegebruiksrecht, e.informatieobjecttype, e.latest_enkelvoudiginformatieobjectversie_id, e.lock, e.locked, e.modificationtime, e.modifiedby, e.owner, e.xmin, e0.id, e0.auteur, e0.beginregistratie, e0.beschrijving, e0.bestandsnaam, e0.bestandsomvang, e0.bronorganisatie, e0.createdby, e0.creatiedatum, e0.creationtime, e0.enkelvoudiginformatieobject_id, e0.formaat, e0.identificatie, e0.inhoud, e0.inhoud_is_vervallen, e0.integriteit_algoritme, e0.integriteit_datum, e0.integriteit_waarde, e0.link, e0.modificationtime, e0.modifiedby, e0.multipartdocument_id, e0.ondertekening_datum, e0.ondertekening_soort, e0.ontvangstdatum, e0.owner, e0.xmin, e0.status, e0.taal, e0.titel, e0.trefwoorden, e0.verschijningsvorm, e0.versie, e0.vertrouwelijkheidaanduiding, e0.verzenddatum, t."InformatieObjectType"
FROM enkelvoudiginformatieobjecten AS e
INNER JOIN "TempInformatieObjectAuthorization" AS t ON e.informatieobjecttype = t."InformatieObjectType"
LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
WHERE e.owner = 'todo' AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding"
ORDER BY e.id, t."InformatieObjectType", e0.id
LIMIT 100 OFFSET 0

SELECT b.id, b.enkelvoudiginformatieobjectversie_id, b.omvang, b.uploadpart_id, b.volgnummer, b.voltooid, t0.id, t0."InformatieObjectType", t0.id0
FROM (
    SELECT e.id, t."InformatieObjectType", e0.id AS id0
    FROM enkelvoudiginformatieobjecten AS e
    INNER JOIN "TempInformatieObjectAuthorization" AS t ON e.informatieobjecttype = t."InformatieObjectType"
    LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
    WHERE e.owner = 'todo' AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding"
    ORDER BY e.id
    LIMIT 100 OFFSET 0
) AS t0
INNER JOIN bestandsdelen AS b ON t0.id0 = b.enkelvoudiginformatieobjectversie_id
ORDER BY t0.id, t0."InformatieObjectType", t0.id0


--New optimized queries
SELECT count(*)::int
FROM enkelvoudiginformatieobjecten AS e
LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
WHERE e.owner = 'todo' AND EXISTS (
    SELECT 1
    FROM "TempInformatieObjectAuthorization" AS t
    WHERE t."InformatieObjectType" = e.informatieobjecttype AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")

SELECT e.id, e.catalogus_id, e.createdby, e.creationtime, e.indicatiegebruiksrecht, e.informatieobjecttype, e.latest_enkelvoudiginformatieobjectversie_id, e.lock, e.locked, e.modificationtime, e.modifiedby, e.owner, e.xmin, e0.id, e0.auteur, e0.beginregistratie, e0.beschrijving, e0.bestandsnaam, e0.bestandsomvang, e0.bronorganisatie, e0.createdby, e0.creatiedatum, e0.creationtime, e0.enkelvoudiginformatieobject_id, e0.formaat, e0.identificatie, e0.inhoud, e0.inhoud_is_vervallen, e0.integriteit_algoritme, e0.integriteit_datum, e0.integriteit_waarde, e0.link, e0.modificationtime, e0.modifiedby, e0.multipartdocument_id, e0.ondertekening_datum, e0.ondertekening_soort, e0.ontvangstdatum, e0.owner, e0.xmin, e0.status, e0.taal, e0.titel, e0.trefwoorden, e0.verschijningsvorm, e0.versie, e0.vertrouwelijkheidaanduiding, e0.verzenddatum
FROM enkelvoudiginformatieobjecten AS e
LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
WHERE e.owner = 'todo' AND EXISTS (
    SELECT 1
    FROM "TempInformatieObjectAuthorization" AS t
    WHERE t."InformatieObjectType" = e.informatieobjecttype AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")
ORDER BY e.id, e0.id
LIMIT 100 OFFSET 0

SELECT b.id, b.enkelvoudiginformatieobjectversie_id, b.omvang, b.uploadpart_id, b.volgnummer, b.voltooid, t0.id, t0.id0
FROM (
    SELECT e.id, e0.id AS id0
    FROM enkelvoudiginformatieobjecten AS e
    LEFT JOIN enkelvoudiginformatieobjectversies AS e0 ON e.latest_enkelvoudiginformatieobjectversie_id = e0.id
    WHERE e.owner = 'todo' AND EXISTS (
        SELECT 1
        FROM "TempInformatieObjectAuthorization" AS t
        WHERE t."InformatieObjectType" = e.informatieobjecttype AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")
    ORDER BY e.id
    LIMIT 100 OFFSET 0
) AS t0
INNER JOIN bestandsdelen AS b ON t0.id0 = b.enkelvoudiginformatieobjectversie_id
ORDER BY t0.id, t0.id0