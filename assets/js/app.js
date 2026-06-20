import { auth, db, googleProvider, firebaseApi } from "./firebase.js";

const MAX_FREE_HABITS = 5;
let currentUser = null;
let habits = [];
let unsubscribeHabits = null;

const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => Array.from(document.querySelectorAll(selector));

const authModal = new bootstrap.Modal($("#authModal"));
const habitModal = new bootstrap.Modal($("#habitModal"));
const toast = new bootstrap.Toast($("#appToast"));

const todayKey = () => new Date().toISOString().slice(0, 10);

function showToast(title, message) {
  $("#toastTitle").textContent = title;
  $("#toastBody").textContent = message;
  toast.show();
}

function setLoading(button, isLoading, originalText) {
  if (!button) return;
  button.disabled = isLoading;
  button.innerHTML = isLoading ? '<span class="spinner-border spinner-border-sm me-2"></span>Aguarde...' : originalText;
}

function setAuthUi(user) {
  const logged = Boolean(user);
  $$(".public-only").forEach(el => el.classList.toggle("d-none", logged));
  $$(".auth-only").forEach(el => el.classList.toggle("d-none", !logged));
  if (logged) {
    $("#userGreeting").textContent = `Olá, ${user.displayName || user.email || "usuário"}. Acompanhe sua consistência de hoje.`;
    location.hash = "dashboard";
  }
}

function userHabitsCollection() {
  return firebaseApi.collection(db, "users", currentUser.uid, "habits");
}

function habitDocument(id) {
  return firebaseApi.doc(db, "users", currentUser.uid, "habits", id);
}

function listenHabits() {
  if (unsubscribeHabits) unsubscribeHabits();
  $("#loadingState").classList.remove("d-none");
  const q = firebaseApi.query(userHabitsCollection(), firebaseApi.orderBy("createdAt", "desc"));
  unsubscribeHabits = firebaseApi.onSnapshot(q, (snapshot) => {
    habits = snapshot.docs.map(docItem => ({ id: docItem.id, ...docItem.data() }));
    renderHabits();
  }, (error) => {
    console.error(error);
    showToast("Erro", "Não foi possível carregar os hábitos. Confira as regras do Firestore.");
    $("#loadingState").classList.add("d-none");
  });
}

function renderHabits() {
  $("#loadingState").classList.add("d-none");
  $("#emptyState").classList.toggle("d-none", habits.length > 0);
  $("#limitAlert").classList.toggle("d-none", habits.length < MAX_FREE_HABITS);

  const today = todayKey();
  const doneToday = habits.filter(h => (h.completedDates || []).includes(today)).length;
  const best = habits.reduce((max, h) => Math.max(max, getBestStreak(h.completedDates || [])), 0);
  $("#kpiTotal").textContent = habits.length;
  $("#kpiDoneToday").textContent = doneToday;
  $("#kpiBestStreak").textContent = best;

  $("#habitsList").innerHTML = habits.map(habitCardHtml).join("");
  bindHabitCardEvents();
}

function habitCardHtml(habit) {
  const completedDates = habit.completedDates || [];
  const done = completedDates.includes(todayKey());
  const currentStreak = getCurrentStreak(completedDates);
  const bestStreak = getBestStreak(completedDates);
  const history = getLastDays(30).map(day => {
    const isDone = completedDates.includes(day.key);
    return `<span class="history-day ${isDone ? "done" : ""} ${day.key === todayKey() ? "today" : ""}" title="${day.label}: ${isDone ? "feito" : "pendente"}"></span>`;
  }).join("");

  return `
    <article class="habit-card" style="--habit-color:${escapeHtml(habit.color || "#10B981")}">
      <div class="habit-color-bar"></div>
      <div class="habit-title-row">
        <div>
          <h2 class="habit-title">${escapeHtml(habit.name || "Hábito")}</h2>
          <div class="habit-meta">${done ? "Concluído hoje" : "Pendente hoje"}</div>
        </div>
        <div class="habit-actions">
          <button class="icon-btn btn-edit" data-id="${habit.id}" title="Editar"><i class="bi bi-pencil"></i></button>
          <button class="icon-btn btn-delete" data-id="${habit.id}" title="Excluir"><i class="bi bi-trash"></i></button>
        </div>
      </div>
      <button class="check-btn ${done ? "done" : ""}" data-id="${habit.id}">
        <i class="bi ${done ? "bi-check-circle-fill" : "bi-circle"} me-2"></i>${done ? "Feito hoje" : "Marcar como feito"}
      </button>
      <div class="habit-stats">
        <span class="stat-pill"><i class="bi bi-fire me-1"></i>Streak atual: ${currentStreak}</span>
        <span class="stat-pill"><i class="bi bi-trophy me-1"></i>Maior: ${bestStreak}</span>
        <span class="stat-pill"><i class="bi bi-calendar-check me-1"></i>Total: ${completedDates.length}</span>
      </div>
      <div class="history-grid">${history}</div>
    </article>
  `;
}

function bindHabitCardEvents() {
  $$(".check-btn").forEach(button => button.addEventListener("click", () => toggleToday(button.dataset.id)));
  $$(".btn-edit").forEach(button => button.addEventListener("click", () => openEdit(button.dataset.id)));
  $$(".btn-delete").forEach(button => button.addEventListener("click", () => deleteHabit(button.dataset.id)));
}

async function toggleToday(id) {
  const habit = habits.find(h => h.id === id);
  if (!habit) return;
  const today = todayKey();
  const set = new Set(habit.completedDates || []);
  const wasDone = set.has(today);
  wasDone ? set.delete(today) : set.add(today);
  const completedDates = Array.from(set).sort();
  await firebaseApi.updateDoc(habitDocument(id), { completedDates });
  showToast("Progresso atualizado", wasDone ? "Conclusão de hoje removida." : "Parabéns! Hábito marcado como feito hoje.");
}

function openEdit(id) {
  const habit = habits.find(h => h.id === id);
  if (!habit) return;
  $("#habitModalLabel").textContent = "Editar hábito";
  $("#habitId").value = habit.id;
  $("#habitName").value = habit.name || "";
  $("#habitColor").value = habit.color || "#10B981";
  $("#btnSaveHabit").textContent = "Salvar alterações";
  $("#btnCancelEdit").classList.remove("d-none");
  habitModal.show();
}

function resetHabitForm() {
  $("#habitModalLabel").textContent = "Novo hábito";
  $("#habitId").value = "";
  $("#habitForm").reset();
  $("#habitColor").value = "#10B981";
  $("#btnSaveHabit").textContent = "Salvar hábito";
  $("#btnCancelEdit").classList.add("d-none");
}

async function deleteHabit(id) {
  const habit = habits.find(h => h.id === id);
  if (!habit) return;
  if (!confirm(`Excluir o hábito "${habit.name}"? Essa ação não pode ser desfeita.`)) return;
  await firebaseApi.deleteDoc(habitDocument(id));
  showToast("Hábito excluído", "O hábito foi removido com sucesso.");
}

function getLastDays(total) {
  const formatter = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit" });
  return Array.from({ length: total }, (_, index) => {
    const date = new Date();
    date.setDate(date.getDate() - (total - 1 - index));
    const key = date.toISOString().slice(0, 10);
    return { key, label: formatter.format(date) };
  });
}

function getCurrentStreak(dates) {
  const set = new Set(dates);
  let streak = 0;
  const cursor = new Date();
  while (set.has(cursor.toISOString().slice(0, 10))) {
    streak++;
    cursor.setDate(cursor.getDate() - 1);
  }
  return streak;
}

function getBestStreak(dates) {
  const sorted = [...new Set(dates)].sort();
  let best = 0;
  let current = 0;
  let previous = null;
  for (const dateKey of sorted) {
    if (!previous) current = 1;
    else {
      const prevDate = new Date(previous);
      prevDate.setDate(prevDate.getDate() + 1);
      current = prevDate.toISOString().slice(0, 10) === dateKey ? current + 1 : 1;
    }
    best = Math.max(best, current);
    previous = dateKey;
  }
  return best;
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char]));
}

$("#btnGoogle").addEventListener("click", async () => {
  const btn = $("#btnGoogle");
  const original = btn.innerHTML;
  setLoading(btn, true, original);
  try {
    await firebaseApi.signInWithPopup(auth, googleProvider);
    authModal.hide();
  } catch (error) {
    console.error(error);
    showToast("Falha no login", "Não foi possível entrar com Google. Verifique se o provedor está habilitado no Firebase.");
  } finally {
    setLoading(btn, false, original);
  }
});

$("#authForm").addEventListener("submit", async (event) => {
  event.preventDefault();
  const btn = $("#btnEmailLogin");
  const original = btn.innerHTML;
  setLoading(btn, true, original);
  const email = $("#authEmail").value.trim();
  const password = $("#authPassword").value;
  try {
    try {
      await firebaseApi.signInWithEmailAndPassword(auth, email, password);
    } catch (loginError) {
      if (["auth/invalid-credential", "auth/user-not-found"].includes(loginError.code)) {
        await firebaseApi.createUserWithEmailAndPassword(auth, email, password);
      } else {
        throw loginError;
      }
    }
    authModal.hide();
    $("#authForm").reset();
  } catch (error) {
    console.error(error);
    showToast("Falha no acesso", firebaseErrorMessage(error));
  } finally {
    setLoading(btn, false, original);
  }
});

$("#btnLogout").addEventListener("click", async () => {
  await firebaseApi.signOut(auth);
  location.hash = "home";
});

$("#habitForm").addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!currentUser) return;
  const id = $("#habitId").value;
  const name = $("#habitName").value.trim();
  const color = $("#habitColor").value;
  if (!name) return;
  if (!id && habits.length >= MAX_FREE_HABITS) {
    showToast("Limite gratuito", "O MVP permite até 5 hábitos. Exclua um hábito para criar outro.");
    return;
  }
  const btn = $("#btnSaveHabit");
  const original = btn.innerHTML;
  setLoading(btn, true, original);
  try {
    if (id) {
      await firebaseApi.updateDoc(habitDocument(id), { name, color });
      showToast("Hábito atualizado", "As alterações foram salvas.");
    } else {
      await firebaseApi.addDoc(userHabitsCollection(), { name, color, createdAt: firebaseApi.serverTimestamp(), completedDates: [] });
      showToast("Hábito criado", "Agora é só marcar diariamente.");
    }
    habitModal.hide();
    resetHabitForm();
  } catch (error) {
    console.error(error);
    showToast("Erro", "Não foi possível salvar o hábito.");
  } finally {
    setLoading(btn, false, original);
  }
});

$("#btnCancelEdit").addEventListener("click", resetHabitForm);
$("#habitModal").addEventListener("hidden.bs.modal", resetHabitForm);

firebaseApi.onAuthStateChanged(auth, (user) => {
  currentUser = user;
  setAuthUi(user);
  if (user) {
    listenHabits();
  } else {
    if (unsubscribeHabits) unsubscribeHabits();
    habits = [];
    renderHabits();
  }
});

function firebaseErrorMessage(error) {
  const map = {
    "auth/email-already-in-use": "Este email já está em uso.",
    "auth/invalid-email": "Email inválido.",
    "auth/weak-password": "A senha precisa ter pelo menos 6 caracteres.",
    "auth/wrong-password": "Senha incorreta.",
    "auth/popup-closed-by-user": "Login cancelado antes da conclusão."
  };
  return map[error.code] || "Verifique os dados e tente novamente.";
}
