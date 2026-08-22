using System.ComponentModel.DataAnnotations;

namespace KoiFengShuiSystem.DataAccess.Models;

public class Follow
{
    [Key]
    public int FollowId { get; set; }

    public int PostId { get; set; }

    public int AccountId { get; set; }

    public virtual Post Post { get; set; } = null!;
}
