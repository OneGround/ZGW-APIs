using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneGround.ZGW.DataAccess;

public class OwnedEntity
{
    [Required]
    [StringLength(9, MinimumLength = 9)]
    [Column("owner")]
    public string Owner { get; set; }
}
