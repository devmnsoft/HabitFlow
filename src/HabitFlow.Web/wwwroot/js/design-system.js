(() => {
  'use strict';
  document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(element => {
    if (window.bootstrap?.Tooltip) window.bootstrap.Tooltip.getOrCreateInstance(element);
  });
  document.documentElement.classList.add('hf-design-system-ready');
})();
