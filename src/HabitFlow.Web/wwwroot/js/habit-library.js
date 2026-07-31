document.addEventListener("submit", async (event) => {
  const form = event.target.closest(".hf-favorite-form");
  if (!form) return;
  event.preventDefault();
  const button = form.querySelector("button");
  const status = document.querySelector("[data-favorite-status]");
  button.disabled = true;
  try {
    const response = await fetch(form.action, { method: "POST", body: new FormData(form), headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error("favorite request failed");
    const result = await response.json();
    button.textContent = result.favorite ? "Remover favorito" : "Favoritar";
    button.setAttribute("aria-pressed", String(result.favorite));
    form.action = form.action.replace(result.favorite ? /\/favorite$/ : /\/unfavorite$/, result.favorite ? "/unfavorite" : "/favorite");
    status.textContent = result.message;
  } catch { status.textContent = "Não foi possível atualizar o favorito agora."; }
  finally { button.disabled = false; button.focus(); }
});
