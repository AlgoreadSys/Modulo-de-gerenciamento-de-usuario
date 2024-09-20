using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DotNet.Docker.Models;

[Table("reports")]
public class Report : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }
    
    [Column("reported_user_id")]
    public long ReportedUserId { get; set; }  // Usuário denunciado
    
    [Column("reporting_user_id")]
    public long ReportingUserId { get; set; } // Usuário que fez a denúncia
    
    [Column("reason")]
    public string? Reason { get; set; } // Motivo da denúncia
}
