import { readdir, readFile, writeFile } from "node:fs/promises";
import { join } from "node:path";

async function walk(dir) {
  const out = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...await walk(full));
    else out.push(full);
  }
  return out;
}

function lightObfuscate(source) {
  // Camada leve e segura: Vite/Terser já minifica/mangle; aqui removemos comentários residuais e sourceMappingURL.
  return source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/#[#]?\s*sourceMappingURL=.*/g, "");
}

const files = (await walk("dist")).filter((file) => file.endsWith(".js"));
for (const file of files) {
  const source = await readFile(file, "utf8");
  await writeFile(file, lightObfuscate(source));
}
console.log(`Hardening pós-build aplicado em ${files.length} bundle(s) JS.`);
