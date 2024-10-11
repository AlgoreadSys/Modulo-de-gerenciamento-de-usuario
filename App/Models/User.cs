using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DotNet.Docker.Models;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }
    
    [Column("name")]
    public string? Name { get; set; }
    
    [Column("profileName")]
    public string? ProfileName { get; set; }
    
    [Column("birthDate")]
    public DateTime? BirthDate { get; set; }
    
    [Column("auth_user_id")]
    public Guid Auth_user_id { get; set; }
    
    [Column("following_list")]
    public List<Guid>? FollowingList { get; set; } = [];
}