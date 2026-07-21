(() => {
  const button = document.getElementById('demoCompleteBtn');
  if (!button) return;
  button.addEventListener('click', () => {
    document.getElementById('demoDone').textContent = '4';
    document.getElementById('demoProgressText').textContent = '100%';
    document.getElementById('demoProgressBar').style.width = '100%';
    document.querySelector('#demoPendingHabit span').textContent = 'concluído';
    document.getElementById('demoMessage').classList.remove('d-none');
    button.remove();
  });
})();
