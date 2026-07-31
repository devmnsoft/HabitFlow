document.addEventListener("submit", (event) => {
  const form = event.target.closest("[data-customize-form]");
  if (!form || !form.checkValidity()) return;
  const button = form.querySelector("[data-submit]");
  button.disabled = true;
  button.textContent = "Adicionando…";
  form.querySelector("[data-submit-status]").textContent = "Adicionando o hábito. Aguarde.";
});
