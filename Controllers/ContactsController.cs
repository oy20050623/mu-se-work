using ContactBookAPI.Data;
using ContactBookAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Net.Mime;

namespace ContactBookAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContactsController : ControllerBase
{
    private readonly ContactBookDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ContactsController(ContactBookDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    #region 1.1 收藏联系人
    /// <summary>
    /// 标记/取消标记联系人收藏
    /// </summary>
    /// <param name="id">联系人ID</param>
    /// <param name="isBookmarked">是否收藏</param>
    [HttpPatch("{id}/bookmark")]
    public async Task<IActionResult> ToggleBookmark(int id, [FromBody] bool isBookmarked)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound("联系人不存在");

        contact.IsBookmarked = isBookmarked;
        await _context.SaveChangesAsync();

        return Ok(new { Message = isBookmarked ? "收藏成功" : "取消收藏成功", contact });
    }

    /// <summary>
    /// 获取所有收藏的联系人
    /// </summary>
    [HttpGet("bookmarked")]
    public async Task<ActionResult<IEnumerable<Contact>>> GetBookmarkedContacts()
    {
        var contacts = await _context.Contacts
            .Include(c => c.ContactDetails)
            .Where(c => c.IsBookmarked)
            .ToListAsync();

        return Ok(contacts);
    }
    #endregion

    #region 1.2 添加多个联系方式
    /// <summary>
    /// 为联系人添加单个联系方式
    /// </summary>
    [HttpPost("{contactId}/details")]
    public async Task<IActionResult> AddContactDetail(int contactId, [FromBody] ContactDetail detail)
    {
        if (!await _context.Contacts.AnyAsync(c => c.Id == contactId))
            return NotFound("联系人不存在");

        detail.ContactId = contactId;
        _context.ContactDetails.Add(detail);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContactById), new { id = contactId }, detail);
    }

    /// <summary>
    /// 为联系人批量添加联系方式
    /// </summary>
    [HttpPost("{contactId}/details/batch")]
    public async Task<IActionResult> AddContactDetailsBatch(int contactId, [FromBody] List<ContactDetail> details)
    {
        if (!await _context.Contacts.AnyAsync(c => c.Id == contactId))
            return NotFound("联系人不存在");

        foreach (var detail in details)
        {
            detail.ContactId = contactId;
        }

        _context.ContactDetails.AddRange(details);
        await _context.SaveChangesAsync();

        return Ok(new { Count = details.Count, Message = "批量添加成功" });
    }
    #endregion

    #region 1.3 导入/导出Excel
    /// <summary>
    /// 导出所有联系人到Excel
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel()
    {
        // 获取所有联系人（包含联系方式）
        var contacts = await _context.Contacts
            .Include(c => c.ContactDetails)
            .ToListAsync();

        // 设置EPPlus许可证（非商业使用）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("联系人列表");

            // 表头
            worksheet.Cells["A1"].Value = "ID";
            worksheet.Cells["B1"].Value = "姓名";
            worksheet.Cells["C1"].Value = "是否收藏";
            worksheet.Cells["D1"].Value = "联系方式类型";
            worksheet.Cells["E1"].Value = "联系方式值";

            // 填充数据
            int row = 2;
            foreach (var contact in contacts)
            {
                if (contact.ContactDetails.Any())
                {
                    // 有联系方式的联系人
                    foreach (var detail in contact.ContactDetails)
                    {
                        worksheet.Cells[$"A{row}"].Value = contact.Id;
                        worksheet.Cells[$"B{row}"].Value = contact.Name;
                        worksheet.Cells[$"C{row}"].Value = contact.IsBookmarked ? "是" : "否";
                        worksheet.Cells[$"D{row}"].Value = detail.Type;
                        worksheet.Cells[$"E{row}"].Value = detail.Value;
                        row++;
                    }
                }
                else
                {
                    // 无联系方式的联系人
                    worksheet.Cells[$"A{row}"].Value = contact.Id;
                    worksheet.Cells[$"B{row}"].Value = contact.Name;
                    worksheet.Cells[$"C{row}"].Value = contact.IsBookmarked ? "是" : "否";
                    worksheet.Cells[$"D{row}"].Value = "-";
                    worksheet.Cells[$"E{row}"].Value = "-";
                    row++;
                }
            }

            // 自动列宽
            worksheet.Cells.AutoFitColumns();

            // 生成Excel文件流
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // 返回文件
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "联系人列表.xlsx");
        }
    }

    /// <summary>
    /// 从Excel导入联系人
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("请上传有效的Excel文件");

        // 验证文件类型
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("仅支持.xlsx和.xls格式的Excel文件");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.First();
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                // 从第2行开始读取（跳过表头）
                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[$"B{row}"].Text.Trim();
                    if (string.IsNullOrEmpty(name)) continue; // 跳过空姓名行

                    var isBookmarked = worksheet.Cells[$"C{row}"].Text.Trim() == "是";
                    var detailType = worksheet.Cells[$"D{row}"].Text.Trim();
                    var detailValue = worksheet.Cells[$"E{row}"].Text.Trim();

                    // 查找或创建联系人
                    var contact = await _context.Contacts
                        .FirstOrDefaultAsync(c => c.Name == name);

                    if (contact == null)
                    {
                        contact = new Contact
                        {
                            Name = name,
                            IsBookmarked = isBookmarked
                        };
                        _context.Contacts.Add(contact);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // 更新收藏状态
                        contact.IsBookmarked = isBookmarked;
                        _context.Contacts.Update(contact);
                    }

                    // 添加联系方式（非空时）
                    if (!string.IsNullOrEmpty(detailType) && detailType != "-" && !string.IsNullOrEmpty(detailValue))
                    {
                        var existingDetail = await _context.ContactDetails
                            .FirstOrDefaultAsync(cd => cd.ContactId == contact.Id && cd.Type == detailType && cd.Value == detailValue);

                        if (existingDetail == null)
                        {
                            _context.ContactDetails.Add(new ContactDetail
                            {
                                ContactId = contact.Id,
                                Type = detailType,
                                Value = detailValue
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { Message = "导入成功" });
    }
    #endregion

    #region 基础CRUD（辅助功能）
    /// <summary>
    /// 创建新联系人
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Contact>> CreateContact(Contact contact)
    {
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContactById), new { id = contact.Id }, contact);
    }

    /// <summary>
    /// 根据ID获取联系人（包含联系方式）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContactById(int id)
    {
        var contact = await _context.Contacts
            .Include(c => c.ContactDetails)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null) return NotFound();

        return Ok(contact);
    }

    /// <summary>
    /// 获取所有联系人
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contact>>> GetAllContacts()
    {
        return await _context.Contacts
            .Include(c => c.ContactDetails)
            .ToListAsync();
    }
    #endregion
}