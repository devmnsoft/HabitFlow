#!/usr/bin/env node
import admin from 'firebase-admin';
import { requirePrdConfirmation } from './_prd-guard.js';
requirePrdConfirmation();
admin.initializeApp();
await admin.firestore().doc('systemSettings/public').set({
  companyName: 'MNSOFT', companyLegalName: 'MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA', companyCnpj: '18.160.057/0001-13', commercialEmail: 'comercial@mnsoft.com.br', supportEmail: 'comercial@mnsoft.com.br', whatsappEnabled: false, whatsappNumber: '', whatsappDefaultMessage: 'Olá, vim pelo HabitFlow e gostaria de falar com a equipe da MNSOFT.', whatsappButtonText: 'Falar com a MNSOFT', environment: 'production', updatedAt: admin.firestore.FieldValue.serverTimestamp()
}, { merge: true });
console.log('systemSettings/public PRD atualizado.');
