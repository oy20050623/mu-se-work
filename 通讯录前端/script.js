// 全局联系人数组
let contacts = [];

// 通用AJAX请求函数
function request(url, method = 'GET', data = null, isFormData = false) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        // 替换为你的后端实际运行地址（端口要匹配）
        xhr.open(method, `https://localhost:5001/api${url}`);
        
        if (!isFormData) {
            xhr.setRequestHeader('Content-Type', 'application/json');
        }
        
        xhr.onload = function() {
            if (xhr.status >= 200 && xhr.status < 300) {
                resolve(xhr.responseText ? JSON.parse(xhr.responseText) : null);
            } else {
                reject(new Error(`请求失败：${xhr.status} - ${xhr.statusText}`));
            }
        };
        
        xhr.onerror = function() {
            reject(new Error('网络错误，请检查后端是否启动'));
        };
        
        xhr.send(isFormData ? data : (data ? JSON.stringify(data) : null));
    });
}

// 加载联系人列表
async function loadContacts() {
    try {
        const res = await request('/contacts');
        contacts = res;
        renderContacts();
    } catch (err) {
        alert('加载联系人失败：' + err.message);
        console.error(err);
    }
}

// 渲染联系人列表
function renderContacts() {
    const container = document.getElementById('contactsContainer');
    container.innerHTML = '';

    if (contacts.length === 0) {
        container.innerHTML = '<p>暂无联系人数据</p>';
        return;
    }

    contacts.forEach(contact => {
        const contactItem = document.createElement('div');
        contactItem.className = `contact-item ${contact.isBookmarked ? 'bookmarked' : ''}`;
        
        let detailsHtml = '';
        if (contact.contactDetails && contact.contactDetails.length > 0) {
            contact.contactDetails.forEach(detail => {
                detailsHtml += `<div class="contact-detail">${detail.type}：${detail.value}</div>`;
            });
        } else {
            detailsHtml = '<div class="contact-detail">暂无联系方式</div>';
        }

        contactItem.innerHTML = `
            <h3>
                ${contact.name}（ID：${contact.id}）
                <button class="bookmark-btn" onclick="toggleBookmark(${contact.id})">
                    ${contact.isBookmarked ? '取消收藏' : '收藏'}
                </button>
            </h3>
            ${detailsHtml}
        `;

        container.appendChild(contactItem);
    });
}

// 添加联系人
async function addContact() {
    const nameInput = document.getElementById('contactName');
    const name = nameInput.value.trim();
    
    if (!name) {
        alert('请输入联系人姓名！');
        return;
    }

    try {
        const newContact = await request('/contacts', 'POST', {
            name: name,
            isBookmarked: false
        });
        
        contacts.push(newContact);
        renderContacts();
        nameInput.value = '';
        alert('联系人添加成功！');
    } catch (err) {
        alert('添加联系人失败：' + err.message);
        console.error(err);
    }
}

// 切换收藏状态
async function toggleBookmark(contactId) {
    const contact = contacts.find(c => c.id === contactId);
    if (!contact) return;

    try {
        await request(`/contacts/${contactId}/bookmark`, 'PATCH', {
            isBookmarked: !contact.isBookmarked
        });
        
        contact.isBookmarked = !contact.isBookmarked;
        renderContacts();
    } catch (err) {
        alert('收藏状态修改失败：' + err.message);
        console.error(err);
    }
}

// 添加联系方式
async function addDetail() {
    const contactIdInput = document.getElementById('detailContactId');
    const typeInput = document.getElementById('detailType');
    const valueInput = document.getElementById('detailValue');
    
    const contactId = parseInt(contactIdInput.value);
    const type = typeInput.value.trim();
    const value = valueInput.value.trim();
    
    if (!contactId || !type || !value) {
        alert('请填写完整的联系方式信息！');
        return;
    }

    try {
        const newDetail = await request(`/contacts/${contactId}/details`, 'POST', {
            type: type,
            value: value
        });
        
        const contact = contacts.find(c => c.id === contactId);
        if (contact) {
            if (!contact.contactDetails) contact.contactDetails = [];
            contact.contactDetails.push(newDetail);
            renderContacts();
        }
        
        contactIdInput.value = '';
        typeInput.value = '';
        valueInput.value = '';
        alert('联系方式添加成功！');
    } catch (err) {
        alert('添加联系方式失败：' + err.message);
        console.error(err);
    }
}

// 导出Excel
document.getElementById('exportBtn').addEventListener('click', async function() {
    try {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', 'https://localhost:5001/api/contacts/export');
        xhr.responseType = 'blob';
        
        xhr.onload = function() {
            if (xhr.status === 200) {
                const blob = new Blob([xhr.response], { 
                    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
                });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = '通讯录列表.xlsx';
                a.click();
                URL.revokeObjectURL(url);
                alert('导出成功！');
            } else {
                alert('导出失败：' + xhr.statusText);
            }
        };
        
        xhr.onerror = function() {
            alert('导出失败：网络错误');
        };
        
        xhr.send();
    } catch (err) {
        alert('导出失败：' + err.message);
        console.error(err);
    }
});

// 导入Excel
document.getElementById('importBtn').addEventListener('click', function() {
    document.getElementById('importFile').click();
});

document.getElementById('importFile').addEventListener('change', async function(e) {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
        const res = await request('/contacts/import', 'POST', formData, true);
        alert(res.message);
        loadContacts();
        this.value = '';
    } catch (err) {
        alert('导入失败：' + err.message);
        console.error(err);
    }
});

// 页面加载初始化
window.onload = function() {
    loadContacts();
};