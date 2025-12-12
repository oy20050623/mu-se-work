using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactBookAPI.Models;

public class ContactDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 联系方式类型（电话/邮箱/社交媒体/地址）
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // 枚举更佳，此处简化为字符串

    /// <summary>
    /// 联系方式值
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 关联的联系人ID
    /// </summary>
    [ForeignKey("Contact")]
    public int ContactId { get; set; }

    /// <summary>
    /// 导航属性
    /// </summary>
    public Contact Contact { get; set; } = null!;
}