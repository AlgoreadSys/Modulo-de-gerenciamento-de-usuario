using System.Runtime.InteropServices.JavaScript;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DotNet.Docker.Models;

[Table("TabelaUserTest")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }
    
    [Column("name")]
    public string? Name { get; set; }
    
    [Column("profileName")]
    public string? ProfileName { get; set; }
    
    [Column("birthData")]
    public DateTime? BirthData { get; set; }
}