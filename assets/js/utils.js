export const MAX_HABIT_NAME_LENGTH = 45;
export const $ = (selector) => document.querySelector(selector);
export const $$ = (selector) => Array.from(document.querySelectorAll(selector));
export function escapeHtml(value) { return String(value ?? "").replace(/[&<>'"]/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char])); }
export const toDateKey = (date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
export const todayKey = () => toDateKey(new Date());
export function getLastDays(total) { const formatter = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit" }); return Array.from({ length: total }, (_, index) => { const date = new Date(); date.setDate(date.getDate() - (total - 1 - index)); return { key: toDateKey(date), label: formatter.format(date), weekday: date.getDay() }; }); }
export function formatTimestamp(value) { const date = value?.toDate ? value.toDate() : value instanceof Date ? value : null; return date ? new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date) : "-"; }
export function getCurrentStreak(dates = []) { const set = new Set(dates); let streak = 0, cursor = new Date(); while (set.has(toDateKey(cursor))) { streak++; cursor.setDate(cursor.getDate() - 1); } return streak; }
export function getBestStreak(dates = []) { const sorted = [...new Set(dates)].sort(); let best = 0, current = 0, previous = null; for (const dateKey of sorted) { if (!previous) current = 1; else { const prevDate = new Date(previous); prevDate.setDate(prevDate.getDate() + 1); current = toDateKey(prevDate) === dateKey ? current + 1 : 1; } best = Math.max(best, current); previous = dateKey; } return best; }
export function completionRate(habits, days) { if (!habits.length) return 0; const keys = new Set(getLastDays(days).map(d => d.key)); const done = habits.reduce((sum, h) => sum + (h.completedDates || []).filter(d => keys.has(d)).length, 0); return Math.round((done / (habits.length * days)) * 100); }
