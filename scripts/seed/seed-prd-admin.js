#!/usr/bin/env node
import admin from 'firebase-admin';
import { requirePrdConfirmation } from './_prd-guard.js';
requirePrdConfirmation(); admin.initializeApp();
await admin.firestore().doc('adminConfig/general').set({ environment:'production', adminSource:'functions_env_ADMIN_EMAILS_or_future_custom_claims', secretsStored:false, updatedAt:admin.firestore.FieldValue.serverTimestamp() }, {merge:true});
console.log('adminConfig/general público operacional atualizado sem secrets.');
