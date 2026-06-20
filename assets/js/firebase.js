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
export const auth = getAuth(app);
export const db = getFirestore(app);
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
  limit
};
