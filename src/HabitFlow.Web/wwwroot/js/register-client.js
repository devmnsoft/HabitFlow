(() => {
  const root = document.querySelector('[data-register-client]');
  if (!root) return;
  const radios = [...root.querySelectorAll('[data-person-type]')];
  const doc = root.querySelector('[data-document-input]');
  const docType = root.querySelector('[data-document-type]');
  const label = root.querySelector('[data-document-label]');
  const help = root.querySelector('[data-person-help]');
  const feedback = root.querySelector('[data-document-feedback]');
  const pf = root.querySelector('[data-pf-fields]');
  const pj = root.querySelector('[data-pj-fields]');
  const digits = v => (v || '').replace(/\D/g, '');
  const maskCpf = v => digits(v).slice(0,11).replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d{1,2})$/,'$1-$2');
  const maskCnpj = v => digits(v).slice(0,14).replace(/(\d{2})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1.$2').replace(/(\d{3})(\d)/,'$1/$2').replace(/(\d{4})(\d{1,2})$/,'$1-$2');
  const isPj = () => radios.some(r => r.checked && r.value === 'LegalPerson');
  function sync() {
    const pjSelected = isPj();
    pf.hidden = pjSelected; pj.hidden = !pjSelected;
    docType.value = pjSelected ? 'CNPJ' : 'CPF';
    label.textContent = pjSelected ? 'CNPJ' : 'CPF';
    doc.placeholder = pjSelected ? '00.000.000/0000-00' : '000.000.000-00';
    help.textContent = pjSelected ? 'Use CNPJ quando a conta for para uma empresa, equipe ou organização.' : 'Use CPF quando a conta for para uso individual.';
    doc.value = pjSelected ? maskCnpj(doc.value) : maskCpf(doc.value);
    const len = digits(doc.value).length, max = pjSelected ? 14 : 11;
    feedback.textContent = len ? `${len}/${max} dígitos` : '';
  }
  radios.forEach(r => r.addEventListener('change', sync));
  doc?.addEventListener('input', sync);
  if (!radios.some(r => r.checked)) radios[0].checked = true;
  sync();
})();
