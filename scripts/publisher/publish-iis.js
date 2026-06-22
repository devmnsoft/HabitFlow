#!/usr/bin/env node
import { copyFileSync, cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync, spawnSync } from 'node:child_process';
import { createInterface } from 'node:readline/promises';
import { stdin as input, stdout as output } from 'node:process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = resolve(__dirname, '../..');
const configPath = join(__dirname, 'publisher.config.json');
const exampleConfigPath = join(__dirname, 'publisher.config.example.json');
const args = new Set(process.argv.slice(2));
const forcedZip = args.has('--zip') ? true : args.has('--no-zip') || args.has('--check') ? false : null;

const EXTRA_FORBIDDEN = ['package.json','package-lock.json','firestore.rules','firebase.json','scripts'];
const SAFE_ROOT_FILES = new Set(['index.html','manifest.json','service-worker.js','web.config','README_PUBLICACAO_IIS.txt']);
const SAFE_ROOT_EXT = new Set(['.ico','.png','.jpg','.jpeg','.svg','.webp','.txt']);

function loadConfig(){
  const fallback = JSON.parse(readFileSync(exampleConfigPath, 'utf8'));
  const local = existsSync(configPath) ? JSON.parse(readFileSync(configPath, 'utf8')) : {};
  return { ...fallback, ...local };
}
function runShell(command){
  console.log(`> ${command}`);
  execFileSync(command, { cwd: rootDir, stdio: 'inherit', shell: true });
}
function walk(dir){
  if(!existsSync(dir)) return [];
  return readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = join(dir, entry.name);
    return entry.isDirectory() ? [full, ...walk(full)] : [full];
  });
}
function bytesToHuman(bytes){
  if(bytes < 1024) return `${bytes} B`;
  if(bytes < 1024*1024) return `${(bytes/1024).toFixed(1)} KB`;
  return `${(bytes/1024/1024).toFixed(1)} MB`;
}
function countAndSize(dir){
  return walk(dir).filter(f => statSync(f).isFile()).reduce((acc, file) => ({ files: acc.files + 1, bytes: acc.bytes + statSync(file).size }), { files: 0, bytes: 0 });
}
function forbiddenMatches(packageDir, config){
  const patterns = [...new Set([...(config.forbiddenPatterns || []), ...EXTRA_FORBIDDEN])];
  const findings = [];
  for(const file of walk(packageDir)){
    const rel = relative(packageDir, file).split(sep).join('/');
    const base = rel.split('/').pop();
    const isDir = statSync(file).isDirectory();
    const lowerRel = rel.toLowerCase();
    for(const pattern of patterns){
      const lowerPattern = String(pattern).toLowerCase();
      if(lowerRel === lowerPattern || lowerRel.includes(`/${lowerPattern}`) || base.toLowerCase() === lowerPattern || lowerRel.includes(lowerPattern)) findings.push(`${rel} (padrão: ${pattern})`);
    }
    if(isDir) continue;
    if(rel.endsWith('.map')) findings.push(`${rel} (source map)`);
    const rootName = rel.includes('/') ? rel.slice(0, rel.indexOf('/')) : rel;
    const ext = base.includes('.') ? base.slice(base.lastIndexOf('.')).toLowerCase() : '';
    if(!rel.includes('/') && !SAFE_ROOT_FILES.has(rel) && !SAFE_ROOT_EXT.has(ext)) findings.push(`${rel} (arquivo raiz não permitido no IIS)`);
    if(rel.includes('/') && !['assets/'].some(prefix => rel.startsWith(prefix))) findings.push(`${rel} (subpasta não permitida no IIS)`);
    let text = '';
    try { text = readFileSync(file, 'utf8'); } catch { text = ''; }
    for(const secret of patterns.filter(p => /TOKEN|KEY|sourceMappingURL|firebase-adminsdk|serviceAccount|BEGIN PRIVATE KEY/i.test(p))){
      if(text.includes(secret)) findings.push(`${rel} (conteúdo proibido: ${secret})`);
    }
  }
  return [...new Set(findings)];
}
function createReadme(packageDir){
  writeFileSync(join(packageDir, 'README_PUBLICACAO_IIS.txt'), `HabitFlow - Pacote IIS\n\nEste pacote foi gerado automaticamente pelo Publicador IIS.\n\nComo publicar:\n\n1. Copie todo o conteúdo desta pasta para:\n   C:\\inetpub\\wwwroot\\habitflow\n\n2. Verifique se o arquivo web.config está presente.\n\n3. No IIS:\n   - Habilite Static Content.\n   - Instale IIS URL Rewrite.\n   - Crie site ou aplicação apontando para a pasta.\n   - Configure binding HTTP/HTTPS.\n   - Configure certificado SSL, se produção.\n\n4. No Firebase Console:\n   - Adicione o domínio em Authentication > Authorized domains.\n   - Configure App Check para o domínio, se enforcement estiver ativo.\n   - Garanta que Firebase Functions estão publicadas.\n   - Garanta que Firestore Rules estão publicadas.\n\n5. Testes:\n   - Abrir o site.\n   - Fazer login.\n   - Criar hábito.\n   - Abrir dashboard.\n   - Testar chatbot.\n   - Verificar console sem erros.\n`, 'utf8');
}
function createZip(packageDir, zipOutput){
  if(existsSync(zipOutput)) rmSync(zipOutput, { force: true });
  mkdirSync(dirname(zipOutput), { recursive: true });
  if(process.platform === 'win32') runShell(`powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '${packageDir}\\*' -DestinationPath '${zipOutput}' -Force"`);
  else runShell(`cd "${packageDir}" && zip -qr "${zipOutput}" .`);
}
async function confirmCopy(){
  const rl = createInterface({ input, output });
  const answer = await rl.question('Digite PUBLICAR_IIS para copiar para o IIS: ');
  rl.close();
  return answer.trim() === 'PUBLICAR_IIS';
}
function openFolder(dir){
  const opener = process.platform === 'win32' ? 'explorer' : process.platform === 'darwin' ? 'open' : 'xdg-open';
  spawnSync(opener, [dir], { stdio: 'ignore', detached: true });
}
async function main(){
  const config = loadConfig();
  const distDir = resolve(rootDir, config.distDir);
  const publishRoot = resolve(rootDir, config.publishRoot);
  const packageDir = resolve(rootDir, config.iisPackageDir);
  const zipOutput = resolve(rootDir, config.zipOutput);
  const generateZip = forcedZip ?? Boolean(config.generateZip);
  const copyToIis = args.has('--copy-to-iis') || Boolean(config.copyToIis);

  console.log(`HabitFlow IIS Publisher Pro v${config.version}`);
  runShell(config.buildCommand);
  if(!existsSync(join(distDir, 'index.html'))) throw new Error(`Build inválido: ${join(distDir, 'index.html')} não encontrado.`);
  if(config.cleanBeforePublish) rmSync(packageDir, { recursive: true, force: true });
  mkdirSync(packageDir, { recursive: true });
  cpSync(distDir, packageDir, { recursive: true, force: true });
  if(config.includeWebConfig) copyFileSync(join(rootDir, 'web.config'), join(packageDir, 'web.config'));
  createReadme(packageDir);

  const findings = forbiddenMatches(packageDir, config);
  const sourceMaps = findings.filter(f => f.includes('.map'));
  const secrets = findings.filter(f => /TOKEN|KEY|secret|serviceAccount|firebase-adminsdk/i.test(f));
  const stats = countAndSize(packageDir);
  const scanOk = findings.length === 0;
  if(!scanOk){
    console.error('\nPublicação bloqueada por arquivos/conteúdos proibidos:');
    findings.forEach(f => console.error(`- ${f}`));
  }

  let copied = false;
  if(scanOk && copyToIis && !args.has('--check')){
    if(!(await confirmCopy())) throw new Error('Cópia para IIS cancelada: confirmação explícita não recebida.');
    mkdirSync(config.iisTargetDir, { recursive: true });
    cpSync(packageDir, config.iisTargetDir, { recursive: true, force: true });
    copied = true;
  }
  if(scanOk && generateZip) createZip(packageDir, zipOutput);
  else if(args.has('--no-zip') || args.has('--check')) console.log('ZIP desativado por flag.');

  const report = `# Relatório de Publicação IIS - HabitFlow\n\n- Data/hora: ${new Date().toISOString()}\n- Versão: ${config.version}\n- Ambiente: ${config.environment}\n- Build command: ${config.buildCommand}\n- Pasta dist: ${config.distDir}\n- Pasta IIS final: ${config.iisPackageDir}\n- ZIP gerado: ${scanOk && generateZip ? 'Sim' : 'Não'}\n- web.config incluído: ${config.includeWebConfig ? 'Sim' : 'Não'}\n- Quantidade de arquivos: ${stats.files}\n- Tamanho total: ${bytesToHuman(stats.bytes)}\n- Security scan: ${scanOk ? 'OK' : 'Falha'}\n- Source maps encontrados: ${sourceMaps.length ? 'Sim' : 'Não'}\n- Secrets encontrados: ${secrets.length ? 'Sim' : 'Não'}\n- Arquivos proibidos encontrados: ${findings.length ? findings.join('; ') : 'Nenhum'}\n- copyToIis: ${copied ? 'Sim' : 'Não'}\n- iisTargetDir: ${config.iisTargetDir}\n- Resultado final: ${scanOk ? 'OK' : 'Falha'}\n`;
  mkdirSync(publishRoot, { recursive: true });
  writeFileSync(join(publishRoot, 'RELATORIO_PUBLICACAO_IIS.md'), report, 'utf8');
  if(!scanOk) process.exit(1);
  if((args.has('--open') || config.openOutputFolder) && !process.env.CI) openFolder(packageDir);
  console.log('\nResumo final');
  console.log(`- Pacote IIS: ${packageDir}`);
  console.log(`- ZIP: ${generateZip ? zipOutput : 'não gerado'}`);
  console.log('- Security scan: OK');
  console.log('Atenção: publish/, dist/ e ZIPs são artefatos locais e não devem ser versionados.');
}
main().catch(error => { console.error(`Erro: ${error.message}`); process.exit(1); });
