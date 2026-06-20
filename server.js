import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, normalize } from "node:path";

const PORT = 5177;
const ROOT = process.cwd();

const mimeTypes = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml; charset=utf-8",
  ".ico": "image/x-icon"
};

function safePath(urlPath) {
  const pathname = decodeURIComponent(new URL(urlPath, `http://localhost:${PORT}`).pathname);
  const requested = pathname === "/" ? "/index.html" : pathname;
  const fullPath = normalize(join(ROOT, requested));
  return fullPath.startsWith(ROOT) ? fullPath : join(ROOT, "index.html");
}

createServer(async (request, response) => {
  try {
    const filePath = safePath(request.url || "/");
    const content = await readFile(filePath);
    response.writeHead(200, { "Content-Type": mimeTypes[extname(filePath)] || "application/octet-stream" });
    response.end(content);
  } catch {
    const fallback = await readFile(join(ROOT, "index.html"));
    response.writeHead(200, { "Content-Type": mimeTypes[".html"] });
    response.end(fallback);
  }
}).listen(PORT, () => {
  console.log(`HabitFlow rodando em http://localhost:${PORT}`);
});
