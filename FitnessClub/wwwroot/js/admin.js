
document.addEventListener('DOMContentLoaded', function ()
{
    loadDashboardStats();
    loadRecentClients();
    loadTodayVisits();
    setupEventListeners();

    // Инициализация поиска клиентов
    const searchInput = document.getElementById('clientSearch');
    if (searchInput)
    {
        searchInput.addEventListener('input', debounce(searchClients, 300));
    }

    // Настройка мобильного меню
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

async function loadDashboardStats()
{
    try
    {
        const response = await api.get('/api/admin/statistics');

        // Обновляем статистику на дашборде
        document.getElementById('totalClients').textContent = response.totalClients || 0;
        document.getElementById('activeMemberships').textContent = response.activeMemberships || 0;
        document.getElementById('todayVisits').textContent = response.todayVisits || 0;
        document.getElementById('totalRevenue').textContent = formatCurrency(response.totalRevenue || 0);

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

        container.innerHTML = renderClientsRows(response.slice(0, 10));
    }
    catch (error)
    {
        console.error('Ошибка загрузки клиентов:', error);
        showAlert('Не удалось загрузить клиентов', 'error');
    }
}

async function loadTodayVisits()
{
    try
    {
        const today = new Date().toISOString().split('T')[0];
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

    if (!searchInput || !container) return;

    const searchTerm = searchInput.value.trim();

    try
    {
        const response = await api.get(`/api/admin/clients?search=${encodeURIComponent(searchTerm)}`);
        container.innerHTML = renderClientsRows(response);
    }
    catch (error)
    {
        console.error('Ошибка поиска клиентов:', error);
        showAlert('Ошибка при поиске клиентов', 'error');
    }
}
function renderClientsRows(clients)
{
    if (!clients || clients.length === 0)
    {
        return '<tr><td colspan="5" class="text-center">Клиенты не найдены</td></tr>';
    }

    let html = '';

    clients.forEach(client =>
    {
        const membershipStatus = client.membership
            ? (client.membership.isValid ? 'Активен' : 'Истёк')
            : 'Нет абонемента';

        const statusClass = client.membership?.isValid
            ? 'status-active'
            : 'status-inactive';

        const fullName = `${client.firstName} ${client.lastName}`.trim();

        html += `
            <tr>
                <td>${fullName}</td>
                <td>${client.email}</td>
                <td>${client.phone || 'Не указан'}</td>
                <td><span class="${statusClass}">${membershipStatus}</span></td>
                <td>
                    <button class="btn btn-sm btn-primary" onclick="markAttendance(${client.id}, '${fullName}')">
                        <i class="fas fa-check-circle"></i> Отметить
                    </button>
                    <button class="btn btn-sm btn-secondary" onclick="addMembershipModal(${client.id})">
                        <i class="fas fa-id-card"></i> Абонемент
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

    const formData =
    {
        email: document.getElementById('newClientEmail').value,
        password: document.getElementById('newClientPassword').value,
        firstName: document.getElementById('newClientFirstName').value,
        lastName: document.getElementById('newClientLastName').value,
        phone: document.getElementById('newClientPhone').value
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

async function markAttendance(userId, userName)
{
    if (!confirm(`Отметить посещение для ${userName}?`)) return;

    try
    {
        const response = await api.post('/api/admin/attendance/mark', { userId });
        showAlert('Посещение успешно отмечено!', 'success');

        // Обновление данных
        loadDashboardStats();
        loadTodayVisits();
        searchClients();

    }
    catch (error)
    {
        showAlert(error.message || 'Ошибка отметки посещения', 'error');
    }
}

function addMembershipModal(userId)
{
    // Создаем модальное окно для добавления абонемента
    const modalHtml = `
        <div id="addMembershipModal" class="modal" style="display: block;">
            <div class="modal-content">
                <h3>Добавить абонемент</h3>
                <div class="form-group">
                    <label>Тип абонемента:</label>
                    <select id="membershipType" class="form-control">
                        <option value="OneTime">Разовый (1 посещение)</option>
                        <option value="Monthly">Месячный</option>
                        <option value="Quarterly">Квартальный</option>
                        <option value="Yearly">Годовой</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Длительность (месяцев):</label>
                    <input type="number" id="membershipDuration" class="form-control" value="1" min="1" max="60">
                </div>
                <div class="form-group">
                    <label>Стоимость (₽):</label>
                    <input type="number" id="membershipPrice" class="form-control" value="1000" min="0" step="100">
                </div>
                <div class="modal-buttons">
                    <button onclick="saveMembership(${userId})" class="btn btn-primary">Сохранить</button>
                    <button onclick="closeModal('addMembershipModal')" class="btn btn-secondary">Отмена</button>
                </div>
            </div>
        </div>
    `;

    // Добавляем модальное окно на страницу
    let modal = document.getElementById('addMembershipModal');
    if (modal) modal.remove();

    document.body.insertAdjacentHTML('beforeend', modalHtml);
}

async function saveMembership(userId) {
    try
    {
        const membershipData =
        {
            userId: userId,
            type: document.getElementById('membershipType').value,
            durationMonths: parseInt(document.getElementById('membershipDuration').value),
            price: parseFloat(document.getElementById('membershipPrice').value)
        };

        const response = await api.post('/api/admin/memberships', membershipData);
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

function setupEventListeners()
{
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn)
    {
        logoutBtn.addEventListener('click', logout);
    }

    const createClientBtn = document.getElementById('createClientBtn');
    if (createClientBtn)
    {
        createClientBtn.addEventListener('click', () =>
        {
            const modal = document.getElementById('createClientModal');
            if (modal) modal.style.display = 'block';
        });
    }

    document.querySelectorAll('.close-modal').forEach(btn =>
    {
        btn.addEventListener('click', () =>
        {
            const modal = document.getElementById('createClientModal');
            if (modal) modal.style.display = 'none';
        });
    });
}

function closeModal(modalId)
{
    const modal = document.getElementById(modalId);
    if (modal) modal.remove();
}