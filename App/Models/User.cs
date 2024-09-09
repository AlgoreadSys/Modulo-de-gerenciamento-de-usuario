using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DotNet.Docker.Properties.Model;

[Table("TabelaUserTest")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }
    [Column("Name")]
    public string? Name { get; set; }
    [Column("Email")]
    public string? Email { get; set; }
}