using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly ContactContext _context;

    public ContactsController(ContactContext context)
    {
        _context = context;
    }

    // 获取所有联系人（含联系方式）
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contact>>> GetContacts()
    {
        return await _context.Contacts
            .Include(c => c.ContactDetails)
            .ToListAsync();
    }

    // 根据ID获取联系人
    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContact(int id)
    {
        var contact = await _context.Contacts
            .Include(c => c.ContactDetails)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null)
        {
            return NotFound();
        }

        return contact;
    }

    // 添加新联系人
    [HttpPost]
    public async Task<ActionResult<Contact>> PostContact(Contact contact)
    {
        if (string.IsNullOrEmpty(contact.Name))
        {
            return BadRequest("联系人姓名不能为空");
        }

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
    }

    // 收藏/取消收藏联系人
    [HttpPatch("{id}/bookmark")]
    public async Task<IActionResult> ToggleBookmark(int id, [FromBody] BookmarkRequest request)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
        {
            return NotFound();
        }

        contact.IsBookmarked = request.IsBookmarked;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // 添加联系方式
    [HttpPost("{id}/details")]
    public async Task<ActionResult<ContactDetail>> AddContactDetail(int id, ContactDetail detail)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
        {
            return NotFound();
        }

        detail.ContactId = id;
        _context.ContactDetails.Add(detail);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContact), new { id = id }, detail);
    }

    // 导出Excel
    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel()
    {
        var contacts = await _context.Contacts
            .Include(c => c.ContactDetails)
            .ToListAsync();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("通讯录");

            // 设置表头
            worksheet.Cells["A1"].Value = "ID";
            worksheet.Cells["B1"].Value = "姓名";
            worksheet.Cells["C1"].Value = "是否收藏";
            worksheet.Cells["D1"].Value = "联系方式类型";
            worksheet.Cells["E1"].Value = "联系方式值";

            // 填充数据
            int row = 2;
            foreach (var contact in contacts)
            {
                if (contact.ContactDetails.Count == 0)
                {
                    worksheet.Cells[$"A{row}"].Value = contact.Id;
                    worksheet.Cells[$"B{row}"].Value = contact.Name;
                    worksheet.Cells[$"C{row}"].Value = contact.IsBookmarked ? "是" : "否";
                    worksheet.Cells[$"D{row}"].Value = "-";
                    worksheet.Cells[$"E{row}"].Value = "-";
                    row++;
                }
                else
                {
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
            }

            // 自动调整列宽
            worksheet.Cells["A1:E1"].AutoFitColumns();

            // 生成文件流
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // 返回文件
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "通讯录列表.xlsx");
        }
    }

    // 导入Excel（带事务）
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("请上传有效的Excel文件");

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("仅支持.xlsx和.xls格式的Excel文件");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.First();
                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        int successCount = 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var name = worksheet.Cells[$"B{row}"].Text.Trim();
                                if (string.IsNullOrEmpty(name)) continue;

                                var isBookmarked = worksheet.Cells[$"C{row}"].Text.Trim() == "是";
                                var detailType = worksheet.Cells[$"D{row}"].Text.Trim();
                                var detailValue = worksheet.Cells[$"E{row}"].Text.Trim();

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
                                    successCount++;
                                }
                                else
                                {
                                    contact.IsBookmarked = isBookmarked;
                                    _context.Contacts.Update(contact);
                                }

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
                                        successCount++;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"处理行 {row} 时出错: {ex.Message}");
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Ok(new { Message = $"导入成功，处理 {successCount} 条数据", TotalRows = rowCount - 1 });
                    }
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"导入失败: {ex.Message}");
            }
        }
    }

    // 收藏请求模型
    public class BookmarkRequest
    {
        public bool IsBookmarked { get; set; }
    }
}