'use strict';
document.querySelectorAll('.js-cycle').forEach((button) => button.addEventListener('click', () => {
  document.querySelectorAll('.js-cycle').forEach((item) => { item.classList.toggle('active', item === button); item.classList.toggle('btn-success', item === button); item.classList.toggle('btn-outline-success', item !== button); });
  const yearly = button.dataset.cycle === 'Yearly';
  document.querySelectorAll('.js-price').forEach((item) => { item.textContent = yearly ? item.dataset.yearly : item.dataset.monthly; });
  document.querySelectorAll('.js-period').forEach((item) => { item.textContent = yearly ? '/ano' : '/mês'; });
  document.querySelectorAll('.js-cycle-input').forEach((item) => { item.value = button.dataset.cycle; });
}));
