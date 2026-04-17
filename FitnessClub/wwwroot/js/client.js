
document.addEventListener('DOMContentLoaded', function ()
{
    checkAuth();
    loadProfileData();
    loadAttendances();
    

    const logoutBtn = document.getElementById('logoutBtn');

    if (logoutBtn)
    {
        logoutBtn.addEventListener('click', logout);
    }

    const adminDashboardBtn = document.getElementById('adminDashboardBtn');

    if (adminDashboardBtn)
    {
        const savedEmail = (localStorage.getItem('userEmail') || '').trim().toLowerCase();

        if (savedEmail === 'admin@fitness.ru')
        {
            adminDashboardBtn.style.display = 'inline-flex';
            adminDashboardBtn.addEventListener('click', function () {
                window.location.href = '/admin/dashboard.html';
            });
        }
        else
        {
            adminDashboardBtn.style.display = 'none';
        }
    }

    // Обработка мобильного меню
    const navToggle = document.getElementById('navToggle');

    if (navToggle)
    {
        navToggle.addEventListener('click', () =>
        {
            const navLinks = document.getElementById('navLinks');

            if (navLinks)
            {
                navLinks.classList.toggle('show');
            }
        });
    }
});

function getMembershipTypeName(type)
{
    const types =
    {
        'OneTime': 'Разовое посещение',
        'Visits8': '8 посещений',
        'Visits12': '12 посещений',
        'Monthly': 'Месячный',
        'Quarterly': 'Квартальный',
        'Yearly': 'Годовой',

        1: 'Разовое посещение',
        2: '8 посещений',
        3: '12 посещений',
        4: 'Месячный',
        5: 'Квартальный',
        6: 'Годовой'
    };

    return types[type] || '—';
}

async function loadProfileData()
{
    try
    {
        const [profileResponse, membershipResponse] = await Promise.all
        ([
            api.get('/api/client/profile'),
            api.get('/api/client/membership')
        ]);

        // Профиль
        document.getElementById('userName').textContent =`${profileResponse.firstName || ''} ${profileResponse.lastName || ''}`.trim();
        document.getElementById('userEmail').textContent = profileResponse.email && profileResponse.email.trim() !== '' ? profileResponse.email : '—';

        document.getElementById('totalVisits').textContent = profileResponse.totalVisits ?? 0;
        document.getElementById('lastMonthVisits').textContent = profileResponse.lastMonthVisits ?? 0;

        // Абонемент
        const membershipContainer = document.getElementById('membershipInfo');

        if (membershipResponse.membership)
        {
            const type = membershipResponse.membership.type;
            const isVisitBased = ['OneTime', 'Visits8', 'Visits12'].includes(type);
            const isTimeBased = ['Monthly', 'Quarterly', 'Yearly'].includes(type);

            const status = membershipResponse.membership.isValid ? 'Активен' : 'Не активен';
            const statusClass = membershipResponse.membership.isValid ? 'status-active' : 'status-inactive';

            if (isTimeBased)
            {
                const startDate = new Date(membershipResponse.membership.startDate).toLocaleDateString('ru-RU');
                const endDate = new Date(membershipResponse.membership.endDate).toLocaleDateString('ru-RU');

                membershipContainer.innerHTML =`
                    <div class="membership-card">
                        <h3>Мой абонемент</h3>
                        <div class="membership-details">
                            <p><strong>Тип:</strong> ${getMembershipTypeName(type)}</p>
                            <p><strong>Статус:</strong> <span class="${statusClass}">${status}</span></p>
                            <p><strong>Дата приобретения:</strong> ${startDate}</p>
                            <p><strong>Дата истечения:</strong> ${endDate}</p>
                            <p><strong>Стоимость:</strong> ${formatCurrency(membershipResponse.membership.price)}</p>
                        </div>
                    </div>
                `;
            }
            else if (isVisitBased)
            {
                membershipContainer.innerHTML =`
                    <div class="membership-card">
                        <h3>Мой абонемент</h3>
                        <div class="membership-details">
                            <p><strong>Тип:</strong> ${getMembershipTypeName(type)}</p>
                            <p><strong>Статус:</strong> <span class="${statusClass}">${status}</span></p>
                            <p><strong>Осталось посещений:</strong> ${membershipResponse.membership.remainingVisits ?? 0}</p>
                            <p><strong>Стоимость:</strong> ${formatCurrency(membershipResponse.membership.price)}</p>
                        </div>
                    </div>
                `;
            }
            else
            {
                membershipContainer.innerHTML =`
                    <div class="membership-card">
                        <h3>Мой абонемент</h3>
                        <div class="membership-details">
                            <p><strong>Тип:</strong> ${getMembershipTypeName(type)}</p>
                            <p><strong>Статус:</strong> <span class="${statusClass}">${status}</span></p>
                        </div>
                    </div>
                `;
            }
        }
        else
        {
            membershipContainer.innerHTML =`
                <div class="membership-card">
                    <h3>Мой абонемент</h3>
                    <p class="no-membership">У вас нет активного абонемента</p>
                </div>
            `;
        }
    }
    catch (error)
    {
        console.error('Ошибка загрузки данных профиля:', error);
        showAlert('Не удалось загрузить данные профиля', 'error');
    }
}

async function loadAttendances()
{
    try
    {
        const response = await api.get('/api/client/attendance');
        const container = document.getElementById('attendanceList');

        if (response.length === 0)
        {
            container.innerHTML = '<p class="no-data">Посещений пока нет</p>';
            return;
        }

        let html = '<table class="table"><thead><tr><th>Дата и время</th><th>Отметил</th></tr></thead><tbody>';

        response.forEach(attendance =>
        {
            const date = formatDate(attendance.checkInTime);
            html += `
                <tr>
                    <td>${date}</td>
                    <td>${attendance.checkedByAdmin ?? 'Система'}</td>
                </tr>
            `;
        });

        html += '</tbody></table>';
        container.innerHTML = html;

    }
    catch (error)
    {
        console.error('Ошибка загрузки посещений:', error);
        showAlert('Не удалось загрузить историю посещений', 'error');
    }
}
