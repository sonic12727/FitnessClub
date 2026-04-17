document.addEventListener('DOMContentLoaded', function ()
{
    const openBtn = document.getElementById('openCallbackModalBtn');
    const form = document.getElementById('callbackForm');
    const navToggle = document.getElementById('navToggle');

    if (openBtn)
    {
        openBtn.addEventListener('click', openCallbackModal);
    }

    if (form)
    {
        form.addEventListener('submit', submitCallbackRequest);
    }

    if (navToggle)
    {
        navToggle.addEventListener('click', () =>
        {
            const navLinks = document.getElementById('navLinks');
            if (navLinks) navLinks.classList.toggle('show');
        });
    }
});

function openCallbackModal()
{
    const modal = document.getElementById('callbackModal');
    if (modal) modal.style.display = 'flex';
}

function closeCallbackModal()
{
    const modal = document.getElementById('callbackModal');
    if (modal) modal.style.display = 'none';
}

async function submitCallbackRequest(event)
{
    event.preventDefault();

    const name = document.getElementById('callbackName')?.value.trim();
    const phone = document.getElementById('callbackPhone')?.value.trim();

    if (!name)
    {
        showAlert('Введите имя', 'warning');
        return;
    }

    if (!phone)
    {
        showAlert('Введите телефон', 'warning');
        return;
    }

    if (!isValidPhone(phone))
    {
        showAlert('Введите корректный номер телефона', 'warning');
        return;
    }

    const payload =
    {
        name: name,
        phone: cleanPhoneNumber(phone)
    };

    try
    {
        await api.post('/api/public/callbacks', payload);

        showAlert('Заявка на перезвон отправлена!', 'success');

        const form = document.getElementById('callbackForm');

        if (form) form.reset();

        closeCallbackModal();
    }
    catch (error)
    {
        showAlert(error.message || 'Не удалось отправить заявку', 'error');
    }
}