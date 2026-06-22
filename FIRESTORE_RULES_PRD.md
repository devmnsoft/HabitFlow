# Firestore Rules PRD

As regras atuais mantêm `users/{uid}` isolado por dono, bloqueiam escrita direta em billing, supportConversations, rateLimits, systemAuditLogs, adminAuditLogs, billingEvents, supportTickets, securityIncidents e systemSettings. Perfil não permite alteração direta de `plan`, `planStatus`, `role`, `isAdmin` e campos administrativos. Suporte, auditoria, billing e settings são operados via Firebase Functions.
