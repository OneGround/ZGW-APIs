# OneGround Gesprekken API (experimental)

## About

An implementation of the draft MijnGesprekken API: digital conversations (gesprekken) between a resident or business and a municipality. The draft standard is still being developed; this component follows the current draft and is not yet stable.

## Resources and endpoints

| Endpoint | Purpose |
|---|---|
| `GET /api/v1/gesprekken` | List gesprekken, newest first, with cursor pagination: `pageSize` (default 20, at most 100) and an opaque `cursor`; follow `next` until it is `null`. Filters: `referentie`, `aanleidingZaak`, `aanleidingPlan`, `aanvangsmomentGesprek__gte`, `aanvangsmomentGesprek__lte`, `gespreksdeelname`, `gespreksdeelname__type`, `gespreksdeelname__partij`, `gespreksdeelname__actor`. Each gesprek has `laatsteBijdrage`, so an overview needs no call per gesprek |
| `POST /api/v1/gesprekken` | Create a gesprek together with its ontvanger(s), the verzender and the first gespreksbijdrage, in one transaction |
| `GET /api/v1/gesprekken/{uuid}` | A gesprek with its gespreksdeelnames and gespreksbijdragen, oldest first |
| `POST /api/v1/gesprekken/{uuid}/eindigen` | End a gesprek |
| `POST /api/v1/gespreksbijdragen` | Add a gespreksbijdrage |
| `POST /api/v1/gespreksbijdragen/{uuid}/gelezen` | Register that a gespreksdeelname read a gespreksbijdrage (idempotent) |
| `POST /api/v1/gespreksbijdragen/{uuid}/redigeren` | Beheer: remove a gespreksbijdrage that should not have been there, also from the event history (scope `gesprekken.redigeren`, body `reden`) |
| `POST /api/v1/gespreksdeelnames` | Add a gespreksdeelname to an existing gesprek |
| `POST /api/v1/gespreksdeelnames/{uuid}/eindigen` | End a gespreksdeelname |
| `POST /api/v1/bestanden/upload-sessie` | Start an upload of a bijlage: returns a temporary `uploadUrl` and the `bestand` |
| `GET /api/v1/bestanden/{fileId}` | Status of a bestand and, once available, a temporary `downloadUrl` |
| `GET /api/v1/events` | Follow changes (Server-Sent Events) |

References between resources are URLs, as in the other ZGW APIs: `gesprek`, `verzender`, `isReactieOp`, `gespreksdeelname`, `bestand` and `partij` (a Partij in Klantinteracties), also in the filters. A deelname refers to a Partij or an Actor, with `naam` as its display name; other data about the person is in Klantinteracties. A gesprek has the optional aanleidingen `aanleidingZaak`, `aanleidingThema`, `aanleidingProductDienst`, `aanleidingAgendaAfspraak` and `aanleidingPlan`. `namens` on a deelname is the Partij someone acts for, for example as curator or bewindvoerder; it is for display only, rights are an authorization rule.

Business rules:

- A gesprek always has at least one deelnemer and one gespreksbijdrage, so they are created together.
- A gesprek cannot be started on a zaak that has an einddatum (checked through the Zaken API, unless `Application:IgnoreZaakValidation` is set).
- A gespreksbijdrage is at most 5000 characters.
- Gespreksbijdragen are never changed or deleted by a deelnemer; only beheer can redact one.
- An ended gesprek accepts no new bijdragen or deelnames; an ended deelname cannot send bijdragen.

Errors are `application/problem+json` (RFC 9457); the body is OneGround's error response, whose members match the
shared Fout schema of the specification.

## Automatic reply

With `Application:AutomatischAntwoordActor` set, every bijdrage of an inwoner gets an automatic reply: a bijdrage of that
system actor with `automatisch` set to `true`. `Application:AutomatischAntwoordTekst` holds the text (default "We reageren
binnen 2 werkdagen.") and `Application:AutomatischAntwoordNaam` the display name of the system actor.

## Closing a gesprek and the contactmoment

A gesprek is a slow chat, so its contactmoment has only taken place once it is closed. There is no klantcontact at the
start. When a gesprek is closed, by `eindigen` or automatically, the registration creates one klantcontact in
Klantinteracties and fills `contactmoment` (read-only, `null` while the gesprek runs). `gesprek.beeindigd` carries the
URL. A running gesprek can be found through the filters on partij and zaak.

- `Klantinteracties:Api` and `Klantinteracties:Token` configure the Klantinteracties API. Without them, or when it does
  not accept the klantcontact, the gesprek still closes and `contactmoment` stays `null`.
- `Application:AutomatischSluitenNaDagen` (default 3; 0 turns it off): a background job closes a gesprek after this many
  days without a new bijdrage, every `Application:AutomatischSluitenIntervalMinuten` (default 60). With several instances
  each runs the job; closing is not yet coordinated between them.

## Bijlagen

Bijlagen follow the two-step upload with a pre-signed URL:

1. `POST /bestanden/upload-sessie` with `bestandsnaam`, `bestandsomvang`, `contentType` and optionally `titel`.
2. `PUT` the file to `uploadUrl`, with the same `Content-Type`.
3. `GET /bestanden/{fileId}` until `status` is `beschikbaar`. The status is settled here: an object of another size than
   announced is `geweigerd` and removed; nothing uploaded in time is `verlopen`.
4. Refer to it with `"bijlagen": [{ "bestand": "<url>" }]`, also in `eersteBijdrage`. A bestand belongs to one bijdrage.

The registration copies `titel`, `bestandsnaam`, `contentType` and `bestandsomvang` to the bijlage, so a client can show
it without another call. Storage is S3-compatible: configure `BestandOpslag:Bucket`, `BestandOpslag:AccessKey`,
`BestandOpslag:SecretKey` and either `BestandOpslag:ServiceUrl` (for example Ceph) or `BestandOpslag:Region`.
`Application:BijlageMaxBytes` limits the size (default 50 MiB).

Bijlagen are not Enkelvoudige Informatieobjecten, so they are not part of the zaak file by themselves.

Bestanden that never became a bijlage are not cleaned up yet.

## Event stream

Every change is also written as a CloudEvent to an event log, in the same database transaction as the change itself
(an outbox). `GET /api/v1/events` serves that log as Server-Sent Events and is the only way to follow changes; there are
no ZGW notifications for this API.

- `id` is the position in the stream (also the CloudEvent `sequence`), `event` the CloudEvent `type`, `data` the
  CloudEvent with the resource after the change.
- Types: `nl.vng.gesprekken.gesprek.aangemaakt`, `.gesprek.beeindigd`, `.gespreksbijdrage.aangemaakt`,
  `.gespreksbijdrage.gelezen`, `.gespreksbijdrage.geredigeerd`, `.gespreksdeelname.aangemaakt`,
  `.gespreksdeelname.beeindigd`. `subject` is always the gesprek URL.
- Snapshot + follow: `GET /gesprekken` and `GET /gesprekken/{uuid}` return a `Feed-Cursor` header, determined before
  the snapshot is read. Follow the stream from it with `Last-Event-ID`; reconnect with the last `id` received.
- Optional filters: `gesprek` (repeatable URL) and `aanleidingZaak`. Without `Last-Event-ID` the stream starts at the
  current end. A cursor older than the oldest available event gives `410`; take a new snapshot.
- Appends are serialized with a transaction-scoped advisory lock, so events become visible in sequence order and a
  cursor never skips an event. A comment line `: ping` is sent when the stream is idle (default 30 s).
- A gesprek with a zaak has the extension attribute `aanleidingzaak` on its events, so a consumer can route an event to
  its zaak without looking up the gesprek.
- Redacting a gespreksbijdrage blanks the events about it (its creation and its leesregistraties) and marks them
  redacted. They keep their sequence, so cursors stay valid, but the stream skips them. Then
  `.gespreksbijdrage.geredigeerd` follows, with the URL and the redacted sequences (`Redactie`), so consumers can
  remove that content too. The reason is written to the log, since this component has no audit trail yet.

Writes stay on the REST endpoints.

## Authorization

Scopes `gesprekken.lezen`, `gesprekken.aanmaken` and `gesprekken.bijwerken` on component `grc`, and
`gesprekken.redigeren` for beheer. Authorization per gesprek (which deelnemer may read which gesprek) is not implemented
yet: lists, the stream and bestanden apply per organisation.

## Relation to the VNG API Lab

This component is a proof of concept of the MijnGesprekken draft in the
[VNG API Lab](https://github.com/VNG-Realisatie/VNG-API-Lab):

- [#47](https://github.com/VNG-Realisatie/VNG-API-Lab/pull/47): the specification, `apis/rest/gesprekken/next.yaml`.
- [#7](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/7): MijnGesprekken API.
- [#4](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/4) and [#5](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/5): the pagination and file upload patterns.
- [#51](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/51) and [#13](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/13): authorization, including authorization per gesprek.
- [#16](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/16) and [#53](https://github.com/VNG-Realisatie/VNG-API-Lab/pull/53): a query pattern; for now this API has only list filters.
- [#48](https://github.com/VNG-Realisatie/VNG-API-Lab/issues/48) and [#49](https://github.com/VNG-Realisatie/VNG-API-Lab/pull/49): atomic writes and JSON Commit; writes stay on REST here.

## Running locally

```bash
dotnet run --project OneGround.ZGW.Gesprekken.WebApi
```

It listens on port 5017 and uses database `grc_db` (see `localdev/`).
