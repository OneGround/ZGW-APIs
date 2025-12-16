using System.Collections.Generic;
using System.IO;
using FluentValidation;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.MimeTypes;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Extensions;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._1;

public class EnkelvoudigInformatieObjectRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectBaseRequestDto>
{
    public EnkelvoudigInformatieObjectRequestValidator()
    {
        CascadeRuleFor(r => r.Identificatie).MaximumLength(40);
        CascadeRuleFor(r => r.Bronorganisatie).IsRsin(required: true);
        CascadeRuleFor(r => r.CreatieDatum).IsDate(true);
        CascadeRuleFor(r => r.Titel).NotNull().NotEmpty().MaximumLength(200);
        CascadeRuleFor(r => r.Vertrouwelijkheidaanduiding).IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(r => r.Auteur).NotNull().NotEmpty().MaximumLength(200);
        CascadeRuleFor(r => r.Status).IsEnumName(typeof(Status)).When(r => !string.IsNullOrEmpty(r.Status));
        CascadeRuleFor(r => r.Formaat).IsValidMimeType(v => MimeTypeHelper.IsValidMimeType(v), maxLength: 255, allowEmpty: true);
        CascadeRuleFor(r => r.Taal).IsIso639LanguageCode(required: true, new Dictionary<string, string> { { "nl", "nld" }, { "en", "eng" } });
        CascadeRuleFor(r => r.Bestandsnaam).MaximumLength(255);

        CascadeRuleFor(r => r.Inhoud)
            .NotNull()
            .NotEmpty()
            .Must(_ => false) // Fails always when there is an error file!
            .WithMessage("Incorrect base64 data is specified.")
            .WithErrorCode(ErrorCode.IncorrectBase64Padding)
            .When(r => r.Inhoud != null && File.Exists(r.Inhoud + ".error")); // Note: this dummy error-file is created by Middleware when base64 decoding has failed!

        CascadeRuleFor(r => r.Inhoud)
            .NotNull()
            .NotEmpty()
            .WithMessage("No base64 data is specified.")
            .WithErrorCode(ErrorCode.Required)
            .Must(inhoud => inhoud.IsAnyDocumentUrn() || File.Exists(inhoud)) // Note: inhoud is intercepted by Middleware which writes a file with base64 encoded data
            .When(r => !string.IsNullOrEmpty(r.Inhoud)); // For base64-document (so not for multi-part or document-meta-only documents)

        CascadeRuleFor(r => r.Link).MaximumLength(200);
        CascadeRuleFor(r => r.Beschrijving).MaximumLength(1000);
        CascadeRuleFor(r => r.OntvangstDatum).IsDate(false);
        CascadeRuleFor(r => r.VerzendDatum).IsDate(false);
        CascadeRuleFor(r => r.Ondertekening)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(r => r.Soort).NotNull().NotEmpty().IsEnumName(typeof(Soort));
                v.CascadeRuleFor(r => r.Datum).IsDate(true);
            });
        CascadeRuleFor(r => r.Integriteit)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(r => r.Algoritme).NotNull().NotEmpty().IsEnumName(typeof(Algoritme));
                v.CascadeRuleFor(r => r.Waarde).NotNull().NotEmpty().MaximumLength(128);
                v.CascadeRuleFor(r => r.Datum).IsDate(true);
            });
        CascadeRuleFor(r => r.InformatieObjectType).NotNull().NotEmpty().IsUri().MaximumLength(200);
    }
}
