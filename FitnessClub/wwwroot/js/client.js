
document.addEventListener('DOMContentLoaded', function () {
    // Проверка авторизации
    checkAuth();

    // Загрузка данных профиля
    loadProfileData();
    loadAttendances();
    checkCanCheckIn();

    // Назначение обработчиков событий
    const autoCheckInBtn = document.getElementById('autoCheckInBtn');
    if (autoCheckInBtn) {
        autoCheckInBtn.addEventListener('click', handleAutoCheckIn);
    }

    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }

    // Обработка мобильного меню
    const navToggle = document.getElementById('navToggle');
    if (navToggle) {
        navToggle.addEventListener('click', () => {
            const navLinks = document.getElementById('navLinks');
            if (navLinks) {
                navLinks.classList.toggle('show');
            }
        });
    }
});

async function loadProfileData() {
    try {
        const response = await api.get('/api/client/my-membership');

        // Заполнение профиля
        document.getElementById('userName').textContent = `${response.firstName} ${response.lastName}`;
        document.getElementById('userEmail').textContent = response.email;
        document.getElementById('totalVisits').textContent = response.totalVisits;
        document.getElementById('lastMonthVisits').textContent = response.lastMonthVisits;

        // Отображение абонемента
        const membershipContainer = document.getElementById('membershipInfo');
        if (response.membership) {
            const endDate = new Date(response.membership.endDate).toLocaleDateString('ru-RU');
            const startDate = new Date(response.membership.startDate).toLocaleDateString('ru-RU');
            const status = response.membership.isValid ? 'Активен' : 'Не активен';
            const statusClass = response.membership.isValid ? 'status-active' : 'status-inactive';

            membershipContainer.innerHTML = `
                <div class="membership-card">
                    <h3>Мой абонемент</h3>
                    <div class="membership-details">
                        <p><strong>Тип:</strong> ${getMembershipTypeName(response.membership.type)}</p>
                        <p><strong>Статус:</strong> <span class="${statusClass}">${status}</span></p>
                        <p><strong>Срок действия:</strong> ${startDate} - ${endDate}</p>
                        <p><strong>Стоимость:</strong> ${formatCurrency(response.membership.price)}</p>
                        ${response.membership.type === 'OneTime' ?
                    `<p><strong>Осталось посещений:</strong> ${response.membership.remainingVisits}</p>` : ''}
                    </div>
                </div>
            `;
        } else {
            membershipContainer.innerHTML = `
                <div class="membership-card">
                    <h3>Мой абонемент</h3>
                    <p class="no-membership">У вас нет активного абонемента</p>
                    <button class="btn btn-primary" onclick="window.location.href='/admin/dashboard.html'">Купить абонемент</button>
                </div>
            `;
        }

        // Обновление навигации
        updateNavigation();

    } catch (error) {
        console.error('Ошибка загрузки данных профиля:', error);
        showAlert('Не удалось загрузить данные профиля', 'error');
    }
}

async function loadAttendances() {
    try {
        const response = await api.get('/api/client/my-attendances');
        const container = document.getElementById('attendanceList');

        if (response.length === 0) {
            container.innerHTML = '<p class="no-data">Посещений пока нет</p>';
            return;
        }

        let html = '<table class="table"><thead><tr><th>Дата и время</th><th>Отметил</th></tr></thead><tbody>';

        response.forEach(attendance => {
            const date = formatDate(attendance.checkInTime);
            html += `
                <tr>
                    <td>${date}</td>
                    <td>${attendance.checkedByAdmin}</td>
                </tr>
            `;
        });

        html += '</tbody></table>';
        container.innerHTML = html;

    } catch (error) {
        console.error('Ошибка загрузки посещений:', error);
        showAlert('Не удалось загрузить историю посещений', 'error');
    }
}

async function checkCanCheckIn() {
    try {
        const response = await api.get('/api/client/can-check-in');
        const checkInBtn = document.getElementById('autoCheckInBtn');
        const statusElem = document.getElementById('checkInStatus');

        if (response.canCheckIn) {
            checkInBtn.disabled = false;
            statusElem.innerHTML = '<i class="fas fa-check-circle"></i> Можете отметить посещение';
            statusElem.className = 'status-success';
        } else {
            checkInBtn.disabled = true;
            statusElem.innerHTML = '<i class="fas fa-times-circle"></i> Не можете отметить посещение (уже отметились сегодня или нет абонемента)';
            statusElem.className = 'status-error';
        }
    } catch (error) {
        console.error('Ошибка проверки возможности отметки:', error);
    }
}

async function handleAutoCheckIn() {
    try {
        const response = await api.post('/api/attendance/auto-check-in', {});

        showAlert('Посещение успешно отмечено!', 'success');

        // Обновляем данные
        setTimeout(() => {
            loadProfileData();
            loadAttendances();
            checkCanCheckIn();
        }, 1000);

    } catch (error) {
        showAlert(error.message || 'Ошибка при отметке посещения', 'error');
    }
}

function getMembershipTypeName(type) {
    const types = {
        'OneTime': 'Разовый',
        'Monthly': 'Месячный',
        'Quarterly': 'Квартальный',
        'Yearly': 'Годовой'
    };
    return types[type] || type;
}

function updateNavigation() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    if (token) {
        // Скрываем кнопки входа/регистрации
        const loginLink = document.getElementById('loginLink');
        const registerLink = document.getElementById('registerLink');
        const adminLink = document.getElementById('adminLink');
        const clientLink = document.getElementById('clientLink');
        const logoutLink = document.getElementById('logoutLink');

        if (loginLink) loginLink.style.display = 'none';
        if (registerLink) registerLink.style.display = 'none';
        if (logoutLink) logoutLink.style.display = 'block';

        if (role === 'Admin') {
            if (adminLink) adminLink.style.display = 'block';
            if (clientLink) clientLink.style.display = 'none';
        } else {
            if (adminLink) adminLink.style.display = 'none';
            if (clientLink) clientLink.style.display = 'block';
        }
    }
}