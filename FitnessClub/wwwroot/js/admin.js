
document.addEventListener('DOMContentLoaded', function () {
    // Проверка авторизации и прав
    checkAuth();
    checkAdminRole();

    // Загрузка данных для админ-панели
    loadDashboardStats();
    loadRecentClients();
    loadTodayVisits();

    // Назначение обработчиков
    setupEventListeners();

    // Инициализация поиска клиентов
    const searchInput = document.getElementById('clientSearch');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(searchClients, 300));
    }

    // Настройка мобильного меню
    const navToggle = document.getElementById('navToggle');
    if (navToggle) {
        navToggle.addEventListener('click', () => {
            const navLinks = document.getElementById('navLinks');
            if (navLinks) navLinks.classList.toggle('show');
        });
    }
});

function checkAdminRole() {
    const role = localStorage.getItem('role');
    if (role !== 'Admin') {
        showAlert('У вас нет прав для доступа к админ-панели', 'error');
        setTimeout(() => window.location.href = '/client/profile.html', 2000);
        return false;
    }
    return true;
}

async function loadDashboardStats() {
    try {
        const response = await api.get('/api/admin/statistics');

        // Обновляем статистику на дашборде
        document.getElementById('totalClients').textContent = response.totalClients || 0;
        document.getElementById('activeMemberships').textContent = response.activeMemberships || 0;
        document.getElementById('todayVisits').textContent = response.todayVisits || 0;
        document.getElementById('monthlyRevenue').textContent = formatCurrency(response.monthlyRevenue || 0);

    } catch (error) {
        console.error('Ошибка загрузки статистики:', error);
        showAlert('Не удалось загрузить статистику', 'error');
    }
}

async function loadRecentClients() {
    try {
        const response = await api.get('/api/admin/search-clients');
        const container = document.getElementById('recentClients');

        if (response.length === 0) {
            container.innerHTML = '<tr><td colspan="5" class="text-center">Клиенты не найдены</td></tr>';
            return;
        }

        let html = '';
        response.slice(0, 10).forEach(client => {
            const membershipStatus = client.membership ?
                (client.membership.isValid ? 'Активен' : 'Истёк') :
                'Нет абонемента';

            const statusClass = client.membership?.isValid ? 'status-active' : 'status-inactive';

            html += `
                <tr>
                    <td>${client.firstName} ${client.lastName}</td>
                    <td>${client.email}</td>
                    <td>${client.phone || 'Не указан'}</td>
                    <td><span class="${statusClass}">${membershipStatus}</span></td>
                    <td>
                        <button class="btn btn-sm btn-primary" onclick="markAttendance(${client.id}, '${client.firstName} ${client.lastName}')">
                            <i class="fas fa-check-circle"></i> Отметить
                        </button>
                        <button class="btn btn-sm btn-secondary" onclick="addMembershipModal(${client.id})">
                            <i class="fas fa-id-card"></i> Абонемент
                        </button>
                    </td>
                </tr>
            `;
        });

        container.innerHTML = html;

    } catch (error) {
        console.error('Ошибка загрузки клиентов:', error);
    }
}

async function loadTodayVisits() {
    try {
        console.log('Загрузка сегодняшних посещений...');

        const today = new Date().toISOString().split('T')[0];
        const response = await api.get(`/api/admin/today-visits?date=${today}`);

        console.log('Ответ получен:', response);

        const container = document.getElementById('todayVisitsList');
        if (!container) {
            console.warn('Контейнер todayVisitsList не найден');
            return;
        }

        // ЗАЩИТА 1: Проверяем что response существует и это массив
        if (!response) {
            console.warn('Ответ пустой');
            container.innerHTML = '<tr><td colspan="3" class="text-center">Нет данных</td></tr>';
            return;
        }

        if (!Array.isArray(response)) {
            console.warn('Ответ не является массивом:', response);
            container.innerHTML = '<tr><td colspan="3" class="text-center">Ошибка формата данных</td></tr>';
            return;
        }

        // ЗАЩИТА 2: Проверяем массив
        if (response.length === 0) {
            container.innerHTML = '<tr><td colspan="3" class="text-center">Сегодня посещений ещё нет</td></tr>';
            return;
        }

        // ЗАЩИТА 3: Безопасная обработка каждого элемента
        let html = '';
        let errorCount = 0;

        response.forEach((visit, index) => {
            try {
                // Проверяем каждый уровень вложенности
                const user = visit?.User || visit?.user || {};
                const firstName = user?.FirstName || user?.firstName || '';
                const lastName = user?.LastName || user?.lastName || '';
                const userName = `${firstName} ${lastName}`.trim() || 'Неизвестный клиент';

                // Проверяем время
                let timeStr = 'Время не указано';
                if (visit?.CheckInTime || visit?.checkInTime) {
                    try {
                        const time = new Date(visit.CheckInTime || visit.checkInTime);
                        timeStr = time.toLocaleTimeString('ru-RU');
                    } catch (e) {
                        console.warn('Ошибка преобразования времени:', e);
                    }
                }

                // Проверяем кто отметил
                const checkedBy = visit?.CheckedByAdmin || visit?.checkedByAdmin || 'Система';

                html += `
                    <tr>
                        <td>${userName}</td>
                        <td>${timeStr}</td>
                        <td>${checkedBy}</td>
                    </tr>
                `;
            } catch (itemError) {
                errorCount++;
                console.error(`Ошибка обработки посещения ${index}:`, itemError);
            }
        });

        if (errorCount > 0) {
            console.warn(`Обработано с ${errorCount} ошибками`);
        }

        container.innerHTML = html;

    } catch (error) {
        console.error('Критическая ошибка в loadTodayVisits:', error);

        // Безопасный вывод ошибки
        const container = document.getElementById('todayVisitsList');
        if (container) {
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

async function searchClients() {
    const searchTerm = document.getElementById('clientSearch').value;
    const container = document.getElementById('clientSearchResults');

    if (!container) return;

    try {
        const response = await api.get(`/api/admin/search-clients?search=${encodeURIComponent(searchTerm)}`);

        if (response.length === 0) {
            container.innerHTML = '<tr><td colspan="5" class="text-center">Клиенты не найдены</td></tr>';
            return;
        }

        let html = '';
        response.forEach(client => {
            const membershipStatus = client.membership ?
                (client.membership.isValid ? 'Активен' : 'Истёк') :
                'Нет абонемента';

            const statusClass = client.membership?.isValid ? 'status-active' : 'status-inactive';

            html += `
                <tr>
                    <td>${client.firstName} ${client.lastName}</td>
                    <td>${client.email}</td>
                    <td>${client.phone || 'Не указан'}</td>
                    <td><span class="${statusClass}">${membershipStatus}</span></td>
                    <td>
                        <button class="btn btn-sm btn-primary" onclick="markAttendance(${client.id}, '${client.firstName} ${client.lastName}')">
                            <i class="fas fa-check-circle"></i> Отметить
                        </button>
                        <button class="btn btn-sm btn-secondary" onclick="addMembershipModal(${client.id})">
                            <i class="fas fa-id-card"></i> Абонемент
                        </button>
                    </td>
                </tr>
            `;
        });

        container.innerHTML = html;

    } catch (error) {
        console.error('Ошибка поиска клиентов:', error);
        showAlert('Ошибка при поиске клиентов', 'error');
    }
}

async function createClient() {
    const form = document.getElementById('createClientForm');
    if (!form) return;

    const formData = {
        email: document.getElementById('newClientEmail').value,
        password: document.getElementById('newClientPassword').value,
        firstName: document.getElementById('newClientFirstName').value,
        lastName: document.getElementById('newClientLastName').value,
        phone: document.getElementById('newClientPhone').value
    };

    try {
        const response = await api.post('/api/admin/create-client', formData);
        showAlert('Клиент успешно создан!', 'success');

        // Очистка формы
        form.reset();

        // Обновление списка клиентов
        loadRecentClients();
        searchClients();

        // Закрытие модального окна
        const modal = document.getElementById('createClientModal');
        if (modal) modal.style.display = 'none';

    } catch (error) {
        showAlert(error.message || 'Ошибка создания клиента', 'error');
    }
}

async function markAttendance(userId, userName) {
    if (!confirm(`Отметить посещение для ${userName}?`)) return;

    try {
        const response = await api.post('/api/admin/mark-attendance', { userId });
        showAlert('Посещение успешно отмечено!', 'success');

        // Обновление данных
        loadDashboardStats();
        loadTodayVisits();
        searchClients();

    } catch (error) {
        showAlert(error.message || 'Ошибка отметки посещения', 'error');
    }
}

function addMembershipModal(userId) {
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
    try {
        const membershipData = {
            userId: userId,
            type: document.getElementById('membershipType').value,
            durationMonths: parseInt(document.getElementById('membershipDuration').value),
            price: parseFloat(document.getElementById('membershipPrice').value)
        };

        const response = await api.post('/api/admin/add-membership', membershipData);
        showAlert('Абонемент успешно добавлен!', 'success');

        closeModal('addMembershipModal');
        searchClients();
        loadRecentClients();

    } catch (error) {
        showAlert(error.message || 'Ошибка добавления абонемента', 'error');
    }
}

function setupEventListeners() {
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }

    const createClientBtn = document.getElementById('createClientBtn');
    if (createClientBtn) {
        createClientBtn.addEventListener('click', () => {
            const modal = document.getElementById('createClientModal');
            if (modal) modal.style.display = 'block';
        });
    }

    const closeModalBtn = document.querySelector('.close-modal');
    if (closeModalBtn) {
        closeModalBtn.addEventListener('click', () => {
            const modal = document.getElementById('createClientModal');
            if (modal) modal.style.display = 'none';
        });
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.remove();
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function updateNavigation() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    if (token) {
        const loginLink = document.getElementById('loginLink');
        const registerLink = document.getElementById('registerLink');
        const adminLink = document.getElementById('adminLink');
        const logoutLink = document.getElementById('logoutLink');

        if (loginLink) loginLink.style.display = 'none';
        if (registerLink) registerLink.style.display = 'none';
        if (logoutLink) logoutLink.style.display = 'block';

        if (role === 'Admin') {
            if (adminLink) adminLink.style.display = 'block';
            
        }
        else
        {
            if (adminLink) adminLink.style.display = 'none';
            
        }
    }
    else
    {
        // Если не авторизован
        if (loginLink) loginLink.style.display = 'block';
        if (registerLink) registerLink.style.display = 'block';
        if (adminLink) adminLink.style.display = 'none';
        if (logoutLink) logoutLink.style.display = 'none';
    }
}