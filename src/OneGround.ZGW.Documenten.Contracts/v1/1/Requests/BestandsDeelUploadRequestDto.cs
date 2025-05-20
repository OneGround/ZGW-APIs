using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OneGround.ZGW.Documenten.Contracts.v1._1.Requests;

public class BestandsDeelUploadRequestDto
{
    [FromForm(Name = "lock")]
    public string Lock { get; set; }

    [FromForm(Name = "inhoud")]
    public IFormFile Inhoud { get; set; }
}
