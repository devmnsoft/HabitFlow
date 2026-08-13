(() => {
  const tip = document.querySelector('[data-product-tip]');
  if (!tip) return;
  const title = tip.querySelector('#product-tip-title')?.textContent.trim();
  const content = tip.querySelector('#product-tip-content')?.textContent.trim();
  if (!title || !content || !tip.dataset.tipId) { tip.hidden = true; return; }
  const trigger = document.querySelector(tip.dataset.target || '');
  const place = () => {
    if (!trigger || matchMedia('(max-width: 575.98px)').matches) {
      tip.classList.add('hf-product-tip--fallback');
      return;
    }
    const anchor = trigger.getBoundingClientRect();
    const width = Math.min(352, innerWidth - 32);
    tip.style.left = `${Math.max(16, Math.min(anchor.left, innerWidth - width - 16))}px`;
    tip.style.top = `${Math.min(anchor.bottom + 12, innerHeight - tip.offsetHeight - 16)}px`;
  };
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const close = async () => {
    const response = await fetch(`/product-tips/${tip.dataset.tipId}/dismiss`, {
      method: 'POST', headers: token ? { RequestVerificationToken: token } : {}
    });
    if (!response.ok) {
      tip.querySelector('[data-tip-status]').textContent = 'Não foi possível salvar agora.';
      return;
    }
    tip.hidden = true;
    trigger?.focus();
  };
  tip.querySelectorAll('[data-tip-dismiss],[data-tip-understood]').forEach(button => button.addEventListener('click', close));
  tip.addEventListener('keydown', event => { if (event.key === 'Escape') { event.preventDefault(); close(); } });
  tip.hidden = false;
  place();
  tip.querySelector('[data-tip-understood]')?.focus({ preventScroll: true });
  addEventListener('resize', place, { passive: true });
})();
