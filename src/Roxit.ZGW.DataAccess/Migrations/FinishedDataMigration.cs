using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roxit.ZGW.DataAccess.Migrations;

[Table("finished_data_migrations")]
public class FinishedDataMigration
{
    public FinishedDataMigration(string name)
    {
        Name = name;
        Executed = DateTime.UtcNow;
        ApplicationVersion = $"{GetType().Assembly.GetName().Version}";
    }

    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("executed")]
    public DateTime Executed { get; set; }

    [Column("application_version")]
    public string ApplicationVersion { get; set; }
}
