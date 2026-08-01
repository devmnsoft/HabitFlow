document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("[data-customize-form]");
  if (!form) return;
  const frequency = form.querySelector("[data-frequency]");
  const days = form.querySelector("[data-custom-days]");
  const createGoal = form.querySelector("[data-create-goal]");
  const goalFields = form.querySelector("[data-goal-fields]");
  const updateConditionalFields = () => {
    if (days) days.hidden = frequency?.value !== "CustomWeekly";
    if (goalFields) goalFields.hidden = !createGoal?.checked;
  };
  frequency?.addEventListener("change", updateConditionalFields);
  createGoal?.addEventListener("change", updateConditionalFields);
  updateConditionalFields();

  form.querySelector(".validation-summary-errors, .field-validation-error")?.focus();
  form.addEventListener("submit", () => {
    if (!form.checkValidity()) return;
    const button = form.querySelector("[data-submit]");
    if (button) button.disabled = true;
    form.querySelector("[data-submit-spinner]")?.classList.remove("d-none");
    const status = form.querySelector("[data-submit-status]");
    if (status) status.textContent = "Adicionando o hábito. Aguarde.";
  });
});
