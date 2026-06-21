import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getAnalytics, isSupported } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-analytics.js";
import {
  getAuth,
  GoogleAuthProvider,
  signInWithPopup,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  onAuthStateChanged,
  signOut
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";
import { getFunctions, httpsCallable } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-functions.js";
import { setupAppCheck } from "./app-check.js";
import {
  getFirestore,
  collection,
  addDoc,
  updateDoc,
  deleteDoc,
  doc,
  getDoc,
  setDoc,
  onSnapshot,
  getDocs,
  serverTimestamp,
  query,
  orderBy,
  limit
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";

// A configuração Firebase do client identifica o projeto, mas não deve conceder acesso sem Rules e App Check.
// Tokens administrativos, Telegram, IA, pagamentos e credenciais do Admin SDK nunca devem ficar no frontend.
const firebaseConfig = {
  apiKey: "AIzaSyAyhjZTwJulgXM_Qpxq7KfKSgl9-m04fmY",
  authDomain: "habitflow-5f945.firebaseapp.com",
  projectId: "habitflow-5f945",
  storageBucket: "habitflow-5f945.firebasestorage.app",
  messagingSenderId: "73871121741",
  appId: "1:73871121741:web:4caf0dd1d2445ccc58eb04",
  measurementId: "G-DCYPXDMCGX"
};

export const app = initializeApp(firebaseConfig);
export const appCheck = setupAppCheck(app);
export const auth = getAuth(app);
export const db = getFirestore(app);
export const functions = getFunctions(app);
export const googleProvider = new GoogleAuthProvider();

isSupported().then((supported) => {
  if (supported) getAnalytics(app);
}).catch(() => {});

export const firebaseApi = {
  signInWithPopup,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  onAuthStateChanged,
  signOut,
  collection,
  addDoc,
  updateDoc,
  deleteDoc,
  doc,
  getDoc,
  setDoc,
  onSnapshot,
  getDocs,
  serverTimestamp,
  query,
  orderBy,
  limit,
  httpsCallable,
  functions
};
