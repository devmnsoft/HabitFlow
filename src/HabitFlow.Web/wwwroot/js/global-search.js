(() => {
  const root = document.querySelector('#globalSearch');
  if (!root) return;
  const input = root.querySelector('#globalSearchInput');
  const results = root.querySelector('#globalSearchResults');
  const status = root.querySelector('#globalSearchStatus');
  let timer, request, activeIndex = -1, returnFocus;

  const items = () => [...results.querySelectorAll('.hf-search-item')];
  const select = index => {
    const links = items();
    if (!links.length) return;
    activeIndex = (index + links.length) % links.length;
    links.forEach((link, i) => link.setAttribute('aria-selected', String(i === activeIndex)));
    input.setAttribute('aria-activedescendant', links[activeIndex].id);
    links[activeIndex].scrollIntoView({ block: 'nearest' });
  };
  const open = trigger => {
    returnFocus = trigger;
    root.hidden = false; root.setAttribute('aria-hidden', 'false');
    document.body.classList.add('hf-search-open'); input.focus();
  };
  const close = () => {
    request?.abort(); root.hidden = true; root.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('hf-search-open'); returnFocus?.focus();
  };
  const escape = value => { const node = document.createElement('span'); node.textContent = value ?? ''; return node.innerHTML; };
  const render = data => {
    activeIndex = -1; input.removeAttribute('aria-activedescendant');
    if (!data.groups.length) {
      results.innerHTML = '<div class="hf-search-empty"><strong>Nada por aqui ainda.</strong><span>Tente outra palavra ou confira a ortografia.</span></div>';
      status.textContent = 'Nenhum resultado encontrado.'; return;
    }
    const groups = data.groups.reduce((all, item) => { (all[item.type] ??= []).push(item); return all; }, {});
    let number = 0;
    results.innerHTML = Object.entries(groups).map(([type, entries]) => `<section><h3 class="hf-search-group-title">${escape(type)}</h3>${entries.map(item => { const id = `hf-search-result-${number++}`; return `<a id="${id}" class="hf-search-item" role="option" aria-selected="false" href="${escape(item.url)}"><span class="hf-search-icon" aria-hidden="true">${item.icon === 'target' ? '◎' : item.icon === 'book' ? '▤' : '✓'}</span><span class="hf-search-copy"><strong>${escape(item.title)}</strong><span>${escape(item.description)}</span></span><span aria-hidden="true">→</span></a>`; }).join('')}</section>`).join('');
    status.textContent = `${data.groups.length} resultados encontrados.`;
  };
  const search = async () => {
    const query = input.value.trim();
    if (query.length < 2) { results.innerHTML = '<div class="hf-search-welcome"><strong>Continue digitando…</strong><span>Use pelo menos 2 caracteres.</span></div>'; return; }
    request?.abort(); request = new AbortController();
    results.innerHTML = '<div class="hf-search-loading" aria-hidden="true"><span></span><span></span><span></span></div>'; status.textContent = 'Buscando…';
    try { const response = await fetch(`/global-search?q=${encodeURIComponent(query)}`, { signal: request.signal, headers: { Accept: 'application/json' } }); if (!response.ok) throw new Error(); render(await response.json()); }
    catch (error) { if (error.name !== 'AbortError') { results.innerHTML = '<div class="hf-search-empty"><strong>Não foi possível buscar agora.</strong><span>Tente novamente em instantes.</span></div>'; status.textContent = 'Erro ao buscar.'; } }
  };
  document.querySelectorAll('[data-search-open]').forEach(trigger => trigger.addEventListener('click', () => open(trigger)));
  root.querySelectorAll('[data-search-close]').forEach(trigger => trigger.addEventListener('click', close));
  input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(search, 250); });
  root.addEventListener('keydown', event => {
    if (event.key === 'Escape') { event.preventDefault(); close(); }
    else if (event.key === 'ArrowDown') { event.preventDefault(); select(activeIndex + 1); }
    else if (event.key === 'ArrowUp') { event.preventDefault(); select(activeIndex - 1); }
    else if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); items()[activeIndex].click(); }
    else if (event.key === 'Tab') { const focusable = [input, ...items(), ...root.querySelectorAll('[data-search-close]')]; const first = focusable[0], last = focusable.at(-1); if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); } else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); } }
  });
})();
