# Content Security Policy

A CSP está em `firebase.json` e permite Firebase, Google Auth, Bootstrap CDN, Google Fonts e endpoints de Firestore/Functions. Se o popup de Google Login quebrar, revise `script-src`, `frame-src` e `connect-src` mantendo `frame-ancestors 'none'` e `object-src 'none'`.
