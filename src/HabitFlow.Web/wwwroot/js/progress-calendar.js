'use strict';
document.getElementById('view-toggle')?.addEventListener('click', event => {
    const content = document.getElementById('calendar-content');
    const enabled = content.classList.toggle('calendar-mode');
    event.currentTarget.textContent = enabled ? 'Lista' : 'Calendário';
    event.currentTarget.setAttribute('aria-pressed', String(enabled));
});
