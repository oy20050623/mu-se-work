using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactBookAPI.Models;

public class Contact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否收藏（书签）
    /// </summary>
    public bool IsBookmarked { get; set; } = false;

    /// <summary>
    /// 联系人的多个联系方式
    /// </summary>
    public List<ContactDetail> ContactDetails { get; set; } = new();
}
