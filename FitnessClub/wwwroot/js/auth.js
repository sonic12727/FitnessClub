// Обработка авторизации
document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;

            try {
                const response = await api.post('/api/auth/login', { email, password });

                console.log('Login response:', response);

                if (response.token) {
                    // Сохраняем все данные
                    localStorage.setItem('token', response.token);
                    localStorage.setItem('role', response.role);
                    localStorage.setItem('userName', response.firstName + ' ' + response.lastName);
                    localStorage.setItem('userId', response.id);
                    localStorage.setItem('userEmail', response.email);

                    // Обновляем токен в API клиенте
                    api.token = response.token;

                    showAlert('Вход выполнен успешно!', 'success');

                    // Редирект в зависимости от роли
                    setTimeout(() => {
                        if (response.role === 'Admin') {
                            window.location.href = '/admin/dashboard.html';
                        } else {
                            window.location.href = '/client/profile.html';
                        }
                    }, 1000);
                } else {
                    showAlert('Не удалось получить токен', 'error');
                }
            } catch (error) {
                console.error('Login error:', error);
                showAlert(error.message || 'Ошибка входа', 'error');
            }
        });
    }

    if (registerForm) {
        registerForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const formData = {
                email: document.getElementById('email').value,
                password: document.getElementById('password').value,
                firstName: document.getElementById('firstName').value,
                lastName: document.getElementById('lastName').value,
                phone: document.getElementById('phone').value,
                role: 'Client'
            };

            try {
                const response = await api.post('/api/auth/register', formData);
                showAlert('Регистрация успешна! Теперь войдите в систему.', 'success');
                setTimeout(() => {
                    window.location.href = '/login.html';
                }, 2000);
            } catch (error) {
                showAlert(error.message || 'Ошибка регистрации', 'error');
            }
        });
    }

    // Проверка авторизации при загрузке страницы
    checkAuth();
});

function checkAuth() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const currentPage = window.location.pathname;

    console.log('checkAuth:', {
        token: token ? 'Есть' : 'Нет',
        role,
        currentPage
    });

    // Если нет токена и пытаемся зайти на защищённую страницу
    if (!token && (currentPage.includes('/admin/') || currentPage.includes('/client/'))) {
        console.log('Нет токена, редирект на логин');
        window.location.href = '/login.html';
        return;
    }

    // Если есть токен и пытаемся зайти на страницу логина/регистрации
    if (token && (currentPage.includes('/login.html') || currentPage.includes('/register.html'))) {
        console.log('Есть токен, редирект в ЛК');
        if (role === 'Admin') {
            window.location.href = '/admin/dashboard.html';
        } else {
            window.location.href = '/client/profile.html';
        }
        return;
    }

    // Проверка прав доступа для админ-панели
    if (token && currentPage.includes('/admin/') && role !== 'Admin') {
        console.log('Нет прав админа, редирект в ЛК клиента');
        showAlert('У вас нет прав для доступа к админ-панели', 'error');
        setTimeout(() => {
            window.location.href = '/client/profile.html';
        }, 1000);
    }
}

function logout() {
    localStorage.clear();
    window.location.href = '/';
}