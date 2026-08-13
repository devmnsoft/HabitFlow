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

const library = document.querySelector("[data-library]");
if (library) {
  const form = library.querySelector("[data-library-filters]");
  const cards = [...library.querySelectorAll("[data-template]")];
  const count = library.querySelector("[data-library-count]");
  const empty = library.querySelector("[data-library-empty]");
  const value = (name) => String(new FormData(form).get(name) || "").toLowerCase();
  const applyFilters = () => {
    const selected = { focus: value("focus"), category: value("category"), difficulty: value("difficulty"), duration: Number(value("duration")), frequency: value("frequency"), plan: value("minimumPlan"), favorites: form.elements.favoritesOnly?.checked };
    let visible = 0;
    cards.forEach((card) => {
      const matches = (!selected.focus || card.dataset.focus.toLowerCase() === selected.focus)
        && (!selected.category || card.dataset.category.toLowerCase() === selected.category)
        && (!selected.difficulty || card.dataset.difficulty.toLowerCase() === selected.difficulty)
        && (!selected.duration || Number(card.dataset.duration) <= selected.duration)
        && (!selected.frequency || card.dataset.frequency.toLowerCase() === selected.frequency)
        && (!selected.plan || card.dataset.plan.toLowerCase() === selected.plan)
        && (!selected.favorites || card.dataset.favorite === "true");
      card.hidden = !matches;
      if (matches) visible += 1;
    });
    count.textContent = `${visible} ${visible === 1 ? "sugestão encontrada" : "sugestões encontradas"}`;
    empty.hidden = visible !== 0;
  };
  form.addEventListener("input", applyFilters);
  form.addEventListener("submit", (event) => { event.preventDefault(); applyFilters(); history.replaceState(null, "", `${form.action}?${new URLSearchParams(new FormData(form))}`); });
  library.querySelector("[data-clear-filters]")?.addEventListener("click", () => { form.reset(); applyFilters(); form.querySelector("select")?.focus(); });
  applyFilters();
}
