const DEFAULT_ALLOWED_ORIGINS = [
  "http://localhost:5177",
  "http://127.0.0.1:5177",
  "https://habitflow-5f945.web.app",
  "https://habitflow-5f945.firebaseapp.com"
];

function getAllowedOrigins() {
  const extra = process.env.APP_ALLOWED_ORIGINS
    ? process.env.APP_ALLOWED_ORIGINS.split(",").map((x) => x.trim()).filter(Boolean)
    : [];

  return [...new Set([...DEFAULT_ALLOWED_ORIGINS, ...extra])];
}

function applyCors(req, res) {
  const origin = req.headers.origin;
  const allowedOrigins = getAllowedOrigins();

  if (origin && allowedOrigins.includes(origin)) {
    res.set("Access-Control-Allow-Origin", origin);
  }

  res.set("Vary", "Origin");
  res.set("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  res.set("Access-Control-Allow-Headers", "Content-Type,Authorization");
  res.set("Access-Control-Max-Age", "3600");

  if (req.method === "OPTIONS") {
    res.status(204).send("");
    return true;
  }

  return false;
}

module.exports = { applyCors, getAllowedOrigins };
