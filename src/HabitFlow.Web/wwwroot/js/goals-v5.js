(() => {
  'use strict';
  const dialog = document.querySelector('[data-goal-dialog]');
  const opener = document.querySelector('[data-goal-dialog-open]');
  if (!dialog || !opener) return;
  let returnFocus = opener;
  const close = () => { dialog.close(); returnFocus.focus(); };
  opener.addEventListener('click', () => { returnFocus = document.activeElement; dialog.showModal(); });
  dialog.querySelector('[data-goal-dialog-close]')?.addEventListener('click', close);
  dialog.addEventListener('click', event => { if (event.target === dialog) close(); });
  dialog.addEventListener('cancel', event => { event.preventDefault(); close(); });
})();
