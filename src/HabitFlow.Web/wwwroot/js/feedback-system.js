(() => {
  'use strict';
  const host = document.querySelector('[data-toast-host]');
  const showToast = (type, title, message, duration = 4200) => {
    if (!host) return;
    const toast = document.createElement('div');
    toast.className = 'toast hf-toast'; toast.dataset.type = type;
    toast.setAttribute('role', type === 'error' ? 'alert' : 'status');
    const body = document.createElement('div'); body.className = 'toast-body';
    const heading = document.createElement('strong'); heading.className = 'd-block'; heading.textContent = title;
    const text = document.createElement('span'); text.textContent = message;
    body.append(heading, text); toast.append(body); host.append(toast);
    const instance = window.bootstrap?.Toast.getOrCreateInstance(toast, { delay: duration });
    toast.addEventListener('hidden.bs.toast', () => toast.remove(), { once: true });
    instance?.show();
  };
  window.HabitFlowFeedback = Object.freeze({ showToast });
})();
