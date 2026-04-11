
document.addEventListener('DOMContentLoaded', function ()
{
    loadDashboardStats();
    loadRecentClients();
    loadTodayVisits();
    updateDashboardMeta();
    setupEventListeners();

    const searchInput = document.getElementById('clientSearch');

    if (searchInput)
    {
        searchInput.addEventListener('input', debounce(searchClients, 300));
    }

    const navToggle = document.getElementById('navToggle');

    if (navToggle)
    {
        navToggle.addEventListener('click', () =>
        {
            const navLinks = document.getElementById('navLinks');
            if (navLinks) navLinks.classList.toggle('show');
        });
    }
});

async function refreshDashboard()
{
    await loadDashboardStats();
    await loadRecentClients();
    await loadTodayVisits();
    updateDashboardMeta();
}

async function loadDashboardStats()
{
    try
    {
        const response = await api.get('/api/admin/statistics');

        const totalClientsEl = document.getElementById('totalClients');
        const activeMembershipsEl = document.getElementById('activeMemberships');
        const todayVisitsEl = document.getElementById('todayVisits');
        const monthlyRevenueEl = document.getElementById('monthlyRevenue');

        if (totalClientsEl) totalClientsEl.textContent = response.totalClients || 0;
        if (activeMembershipsEl) activeMembershipsEl.textContent = response.activeMemberships || 0;
        if (todayVisitsEl) todayVisitsEl.textContent = response.todayVisits || 0;
        if (monthlyRevenueEl) monthlyRevenueEl.textContent = formatCurrency(response.totalRevenue || 0);
    }
    catch (error)
    {
        console.error('Ошибка загрузки статистики:', error);
        showAlert('Не удалось загрузить статистику', 'error');
    }
}

async function loadRecentClients()
{
    try
    {
        const response = await api.get('/api/admin/clients');
        const container = document.getElementById('recentClients');
        if (!container) return;

        container.innerHTML = renderRecentClientsRows(response.slice(0, 5));
    }
    catch (error)
    {
        console.error('Ошибка загрузки клиентов:', error);
        showAlert('Не удалось загрузить клиентов', 'error');
    }
}

function getLocalDateString()
{
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

async function loadTodayVisits()
{
    try
    {
        const today = getLocalDateString();
        const response = await api.get(`/api/admin/attendance/today-visits?date=${today}`);
        const container = document.getElementById('todayVisitsList');

        if (!container) return;

        if (!Array.isArray(response) || response.length === 0)
        {
            container.innerHTML = '<tr><td colspan="3" class="text-center">Сегодня посещений ещё нет</td></tr>';
            return;
        }

        let html = '';

        response.forEach(visit =>
        {
            const user = visit.user || {};
            const userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Неизвестный клиент';
            const timeStr = visit.checkInTime
                ? new Date(visit.checkInTime).toLocaleTimeString('ru-RU')
                : 'Время не указано';

            html += `
                <tr>
                    <td>${userName}</td>
                    <td>${timeStr}</td>
                    <td>${visit.checkedByAdmin || 'Система'}</td>
                </tr>
            `;
        });

        container.innerHTML = html;
    }
    catch (error)
    {
        console.error('Критическая ошибка в loadTodayVisits:', error);

        const container = document.getElementById('todayVisitsList');
        if (container)
        {
            container.innerHTML = `
                <tr>
                    <td colspan="3" class="text-center text-muted">
                        Информация о посещениях временно недоступна
                    </td>
                </tr>
            `;
        }
    }
}

async function searchClients()
{
    const searchInput = document.getElementById('clientSearch');
    const container = document.getElementById('clientSearchResults');
    const countEl = document.getElementById('clientCount');

    if (!searchInput || !container) return;

    const searchTerm = searchInput.value.trim();

    try
    {
        const response = await api.get(`/api/admin/clients?search=${encodeURIComponent(searchTerm)}`);

        container.innerHTML = renderClientsRows(response);

        if (countEl)
        {
            countEl.textContent = Array.isArray(response) ? response.length : 0;
        }
    }
    catch (error)
    {
        console.error('Ошибка поиска клиентов:', error);

        container.innerHTML =
        `
            <tr>
                <td colspan="7" class="text-center text-muted">
                    Не удалось загрузить результаты поиска
                </td>
            </tr>
        `;

        if (countEl)
        {
            countEl.textContent = '0';
        }
    }
}
function renderClientsRows(clients)
{
    if (!clients || clients.length === 0)
    {
        return '<tr><td colspan="7" class="text-center">Клиенты не найдены</td></tr>';
    }

    let html = '';

    clients.forEach(client =>
    {
        const fullName = `${client.firstName || ''} ${client.lastName || ''}`.trim() || '—';
        const email = client.email && client.email.trim() !== '' ? client.email : '—';
        const phone = client.phone && client.phone.trim() !== '' ? client.phone : '—';
        const createdAt = client.createdAt ? formatDateOnly(client.createdAt) : '—';

        const membership = client.membership || null;
        const membershipStatus = membership ? (membership.isValid ? 'Активен' : 'Истёк') : 'Истёк';
        const statusClass = membership?.isValid ? 'status-active' : 'status-inactive';

        const hasVisitedToday = !!client.hasVisitedToday;
        const attendanceBtnClass = hasVisitedToday ? 'btn btn-sm btn-success' : 'btn btn-sm btn-primary';
        const attendanceBtnText = hasVisitedToday ? 'Отмечен' : 'Отметить';

        html += `
            <tr>
                <td>${fullName}</td>
                <td>${email}</td>
                <td>${phone}</td>
                <td>${createdAt}</td>
                <td>${formatMembershipInfo(membership)}</td>
                <td><span class="${statusClass}">${membershipStatus}</span></td>
                <td>
                    <div class="client-actions">
                        <button class="${attendanceBtnClass}"
                            onclick="markAttendance(${client.id}, '${fullName.replace(/'/g, "\\'")}', ${!!membership?.isValid}, ${hasVisitedToday})">
                            <i class="fas fa-check-circle"></i> ${attendanceBtnText}
                        </button>
                        <button class="btn btn-sm btn-primary" onclick="openEditClientModal(${client.id})">
                            <i class="fas fa-pen"></i> Редактировать
                        </button>
                        <button class="btn btn-sm btn-primary" onclick="addMembershipModal(${client.id})">
                            <i class="fas fa-id-card"></i> Абонемент
                        </button>
                    </div>
                </td>
            </tr>
        `;
    });

    return html;
}

function renderRecentClientsRows(clients)
{
    if (!clients || clients.length === 0)
    {
        return '<tr><td colspan="5" class="text-center">Клиенты не найдены</td></tr>';
    }

    let html = '';

    clients.forEach(client =>
    {
        const fullName = `${client.firstName || ''} ${client.lastName || ''}`.trim() || '—';
        const email = client.email && client.email.trim() !== '' ? client.email : '—';
        const phone = client.phone || 'Не указан';
        const membershipStatus = client.membership ? (client.membership.isValid ? 'Активен' : 'Истёк') : 'Нет абонемента';
        const statusClass = client.membership?.isValid ? 'status-active' : 'status-inactive';
        const hasVisitedToday = !!client.hasVisitedToday;
        const buttonClass = hasVisitedToday ? 'btn btn-sm btn-success' : 'btn btn-sm btn-primary';
        const buttonText = hasVisitedToday ? 'Отмечен' : 'Отметить';

        html +=
        `
            <tr>
                <td>${fullName}</td>
                <td>${email}</td>
                <td>${phone}</td>
                <td><span class="${statusClass}">${membershipStatus}</span></td>
                <td>
                    <button class="${buttonClass}"
                        onclick="markAttendance(${client.id}, '${fullName.replace(/'/g, "\\'")}', ${!!client.membership?.isValid}, ${hasVisitedToday})">
                        <i class="fas fa-check-circle"></i> ${buttonText}
                    </button>
                </td>
            </tr>
        `;
    });

    return html;
}

async function createClient()
{
    const form = document.getElementById('createClientForm');
    if (!form) return;

    const rawEmail = document.getElementById('newClientEmail').value.trim();

    const formData =
    {
        email: rawEmail === '' ? null : rawEmail,
        password: document.getElementById('newClientPassword').value.trim(),
        firstName: document.getElementById('newClientFirstName').value.trim(),
        lastName: document.getElementById('newClientLastName').value.trim(),
        phone: document.getElementById('newClientPhone').value.trim()
    };

    try
    {
        const response = await api.post('/api/admin/clients', formData);
        showAlert('Клиент успешно создан!', 'success');

        // Очистка формы
        form.reset();

        // Обновление списка клиентов
        loadRecentClients();
        searchClients();

        // Закрытие модального окна
        const modal = document.getElementById('createClientModal');
        if (modal) modal.style.display = 'none';

    }
    catch (error)
    {
        showAlert(error.message || 'Ошибка создания клиента', 'error');
    }
}

async function openEditClientModal(clientId)
{
    try
    {
        const client = await api.get(`/api/admin/clients/${clientId}`);

        document.getElementById('editClientId').value = client.id;
        document.getElementById('editClientEmail').value = client.email || '';
        document.getElementById('editClientPassword').value = '';
        document.getElementById('editClientFirstName').value = client.firstName || '';
        document.getElementById('editClientLastName').value = client.lastName || '';
        document.getElementById('editClientPhone').value = client.phone || '';

        document.getElementById('editClientModal').style.display = 'flex';
    }
    catch (error) 
    {
        showAlert(error.message || 'Не удалось загрузить клиента', 'error');
    }
}

async function updateClient()
{
    try
    {
        const id = document.getElementById('editClientId').value;
        const rawEmail = document.getElementById('editClientEmail').value.trim();
        const rawPassword = document.getElementById('editClientPassword').value.trim();

        const formData =
        {
            email: rawEmail === '' ? null : rawEmail,
            password: rawPassword === '' ? null : rawPassword,
            firstName: document.getElementById('editClientFirstName').value.trim(),
            lastName: document.getElementById('editClientLastName').value.trim(),
            phone: document.getElementById('editClientPhone').value.trim()
        };

        await api.put(`/api/admin/clients/${id}`, formData);

        showAlert('Данные клиента обновлены', 'success');
        closeModal('editClientModal');
        searchClients();
        loadRecentClients();
    }
    catch (error)
    {
        showAlert(error.message || 'Ошибка обновления клиента', 'error');
    }
}

async function markAttendance(userId, userName, hasActiveMembership = true, hasVisitedToday = false)
{
    if (!hasActiveMembership)
    {
        showAlert('У клиента нет активного абонемента', 'error');
        return;
    }

    if (hasVisitedToday)
    {
        showAlert('Клиент уже был отмечен сегодня', 'warning');
        return;
    }

    if (!confirm(`Отметить посещение для ${userName}?`)) return;

    try
    {
        await api.post('/api/admin/attendance/mark', { userId });
        showAlert('Посещение успешно отмечено!', 'success');

        loadDashboardStats();
        loadTodayVisits();
        loadRecentClients();
        searchClients();
    }
    catch (error)
    {
        showAlert(error.message || 'Ошибка отметки посещения', 'error');
    }
}

function toggleMembershipDuration()
{
    const type = document.getElementById('membershipType')?.value;
    const group = document.getElementById('membershipDurationGroup');

    if (!group) return;

    group.style.display = isVisitBasedMembership(type) ? 'none' : 'block';
}

function getMembershipPreset(type)
{
    const presets =
    {
        OneTime: { title: 'Разовый', price: 3000, description: '1 посещение' },
        Visits8: { title: '8 посещений', price: 12000, description: 'Пакет на 8 посещений' },
        Visits12: { title: '12 посещений', price: 16000, description: 'Пакет на 12 посещений' },
        Monthly: { title: 'Месячный', price: 20000, description: 'Срок действия: 1 месяц' },
        Quarterly: { title: 'Квартальный', price: 40000, description: 'Срок действия: 3 месяца' },
        Yearly: { title: 'Годовой', price: 100000, description: 'Срок действия: 12 месяцев' }
    };

    return presets[type] || null;
}

function updateMembershipPresetInfo()
{
    const type = document.getElementById('membershipType')?.value;
    const info = document.getElementById('membershipPresetInfo');

    if (!info) return;

    const preset = getMembershipPreset(type);

    if (!preset)
    {
        info.innerHTML = '<p>Параметры абонемента не найдены</p>';
        return;
    }

    info.innerHTML =
    `
        <div class="info-item">
            <div class="info-label">Название</div>
            <div class="info-value">${preset.title}</div>
        </div>
        <div class="info-item">
            <div class="info-label">Стоимость</div>
            <div class="info-value">${formatCurrency(preset.price)}</div>
        </div>
        <div class="info-item">
            <div class="info-label">Параметры</div>
            <div class="info-value">${preset.description}</div>
        </div>
    `;
}

function addMembershipModal(userId)
{
    const modalHtml =
    `
        <div id="addMembershipModal" class="modal" style="display: flex;">
            <div class="modal-content">
                <span class="close-modal" onclick="closeModal('addMembershipModal')">&times;</span>
                <h3>Добавить абонемент</h3>

                <div class="form-group">
                    <label>Тип абонемента:</label>
                    <select id="membershipType" class="form-control" onchange="updateMembershipPresetInfo()">
                        <option value="OneTime">Разовое посещение</option>
                        <option value="Visits8">8 посещений</option>
                        <option value="Visits12">12 посещений</option>
                        <option value="Monthly">Месячный</option>
                        <option value="Quarterly">Квартальный</option>
                        <option value="Yearly">Годовой</option>
                    </select>
                </div>

                <div id="membershipPresetInfo" class="client-info-grid" style="margin-top: 20px;"></div>

                <div class="modal-buttons">
                    <button onclick="saveMembership(${userId})" class="btn btn-primary">Сохранить</button>
                    <button onclick="closeModal('addMembershipModal')" class="btn btn-secondary">Отмена</button>
                </div>
            </div>
        </div>
    `;

    const oldModal = document.getElementById('addMembershipModal');
    if (oldModal) oldModal.remove();

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    updateMembershipPresetInfo();
}

async function saveMembership(userId)
{
    try
    {
        const membershipData =
        {
            userId: userId,
            type: document.getElementById('membershipType').value
        };

        await api.post('/api/admin/memberships', membershipData);
        showAlert('Абонемент успешно добавлен!', 'success');

        closeModal('addMembershipModal');
        searchClients();
        loadRecentClients();
    }
    catch (error)
    {
        showAlert(error.message || 'Ошибка добавления абонемента', 'error');
    }
}

function logout()
{
    localStorage.clear();
    window.location.href = '/';
}

function setupEventListeners()
{
    const logoutLink = document.getElementById('logoutLink');

    if (logoutLink)
    {
        logoutLink.addEventListener('click', function (e)
        {
            e.preventDefault();
            logout();
        });
    }

    const createClientBtn = document.getElementById('createClientBtn');

    if (createClientBtn)
    {
        createClientBtn.addEventListener('click', () =>
        {
            const modal = document.getElementById('createClientModal');
            if (modal) modal.style.display = 'flex';
        });
    }

    document.querySelectorAll('.close-modal').forEach(btn =>
    {
        btn.addEventListener('click', () =>
        {
            const modal = btn.closest('.modal');

            if (!modal) return;

            if (modal.id === 'addMembershipModal')
            {
                modal.remove();
            }
            else
            {
                modal.style.display = 'none';
            }
        });
    });
}

function closeModal(modalId)
{
    const modal = document.getElementById(modalId);
    if (!modal) return;

    if (modalId === 'addMembershipModal')
    {
        modal.remove();
        return;
    }

    modal.style.display = 'none';
}

function updateDashboardMeta()
{
    const serverDateEl = document.getElementById('serverDate');
    const lastUpdateEl = document.getElementById('lastUpdate');
    const now = new Date();

    if (serverDateEl)
    {
        serverDateEl.textContent = now.toLocaleString('ru-RU');
    }

    if (lastUpdateEl)
    {
        lastUpdateEl.textContent = now.toLocaleTimeString('ru-RU');
    }
}

function isVisitBasedMembership(type)
{
    return ['OneTime', 'Visits8', 'Visits12'].includes(type);
}

function isTimeBasedMembership(type)
{
    return ['Monthly', 'Quarterly', 'Yearly'].includes(type);
}

function getMembershipTypeName(type)
{
    const types =
    {
        1: 'Разовое посещение',
        2: '8 посещений',
        3: '12 посещений',
        4: 'Месячный',
        5: 'Квартальный',
        6: 'Годовой',

        'OneTime': 'Разовое посещение',
        'Visits8': '8 посещений',
        'Visits12': '12 посещений',
        'Monthly': 'Месячный',
        'Quarterly': 'Квартальный',
        'Yearly': 'Годовой'
    };

    return types[type] || type || '—';
}

function formatMembershipInfo(membership)
{
    if (!membership)
    {
        return '<span class="no-membership">Нет абонемента</span>';
    }

    const typeName = getMembershipTypeName(membership.type);

    if (isTimeBasedMembership(membership.type))
    {
        const start = membership.startDate ? formatDateOnly(membership.startDate) : '—';
        const end = membership.endDate ? formatDateOnly(membership.endDate) : '—';

        return `
            <div>
                <div>${typeName}</div>
                <small class="text-muted">${start} — ${end}</small>
            </div>
        `;
    }

    if (isVisitBasedMembership(membership.type))
    {
        return `
            <div>
                <div>${typeName}</div>
                <small class="text-muted">Осталось: ${membership.remainingVisits ?? 0}</small>
            </div>
        `;
    }

    return `<span>${typeName}</span>`;
}
