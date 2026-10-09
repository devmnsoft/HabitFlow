(() => {
  'use strict';

  const STORAGE_KEY = 'habitflow_offline_queue_v6250';
  let isSyncing = false;

  function getQueue() {
    try {
      return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
    } catch {
      return [];
    }
  }

  function saveQueue(queue) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(queue));
      updateIndicator();
    } catch (e) {
      console.warn('[OfflineSync] Falha ao gravar fila local:', e);
    }
  }

  function clearQueue() {
    localStorage.removeItem(STORAGE_KEY);
    updateIndicator();
  }

  function generateUuid() {
    if (crypto.randomUUID) return crypto.randomUUID();
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0;
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  function enqueue(actionType, entityId, payloadObj) {
    const queue = getQueue();
    const item = {
      id: generateUuid(),
      actionType,
      entityId: entityId || null,
      payloadJson: payloadObj ? JSON.stringify(payloadObj) : null,
      clientCreatedAt: new Date().toISOString(),
      status: 'Pending'
    };
    queue.push(item);
    saveQueue(queue);

    notifyFeedback('Ação salva localmente. Será sincronizada assim que a conexão retornar.', 'info');
    if (navigator.onLine) {
      syncNow();
    }
    return item;
  }

  async function syncNow() {
    if (isSyncing || !navigator.onLine) return;
    const queue = getQueue().filter(i => i.status === 'Pending' || i.status === 'Failed');
    if (queue.length === 0) return;

    isSyncing = true;
    updateIndicator(true);

    try {
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
      const response = await fetch('/offline/sync', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': token
        },
        body: JSON.stringify({ actions: queue })
      });

      if (!response.ok) {
        throw new Error('Servidor retornou erro ' + response.status);
      }

      const data = await response.json();
      const currentQueue = getQueue();

      // Atualiza itens na fila
      const resolvedIds = new Set();
      (data.results || []).forEach(res => {
        if (res.status === 'Synced') {
          resolvedIds.add(res.id);
        } else if (res.status === 'Conflict') {
          notifyFeedback('Houve um conflito em uma ação offline. A tela foi atualizada para o estado mais recente.', 'warning');
          resolvedIds.add(res.id);
        }
      });

      const remaining = currentQueue.filter(item => !resolvedIds.has(item.id));
      saveQueue(remaining);

      if (data.succeeded > 0) {
        notifyFeedback(`${data.succeeded} ação(ões) sincronizada(s) com sucesso!`, 'success');
      }
    } catch (err) {
      console.warn('[OfflineSync] Erro na sincronização:', err);
    } finally {
      isSyncing = false;
      updateIndicator();
    }
  }

  function notifyFeedback(message, type) {
    if (window.HabitFlowFeedback && typeof window.HabitFlowFeedback.toast === 'function') {
      window.HabitFlowFeedback.toast({ message, type });
    } else {
      console.info(`[HabitFlow Feedback] [${type}]: ${message}`);
    }
  }

  function updateIndicator(syncing = false) {
    let bar = document.getElementById('hf-offline-status-bar');
    if (!bar) {
      bar = document.createElement('aside');
      bar.id = 'hf-offline-status-bar';
      bar.className = 'hf-offline-bar';
      bar.setAttribute('role', 'status');
      bar.setAttribute('aria-live', 'polite');
      document.body.prepend(bar);
    }

    const pending = getQueue().length;
    if (!navigator.onLine) {
      bar.hidden = false;
      bar.className = 'hf-offline-bar is-offline';
      bar.innerHTML = `<span class="hf-offline-dot"></span> <span>Modo offline ativo.${pending > 0 ? ` (${pending} pendente(s))` : ''}</span>`;
    } else if (syncing) {
      bar.hidden = false;
      bar.className = 'hf-offline-bar is-syncing';
      bar.innerHTML = `<span>Sincronizando ${pending} ação(ões)...</span>`;
    } else if (pending > 0) {
      bar.hidden = false;
      bar.className = 'hf-offline-bar is-pending';
      bar.innerHTML = `<span>${pending} ação(ões) pendente(s). <button type="button" onclick="window.HabitFlowOffline.syncNow()">Sincronizar agora</button></span>`;
    } else {
      bar.hidden = true;
    }
  }

  // Intercepta cliques de checkin quando offline
  document.addEventListener('click', e => {
    const btn = e.target.closest('[data-offline-checkin]');
    if (!btn) return;

    if (!navigator.onLine) {
      e.preventDefault();
      e.stopPropagation();
      const habitId = btn.getAttribute('data-habit-id');
      if (!habitId) return;

      btn.classList.toggle('is-completed-offline');
      enqueue('complete_habit', habitId, { date: new Date().toISOString().slice(0, 10) });
    }
  }, true);

  window.addEventListener('online', () => {
    notifyFeedback('Conexão restabelecida! Iniciando sincronização...', 'info');
    updateIndicator();
    syncNow();
  });

  window.addEventListener('offline', () => {
    notifyFeedback('Você está offline. Os check-ins serão guardados com segurança.', 'warning');
    updateIndicator();
  });

  // Limpeza de cache no logout
  document.querySelectorAll('form[action*="/logout"], a[href*="/logout"]').forEach(el => {
    el.addEventListener('click', () => {
      clearQueue();
      if ('caches' in window) {
        caches.keys().then(keys => keys.forEach(k => {
          if (!k.includes('public')) caches.delete(k);
        }));
      }
    });
  });

  window.HabitFlowOffline = {
    enqueue,
    syncNow,
    clearQueue,
    getQueue
  };

  document.addEventListener('DOMContentLoaded', () => {
    updateIndicator();
    if (navigator.onLine) syncNow();
  });
})();
