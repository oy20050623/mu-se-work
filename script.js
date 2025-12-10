// 前端临时存储联系人数据（刷新页面会清空）
let contacts = [];

// 1. 添加联系人
function addContact() {
    const nameInput = document.getElementById("nameInput");
    const name = nameInput.value.trim();
    if (!name) {
        alert("请输入联系人姓名！");
        return;
    }
    // 检查是否重名
    const isDuplicate = contacts.some(item => item.name === name);
    if (isDuplicate) {
        alert("该联系人已存在！");
        nameInput.value = "";
        return;
    }
    // 添加新联系人
    contacts.push({
        name: name,
        phones: [],
        emails: [],
        socials: [],
        address: "",
        isStarred: false
    });
    alert(`联系人“${name}”添加成功！`);
    nameInput.value = "";
}

// 2. 添加联系方式
function addContactInfo() {
    const targetName = document.getElementById("targetName").value.trim();
    const infoType = document.getElementById("infoType").value;
    const infoContent = document.getElementById("infoContent").value.trim();
    
    // 校验输入
    if (!targetName || !infoContent) {
        alert("请填写完整联系人姓名和联系方式！");
        return;
    }
    // 找到目标联系人
    const contact = contacts.find(item => item.name === targetName);
    if (!contact) {
        alert("未找到该联系人，请先添加！");
        return;
    }
    // 根据类型补充信息
    switch (infoType) {
        case "phone":
            contact.phones.push(infoContent);
            break;
        case "email":
            contact.emails.push(infoContent);
            break;
        case "social":
            contact.socials.push(infoContent);
            break;
        case "address":
            contact.address = infoContent; // 地址是单个值，直接覆盖
            break;
    }
    alert(`已为“${targetName}”添加${document.getElementById("infoType").options[document.getElementById("infoType").selectedIndex].text}！`);
    document.getElementById("infoContent").value = "";
}

// 3. 收藏联系人（标记黄色背景+已收藏标签）
function starContact() {
    const name = prompt("请输入要收藏的联系人姓名：");
    if (!name) return;
    const contact = contacts.find(item => item.name === name);
    if (!contact) {
        alert("未找到该联系人！");
        return;
    }
    contact.isStarred = true;
    alert(`联系人“${name}”已标记为收藏！`);
    // 收藏后自动刷新列表（如果列表已显示）
    if (document.getElementById("contactList").innerHTML !== "") {
        showAllContacts();
    }
}

// 4. 显示所有联系人（包含完整信息+收藏样式）
function showAllContacts() {
    const listContainer = document.getElementById("contactList");
    listContainer.innerHTML = ""; // 清空原有内容
    
    if (contacts.length === 0) {
        listContainer.innerHTML = "<p style='color: #999; text-align: center; padding: 20px;'>通讯录为空，请添加联系人</p>";
        return;
    }

    // 遍历渲染每个联系人
    contacts.forEach(contact => {
        const contactItem = document.createElement("div");
        // 收藏的联系人添加黄色背景样式
        contactItem.className = `contact-item ${contact.isStarred ? "starred" : ""}`;
        // 拼接联系方式（空值显示“无”）
        const phones = contact.phones.length > 0 ? contact.phones.join(" | ") : "无";
        const emails = contact.emails.length > 0 ? contact.emails.join(" | ") : "无";
        const socials = contact.socials.length > 0 ? contact.socials.join(" | ") : "无";
        const address = contact.address || "无";

        // 渲染联系人信息
        contactItem.innerHTML = `
            <h4>
                ${contact.name}
                ${contact.isStarred ? "<span style='color: #ffc107; margin-left: 8px;'>[已收藏]</span>" : ""}
            </h4>
            <div class="contact-info">
                <p><strong>电话：</strong>${phones}</p>
                <p><strong>邮箱：</strong>${emails}</p>
                <p><strong>社交账号：</strong>${socials}</p>
                <p><strong>地址：</strong>${address}</p>
            </div>
        `;
        listContainer.appendChild(contactItem);
    });
}