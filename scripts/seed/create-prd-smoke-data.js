#!/usr/bin/env node
import admin from 'firebase-admin';
import { requireSmokeEnabled, getRunId } from './_prd-guard.js';
requireSmokeEnabled(); admin.initializeApp(); const db=admin.firestore(); const uid=process.env.PRD_SMOKE_TEST_UID; const smokeRunId=getRunId(); const base={isSmokeTest:true,createdBySmokeTest:true,smokeRunId,environment:'production',createdAt:admin.firestore.FieldValue.serverTimestamp()};
for (const name of ['[SMOKE] Beber água','[SMOKE] Ler 10 minutos','[SMOKE] Caminhar']) await db.collection('users').doc(uid).collection('habits').add({...base,name,category:'Smoke',color:'#10B981',completedDates:[]});
await db.collection('supportTickets').doc(`SMOKE-${smokeRunId}`).set({...base,protocol:`SMOKE-${smokeRunId}`,userId:uid,type:'support',status:'open',priority:'low',title:'[SMOKE] Ticket de teste de suporte',description:'[SMOKE] Validação controlada PRD'});
await db.collection('systemAuditLogs').add({...base,type:'smoke_logger_event',severity:'info',source:'backend',action:'smoke_test',message:'[SMOKE] Evento de teste logger'});
console.log(`Smoke data criada para UID ${uid}; smokeRunId=${smokeRunId}`);
