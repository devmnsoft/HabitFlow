#!/usr/bin/env node
import admin from 'firebase-admin';
import { requirePrdConfirmation } from './_prd-guard.js';
requirePrdConfirmation(); admin.initializeApp(); const db=admin.firestore(); const now=admin.firestore.FieldValue.serverTimestamp();
await db.doc('appConfig/plans').set({ free:{id:'free',name:'Gratuito',habitLimit:5,priceMonthly:0,active:true}, premium_monthly:{id:'premium_monthly',name:'Premium Mensal',priceMonthly:14.90,active:false,status:'coming_soon'}, premium_yearly:{id:'premium_yearly',name:'Premium Anual',priceYearly:99.00,active:false,status:'coming_soon'}, updatedAt:now }, {merge:true});
await db.doc('appConfig/version').set({version:'v2.4-prd',environment:'production',updatedAt:now},{merge:true});
console.log('appConfig/plans e appConfig/version atualizados.');
