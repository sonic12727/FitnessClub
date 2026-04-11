const API_BASE = 'https://localhost:7185';

class ApiClient
{
    constructor()
    {
        this.token = localStorage.getItem('token');
    }

    async request(endpoint, options = {})
    {
        const url = `${API_BASE}${endpoint}`;

        // Обновляем токен перед каждым запросом
        this.token = localStorage.getItem('token');

        const headers =
        {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (this.token)
        {
            headers['Authorization'] = `Bearer ${this.token}`;
        }

        console.log('API Request:',
        {
            url,
            method: options.method || 'GET',
            hasToken: !!this.token
        });

        try
        {
            const response = await fetch(url,
            {
                ...options,
                headers
            });

            console.log('API Response:',
            {
                status: response.status,
                ok: response.ok
            });

            if (response.status === 401)
            {
                console.log('Токен недействителен');
                localStorage.clear();
                window.location.href = '/login.html';
                throw new Error('Сессия истекла');
            }

            if (!response.ok)
            {
                let errorMessage = `HTTP ${response.status}`;
                const contentType = response.headers.get('content-type') || '';

                if (contentType.includes('application/json'))
                {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.error || errorData.title || JSON.stringify(errorData);
                }
                else
                {
                    const errorText = (await response.text()).trim();
                    errorMessage = errorText.replace(/^"|"$/g, '');
                }

                console.error('API Error Response:', errorMessage);
                throw new Error(errorMessage);
            }

            const data = await response.json();
            return data;
        }
        catch (error)
        {
            console.error('API Request Error:', error);
            throw error;
        }
    }

    async get(endpoint)
    {
        return this.request(endpoint, { method: 'GET' });
    }

    async post(endpoint, data)
    {
        return this.request(endpoint,
        {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async put(endpoint, data)
    {
        return this.request(endpoint,
        {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async delete(endpoint)
    {
        return this.request(endpoint, { method: 'DELETE' });
    }
}

// Создаём экземпляр API клиента
const api = new ApiClient();

// Вспомогательные функции
function showAlert(message, type = 'info')
{
    const overlay = document.createElement('div');
    overlay.className = 'alert-overlay';

    const alert = document.createElement('div');
    alert.className = `alert alert-${type}`;
    alert.innerHTML =
    `
        <div class="alert-content">
            <i class="fas fa-${type === 'success'
            ? 'check-circle'
            : type === 'error'
                ? 'exclamation-circle'
                : type === 'warning'
                    ? 'triangle-exclamation'
                    : 'info-circle'}"></i>
            <span>${message}</span>
        </div>
        <button class="alert-close" type="button">&times;</button>
    `;

    overlay.appendChild(alert);
    document.body.appendChild(overlay);

    requestAnimationFrame(() =>
    {
        alert.classList.add('show');
        overlay.classList.add('show');
    });

    const closeAlert = () =>
    {
        alert.classList.remove('show');
        overlay.classList.remove('show');

        setTimeout(() =>
        {
            overlay.remove();
        }, 220);
    };

    alert.querySelector('.alert-close').addEventListener('click', closeAlert);

    overlay.addEventListener('click', (e) =>
    {
        if (e.target === overlay) closeAlert();
    });

    setTimeout(closeAlert, 3500);
}

function formatDate(dateString)
{
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU',
    {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function formatCurrency(amount)
{
    return new Intl.NumberFormat('ru-RU',
    {
        style: 'currency',
        currency: 'KZT',
        minimumFractionDigits: 0
    }).format(amount);
}


// Форматирование даты для отображения
function formatDateTime(dateString)
{
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU',
    {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Форматирование даты без времени
function formatDateOnly(dateString)
{
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

// Проверка email
function isValidEmail(email)
{
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

// Проверка телефона
function isValidPhone(phone)
{
    const re = /^[\+]?[0-9\s\-\(\)]+$/;
    return re.test(phone);
}

// Очистка номера телефона
function cleanPhoneNumber(phone)
{
    return phone.replace(/[^\d+]/g, '');
}

// Дебаунс для поиска
function debounce(func, wait)
{
    let timeout;
    return function executedFunction(...args)
    {
        const later = () =>
        {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Создание элемента с сообщением
function createMessageElement(message, type = 'info')
{
    const div = document.createElement('div');
    div.className = `message message-${type}`;
    div.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
        <span>${message}</span>
    `;
    return div;
}

// Показать загрузку
function showLoading(element)
{
    if (!element) return;
    element.innerHTML = '<div class="loading"></div>';
}

// Скрыть загрузку
function hideLoading(element, originalContent = '')
{
    if (!element) return;
    element.innerHTML = originalContent;
}

// Копирование в буфер обмена
function copyToClipboard(text)
{
    navigator.clipboard.writeText(text).then(() =>
    {
        showAlert('Скопировано в буфер обмена', 'success');
    }).catch(err =>
    {
        console.error('Ошибка копирования: ', err);
    });
}