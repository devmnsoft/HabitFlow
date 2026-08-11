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
  const element = (tag, className, text) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  };
  const replaceResults = node => results.replaceChildren(node);
  const message = (className, title, description) => {
    const container = element('div', className);
    container.append(element('strong', '', title), element('span', '', description));
    return container;
  };
  const render = data => {
    activeIndex = -1; input.removeAttribute('aria-activedescendant');
    if (!data.groups.length) {
      replaceResults(message('hf-search-empty', 'Nada por aqui ainda.', 'Tente outra palavra ou confira a ortografia.'));
      status.textContent = 'Nenhum resultado encontrado.'; return;
    }
    const groups = data.groups.reduce((all, item) => { (all[item.type] ??= []).push(item); return all; }, {});
    let number = 0;
    const fragment = document.createDocumentFragment();
    Object.entries(groups).forEach(([type, entries]) => {
      const section = document.createElement('section');
      section.append(element('h3', 'hf-search-group-title', type));
      entries.forEach(item => {
        const link = element('a', 'hf-search-item');
        link.id = `hf-search-result-${number++}`;
        link.setAttribute('role', 'option');
        link.setAttribute('aria-selected', 'false');
        link.href = typeof item.url === 'string' && item.url.startsWith('/') ? item.url : '#';
        const icon = element('span', 'hf-search-icon', item.icon === 'target' ? '◎' : item.icon === 'book' ? '▤' : '✓');
        icon.setAttribute('aria-hidden', 'true');
        const copy = element('span', 'hf-search-copy');
        copy.append(element('strong', '', item.title), element('span', '', item.description));
        const arrow = element('span', '', '→');
        arrow.setAttribute('aria-hidden', 'true');
        link.append(icon, copy, arrow);
        section.append(link);
      });
      fragment.append(section);
    });
    results.replaceChildren(fragment);
    status.textContent = `${data.groups.length} resultados encontrados.`;
  };
  const search = async () => {
    const query = input.value.trim();
    if (query.length < 2) { replaceResults(message('hf-search-welcome', 'Continue digitando…', 'Use pelo menos 2 caracteres.')); return; }
    request?.abort(); request = new AbortController();
    const loading = element('div', 'hf-search-loading');
    loading.setAttribute('aria-hidden', 'true');
    loading.append(document.createElement('span'), document.createElement('span'), document.createElement('span'));
    replaceResults(loading); status.textContent = 'Buscando…';
    try { const response = await fetch(`/global-search?q=${encodeURIComponent(query)}`, { signal: request.signal, headers: { Accept: 'application/json' } }); if (!response.ok) throw new Error(); render(await response.json()); }
    catch (error) { if (error.name !== 'AbortError') { replaceResults(message('hf-search-empty', 'Não foi possível buscar agora.', 'Tente novamente em instantes.')); status.textContent = 'Erro ao buscar.'; } }
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
