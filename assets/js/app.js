import { auth, db, googleProvider, firebaseApi } from "./firebase.js";

const APP_VERSION = "1.4";
const PAYMENT_PROVIDER = "future";
const PREMIUM_MONTHLY_PRICE = 14.90;
const PREMIUM_YEARLY_PRICE = 99.00;
const ADMIN_EMAILS = [""];
const PLAN_LIMITS = { free: 5, premium: Infinity };
const MAX_HABIT_NAME_LENGTH = 45;
const QUICK_HABITS = ["Beber água", "Ler 10 minutos", "Caminhar", "Meditar", "Estudar"];
const BENEFITS = [
  ["bi-mouse", "Simples de usar", "Crie, marque e acompanhe hábitos sem telas confusas."],
  ["bi-bullseye", "Foco em consistência", "Streaks e mensagens motivam pequenas ações diárias."],
  ["bi-bar-chart", "Progresso visual", "Histórico dos últimos 30 dias para enxergar evolução."],
  ["bi-phone", "Feito para celular", "Layout mobile first e PWA instalável."],
  ["bi-bell-slash", "Sem distrações", "Apenas o essencial para sua rotina."],
  ["bi-gift", "Gratuito para começar", "Até 5 hábitos no plano grátis."],
];

let currentUser = null;
let currentProfile = null;
let currentPlan = "free";
let habits = [];
let usageEventsCount = 0;
let unsubscribeHabits = null;

const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => Array.from(document.querySelectorAll(selector));
const authModal = new bootstrap.Modal($("#authModal"));
const habitModal = new bootstrap.Modal($("#habitModal"));
const confirmDeleteModal = new bootstrap.Modal($("#confirmDeleteModal"));
const toast = new bootstrap.Toast($("#appToast"));
let pendingDeleteId = null;
let deferredInstallPrompt = null;

function userPath(...parts) { return ["users", currentUser.uid, ...parts]; }
function profileDoc(userId = currentUser.uid) { return firebaseApi.doc(db, "users", userId, "profile", "main"); }
function habitsCollection() { return firebaseApi.collection(db, ...userPath("habits")); }
function habitDocument(id) { return firebaseApi.doc(db, ...userPath("habits", id)); }
function usageCollection() { return firebaseApi.collection(db, ...userPath("usage", "events")); }

const toDateKey = (date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
const todayKey = () => toDateKey(new Date());
const totalCompletions = () => habits.reduce((sum, h) => sum + (h.completedDates || []).length, 0);

function handleAppError(error, friendlyMessage = "Não foi possível concluir a ação agora.") {
  console.error("[HabitFlow]", error);
  showToast("Ops", friendlyMessage, "danger");
}

function showToast(title, message, variant = "success") {
  $("#toastTitle").textContent = title;
  $("#toastBody").textContent = message;
  $("#appToast").className = `toast rounded-4 border-0 toast-${variant}`;
  toast.show();
}
function setLoading(button, isLoading, originalText) {
  if (!button) return;
  button.disabled = isLoading;
  button.innerHTML = isLoading ? '<span class="spinner-border spinner-border-sm me-2"></span>Aguarde...' : originalText;
}
function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char])); }
function formatTimestamp(value) {
  const date = value?.toDate ? value.toDate() : null;
  return date ? new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date) : "-";
}

function renderMarketing() {
  $("#benefitsGrid").innerHTML = BENEFITS.map(([icon, title, text]) => `<div class="col-md-6 col-lg-4"><div class="feature-card h-100"><i class="bi ${icon}"></i><h5>${title}</h5><p>${text}</p></div></div>`).join("");
  $$(".plans-public").forEach(el => el.innerHTML = planCardsHtml(false));
}
function planCardsHtml(authenticated) {
  return `<div class="col-lg-6"><div class="plan-card h-100"><span class="badge text-bg-success rounded-pill">Atual</span><h3>Plano Gratuito</h3><div class="price-line"><strong>R$ 0</strong></div><ul><li>Até 5 hábitos</li><li>Histórico de 30 dias</li><li>Streak básico</li><li>Categorias</li><li>PWA instalável</li></ul></div></div><div class="col-lg-6"><div class="plan-card plan-premium h-100"><span class="badge text-bg-dark rounded-pill">Em breve</span><h3>Plano Premium</h3><div class="price-line"><strong>R$ 14,90</strong><span>/mês</span></div><ul><li>Hábitos ilimitados</li><li>Histórico completo</li><li>Relatórios avançados</li><li>Desafios de 30 e 90 dias</li><li>Exportação futura</li><li>Temas</li></ul><button class="btn btn-success rounded-pill btn-premium-interest" type="button">Quero ser avisado</button></div></div>`;
}

async function ensureUserProfile(user) {
  const ref = profileDoc(user.uid);
  const snap = await firebaseApi.getDoc(ref);
  const base = { name: user.displayName || user.email || "Usuário", email: user.email || "", plan: "free", lastLoginAt: firebaseApi.serverTimestamp(), appVersion: APP_VERSION };
  if (snap.exists()) {
    await firebaseApi.setDoc(ref, base, { merge: true });
  } else {
    await firebaseApi.setDoc(ref, { ...base, createdAt: firebaseApi.serverTimestamp(), wantsPremiumNotice: false }, { merge: true });
  }
  const updated = await firebaseApi.getDoc(ref);
  currentProfile = updated.data() || {};
  currentPlan = currentProfile.plan || "free";
}
async function getUserPlan(userId) {
  const snap = await firebaseApi.getDoc(profileDoc(userId));
  return snap.exists() && snap.data().plan ? snap.data().plan : "free";
}
async function trackEvent(type, metadata = {}) {
  if (!currentUser) return;
  try {
    await firebaseApi.addDoc(usageCollection(), { type, createdAt: firebaseApi.serverTimestamp(), metadata });
    usageEventsCount += 1;
    renderAdmin();
  } catch (error) {
    handleAppError(error, "Não foi possível registrar o evento de uso.");
  }
}
function isAdminUser(user) { return Boolean(user?.email && ADMIN_EMAILS.includes(user.email)); }

function setAuthUi(user) {
  const logged = Boolean(user);
  $$(".public-only").forEach(el => el.classList.toggle("d-none", logged));
  $$(".auth-only").forEach(el => el.classList.toggle("d-none", !logged));
  $("#adminTabItem").classList.toggle("d-none", !isAdminUser(user));
  if (logged) {
    $("#userGreeting").textContent = `Olá, ${user.displayName || user.email || "usuário"}. Acompanhe sua consistência de hoje.`;
    location.hash = "dashboard";
  }
}

function listenHabits() {
  if (unsubscribeHabits) unsubscribeHabits();
  $("#loadingState").classList.remove("d-none");
  const q = firebaseApi.query(habitsCollection(), firebaseApi.orderBy("createdAt", "desc"));
  unsubscribeHabits = firebaseApi.onSnapshot(q, (snapshot) => { habits = snapshot.docs.map(d => ({ id: d.id, ...d.data() })); renderAll(); }, (error) => handleAppError(error, "Não foi possível carregar seus hábitos agora."));
}
async function loadUsageEventsCount() {
  try {
    const snap = await firebaseApi.getDocs(firebaseApi.query(usageCollection(), firebaseApi.limit(200)));
    usageEventsCount = snap.size;
    renderAdmin();
  } catch (error) {
    handleAppError(error, "Não foi possível carregar eventos administrativos.");
  }
}

function renderAll() { renderHabits(); renderProgress(); renderProfile(); renderAdmin(); }
function renderHabits() {
  $("#loadingState").classList.add("d-none");
  const limit = PLAN_LIMITS[currentPlan] ?? PLAN_LIMITS.free;
  $("#emptyState").classList.toggle("d-none", habits.length > 0);
  $("#limitAlert").classList.toggle("d-none", !(currentPlan === "free" && habits.length >= limit));
  const today = todayKey();
  const doneToday = habits.filter(h => (h.completedDates || []).includes(today)).length;
  const best = habits.reduce((max, h) => Math.max(max, getBestStreak(h.completedDates || [])), 0);
  $("#kpiTotal").textContent = habits.length; $("#kpiDoneToday").textContent = doneToday; $("#kpiBestStreak").textContent = best; $("#kpiCompletion").textContent = `${habits.length ? Math.round((doneToday / habits.length) * 100) : 0}%`;
  renderQuickHabits();
  $("#habitsList").innerHTML = habits.map(habitCardHtml).join("");
  bindHabitCardEvents();
}
function habitCardHtml(habit) {
  const completedDates = habit.completedDates || [], done = completedDates.includes(todayKey());
  const history = getLastDays(30).map(day => `<span class="history-day ${completedDates.includes(day.key) ? "done" : ""} ${day.key === todayKey() ? "today" : ""}" title="${day.label}: ${completedDates.includes(day.key) ? "feito" : "pendente"}" aria-label="${day.label}: ${completedDates.includes(day.key) ? "feito" : "pendente"}"></span>`).join("");
  return `<article class="habit-card" style="--habit-color:${escapeHtml(habit.color || "#10B981")}"><div class="habit-color-bar"></div><div class="habit-title-row"><div><h2 class="habit-title">${escapeHtml(habit.name || "Hábito")}</h2><div class="habit-meta">${escapeHtml(habit.category || "Outro")} • ${done ? "Concluído hoje" : "Pendente hoje"}</div></div><div class="habit-actions"><button class="icon-btn btn-edit" data-id="${habit.id}" title="Editar hábito" aria-label="Editar ${escapeHtml(habit.name || "hábito")}"><i class="bi bi-pencil"></i></button><button class="icon-btn btn-delete" data-id="${habit.id}" title="Excluir hábito" aria-label="Excluir ${escapeHtml(habit.name || "hábito")}"><i class="bi bi-trash"></i></button></div></div><button class="check-btn ${done ? "done" : ""}" data-id="${habit.id}" aria-label="${done ? "Remover conclusão" : "Marcar como feito"} ${escapeHtml(habit.name || "hábito")}"><i class="bi ${done ? "bi-check-circle-fill" : "bi-circle"} me-2"></i>${done ? "Feito hoje" : "Marcar como feito"}</button><div class="habit-stats"><span class="stat-pill"><i class="bi bi-fire me-1"></i>Streak atual: ${getCurrentStreak(completedDates)}</span><span class="stat-pill"><i class="bi bi-trophy me-1"></i>Maior: ${getBestStreak(completedDates)}</span><span class="stat-pill"><i class="bi bi-calendar-check me-1"></i>Total: ${completedDates.length}</span></div><div class="history-grid">${history}</div></article>`;
}
function bindHabitCardEvents() { $$(".check-btn").forEach(b => b.addEventListener("click", () => toggleToday(b.dataset.id))); $$(".btn-edit").forEach(b => b.addEventListener("click", () => openEdit(b.dataset.id))); $$(".btn-delete").forEach(b => b.addEventListener("click", () => deleteHabit(b.dataset.id))); }
function renderQuickHabits() { $("#quickHabits").innerHTML = QUICK_HABITS.map(name => `<button class="btn btn-outline-success rounded-pill quick-habit-btn" type="button" data-name="${escapeHtml(name)}"><i class="bi bi-plus-lg me-1"></i>${escapeHtml(name)}</button>`).join(""); $$(".quick-habit-btn").forEach(b => b.addEventListener("click", () => createHabit({ name: b.dataset.name, category: "Bem-estar" }))); }

function renderProgress() {
  const today = todayKey(), doneToday = habits.filter(h => (h.completedDates || []).includes(today)).length;
  const messages = [];
  if (!habits.length) messages.push("Comece com um hábito simples hoje."); else if (doneToday === habits.length) messages.push("Excelente! Você concluiu todos os hábitos de hoje."); else if (doneToday > 0) messages.push("Boa! Você já deu um passo hoje."); else messages.push("Ainda dá tempo de marcar seu primeiro hábito hoje.");
  if (habits.some(h => getCurrentStreak(h.completedDates || []) >= 7)) messages.push("Você está criando uma sequência forte. Continue assim.");
  $("#insightsList").innerHTML = habits.length ? messages.map(m => `<div class="insight-item"><i class="bi bi-lightbulb"></i><span>${m}</span></div>`).join("") : emptyStateHtml("bi-graph-up-arrow", "Progresso sem dados", "Crie e conclua hábitos para receber insights pessoais.", "Criar primeiro hábito");
  const ranking = [...habits].sort((a, b) => getCurrentStreak(b.completedDates || []) - getCurrentStreak(a.completedDates || []) || (b.completedDates || []).length - (a.completedDates || []).length);
  $("#rankingList").innerHTML = ranking.length ? ranking.map((h, i) => `<div class="ranking-item"><b>#${i + 1}</b><div><strong>${escapeHtml(h.name || "Hábito")}</strong><span>${escapeHtml(h.category || "Outro")} • ${ (h.completedDates || []).length } conclusões • streak ${getCurrentStreak(h.completedDates || [])} • maior ${getBestStreak(h.completedDates || [])}</span></div></div>`).join("") : emptyStateHtml("bi-trophy", "Ranking vazio", "Seu ranking aparece quando você criar hábitos.", "Criar primeiro hábito");
  bindEmptyStateActions();
}
function renderProfile() {
  $("#profilePlans").innerHTML = `<div class="row g-3">${planCardsHtml(true)}</div>`;
  bindPremiumButtons();
  $("#profileName").textContent = currentProfile?.name || currentUser?.displayName || "-"; $("#profileEmail").textContent = currentProfile?.email || currentUser?.email || "-"; $("#profilePlan").textContent = currentPlan === "premium" ? "Premium" : "Gratuito"; $("#profileCreatedAt").textContent = formatTimestamp(currentProfile?.createdAt); $("#profileLastLoginAt").textContent = formatTimestamp(currentProfile?.lastLoginAt); $("#profileTotalHabits").textContent = habits.length; $("#profileTotalCompletions").textContent = totalCompletions();
}
function renderAdmin() {
  $("#adminHabits").textContent = habits.length;
  $("#adminEvents").textContent = usageEventsCount;
  $("#adminPlan").textContent = currentPlan;
  $("#adminPremium").textContent = currentProfile?.wantsPremiumNotice ? "Sim" : "Não";
  const existing = $("#adminEmptyState");
  if (existing) existing.remove();
  const adminPanel = $("#tabAdmin .panel-card");
  if (adminPanel && usageEventsCount === 0) adminPanel.insertAdjacentHTML("beforeend", `<div id="adminEmptyState" class="mt-3">${emptyStateHtml("bi-clipboard-data", "Nenhum evento ainda", "Os eventos de uso aparecerão aqui após interações no app.", "Criar hábito")}</div>`);
}
function emptyStateHtml(icon, title, text, action) { return `<div class="empty-state empty-state-compact"><i class="bi ${icon}"></i><h4>${title}</h4><p>${text}</p><button class="btn btn-success rounded-pill px-4 btn-empty-create" type="button" data-bs-toggle="modal" data-bs-target="#habitModal">${action}</button></div>`; }
function bindEmptyStateActions() { $$(".btn-empty-create").forEach(btn => btn.addEventListener("click", () => habitModal.show())); }

async function createHabit(data) {
  try {
    if (!currentUser) return;
    if ((await getUserPlan(currentUser.uid)) === "free" && habits.length >= PLAN_LIMITS.free) return showToast("Limite gratuito", "Você atingiu 5 hábitos no plano gratuito.", "warning");
    await firebaseApi.addDoc(habitsCollection(), { name: data.name, category: data.category || "Outro", color: data.color || "#10B981", createdAt: firebaseApi.serverTimestamp(), completedDates: [] });
    await trackEvent("habit_created", { category: data.category || "Outro" });
    showToast("Hábito criado", "Agora é só marcar diariamente.");
  } catch (error) { handleAppError(error, "Não foi possível criar o hábito."); }
}
async function toggleToday(id) {
  try {
    const habit = habits.find(h => h.id === id); if (!habit) return;
    const set = new Set(habit.completedDates || []), wasDone = set.has(todayKey()); wasDone ? set.delete(todayKey()) : set.add(todayKey());
    await firebaseApi.updateDoc(habitDocument(id), { completedDates: Array.from(set).sort() });
    await trackEvent(wasDone ? "habit_uncompleted" : "habit_completed", { habitId: id });
    showToast("Progresso atualizado", wasDone ? "Conclusão de hoje removida." : "Parabéns! Hábito marcado como feito hoje.");
  } catch (error) { handleAppError(error, "Não foi possível atualizar o progresso."); }
}
function openEdit(id) { const h = habits.find(x => x.id === id); if (!h) return; $("#habitModalLabel").textContent = "Editar hábito"; $("#habitId").value = h.id; $("#habitName").value = h.name || ""; $("#habitCategory").value = h.category || "Outro"; $("#habitColor").value = h.color || "#10B981"; $("#btnSaveHabit").textContent = "Salvar alterações"; $("#btnCancelEdit").classList.remove("d-none"); habitModal.show(); }
function resetHabitForm() { $("#habitModalLabel").textContent = "Novo hábito"; $("#habitId").value = ""; $("#habitForm").reset(); $("#habitColor").value = "#10B981"; $("#btnSaveHabit").textContent = "Salvar hábito"; $("#btnCancelEdit").classList.add("d-none"); }
function deleteHabit(id) { pendingDeleteId = id; confirmDeleteModal.show(); }
async function confirmDeleteHabit() { try { if (!pendingDeleteId) return; const id = pendingDeleteId; await firebaseApi.deleteDoc(habitDocument(id)); await trackEvent("habit_deleted", { habitId: id }); showToast("Hábito excluído", "O hábito foi removido com sucesso."); } catch (error) { handleAppError(error, "Não foi possível excluir o hábito."); } finally { pendingDeleteId = null; confirmDeleteModal.hide(); } }
async function startPremiumCheckout() {
  try {
    await trackEvent("premium_checkout_clicked", { provider: PAYMENT_PROVIDER, monthly: PREMIUM_MONTHLY_PRICE, yearly: PREMIUM_YEARLY_PRICE });
    if (currentUser) await firebaseApi.setDoc(profileDoc(), { wantsPremiumNotice: true }, { merge: true });
    currentProfile = { ...currentProfile, wantsPremiumNotice: true };
    showToast("Premium", "O Premium ainda não está disponível. Vamos te avisar assim que for lançado.");
    renderProfile();
  } catch (error) { handleAppError(error, "Não foi possível registrar seu interesse no Premium."); }
}
function bindPremiumButtons() { $$(".btn-premium-interest").forEach(btn => btn.addEventListener("click", () => currentUser ? startPremiumCheckout() : showToast("Premium em breve", "Crie sua conta grátis para registrar interesse no Premium."))); }

function getLastDays(total) { const formatter = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit" }); return Array.from({ length: total }, (_, index) => { const date = new Date(); date.setDate(date.getDate() - (total - 1 - index)); return { key: toDateKey(date), label: formatter.format(date) }; }); }
function getCurrentStreak(dates) { const set = new Set(dates); let streak = 0, cursor = new Date(); while (set.has(toDateKey(cursor))) { streak++; cursor.setDate(cursor.getDate() - 1); } return streak; }
function getBestStreak(dates) { const sorted = [...new Set(dates)].sort(); let best = 0, current = 0, previous = null; for (const dateKey of sorted) { if (!previous) current = 1; else { const prevDate = new Date(previous); prevDate.setDate(prevDate.getDate() + 1); current = toDateKey(prevDate) === dateKey ? current + 1 : 1; } best = Math.max(best, current); previous = dateKey; } return best; }

async function signInWithGoogle(button) { const original = button.innerHTML; setLoading(button, true, original); try { await firebaseApi.signInWithPopup(auth, googleProvider); authModal.hide(); } catch (error) { handleAppError(error, "Não foi possível entrar com Google. Verifique se o provedor está habilitado no Firebase."); } finally { setLoading(button, false, original); } }
$("#btnGoogle").addEventListener("click", () => signInWithGoogle($("#btnGoogle")));
$("#btnHeroGoogle").addEventListener("click", () => signInWithGoogle($("#btnHeroGoogle")));
$("#authForm").addEventListener("submit", async (event) => { event.preventDefault(); const btn = $("#btnEmailLogin"), original = btn.innerHTML; setLoading(btn, true, original); try { try { await firebaseApi.signInWithEmailAndPassword(auth, $("#authEmail").value.trim(), $("#authPassword").value); } catch (e) { if (["auth/invalid-credential", "auth/user-not-found"].includes(e.code)) await firebaseApi.createUserWithEmailAndPassword(auth, $("#authEmail").value.trim(), $("#authPassword").value); else throw e; } authModal.hide(); $("#authForm").reset(); } catch (e) { handleAppError(e, firebaseErrorMessage(e)); } finally { setLoading(btn, false, original); } });
async function logout() { try { await firebaseApi.signOut(auth); location.hash = "home"; } catch (error) { handleAppError(error, "Não foi possível sair da conta."); } }
$("#btnLogout").addEventListener("click", logout); $("#btnLogoutProfile").addEventListener("click", logout);
$("#habitForm").addEventListener("submit", async (event) => { event.preventDefault(); if (!currentUser) return; const id = $("#habitId").value, name = $("#habitName").value.trim(), category = $("#habitCategory").value, color = $("#habitColor").value; if (!name) return showToast("Nome obrigatório", "Informe um nome para o hábito.", "warning"); if (name.length > MAX_HABIT_NAME_LENGTH) return showToast("Nome muito longo", `Use no máximo ${MAX_HABIT_NAME_LENGTH} caracteres.`, "warning"); const btn = $("#btnSaveHabit"), original = btn.innerHTML; setLoading(btn, true, original); try { if (id) { await firebaseApi.updateDoc(habitDocument(id), { name, category, color }); showToast("Hábito atualizado", "As alterações foram salvas."); } else await createHabit({ name, category, color }); habitModal.hide(); resetHabitForm(); } catch (error) { handleAppError(error, "Não foi possível salvar o hábito."); } finally { setLoading(btn, false, original); } });
$("#btnCancelEdit").addEventListener("click", resetHabitForm); $("#habitModal").addEventListener("hidden.bs.modal", resetHabitForm);

firebaseApi.onAuthStateChanged(auth, async (user) => {
  currentUser = user; setAuthUi(user);
  if (user) { try { await ensureUserProfile(user); currentPlan = await getUserPlan(user.uid); await trackEvent("login"); listenHabits(); loadUsageEventsCount(); } catch (error) { handleAppError(error, "Não foi possível atualizar seu perfil."); } }
  else { if (unsubscribeHabits) unsubscribeHabits(); currentProfile = null; currentPlan = "free"; habits = []; renderAll(); }
});
function firebaseErrorMessage(error) { return ({ "auth/email-already-in-use": "Este email já está em uso.", "auth/invalid-email": "Email inválido.", "auth/weak-password": "A senha precisa ter pelo menos 6 caracteres.", "auth/wrong-password": "Senha incorreta.", "auth/popup-closed-by-user": "Login cancelado antes da conclusão." }[error.code] || "Verifique os dados e tente novamente."); }

renderMarketing(); bindPremiumButtons();

$("#btnConfirmDelete").addEventListener("click", confirmDeleteHabit);
window.addEventListener("beforeinstallprompt", (event) => { event.preventDefault(); deferredInstallPrompt = event; $("#installCard")?.classList.remove("d-none"); });
$("#btnInstallApp")?.addEventListener("click", async () => { if (!deferredInstallPrompt) return; deferredInstallPrompt.prompt(); await deferredInstallPrompt.userChoice; deferredInstallPrompt = null; $("#installCard")?.classList.add("d-none"); });
window.addEventListener("appinstalled", () => { deferredInstallPrompt = null; $("#installCard")?.classList.add("d-none"); });
