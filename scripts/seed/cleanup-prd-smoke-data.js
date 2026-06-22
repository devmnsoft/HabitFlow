#!/usr/bin/env node
import admin from 'firebase-admin';
import { requireSmokeEnabled } from './_prd-guard.js';
requireSmokeEnabled(); admin.initializeApp(); const db=admin.firestore(); const uid=process.env.PRD_SMOKE_TEST_UID; const smokeRunId=process.env.SMOKE_RUN_ID; if(!smokeRunId) throw new Error('Defina SMOKE_RUN_ID para limpeza precisa.');
async function clean(q){ const snap=await q.get(); await Promise.all(snap.docs.map(d=>d.ref.delete())); return snap.size; }
const total = await clean(db.collection('users').doc(uid).collection('habits').where('isSmokeTest','==',true).where('smokeRunId','==',smokeRunId)) + await clean(db.collection('supportTickets').where('isSmokeTest','==',true).where('smokeRunId','==',smokeRunId)) + await clean(db.collection('systemAuditLogs').where('isSmokeTest','==',true).where('smokeRunId','==',smokeRunId));
console.log(`Smoke data removida: ${total} documentos.`);
