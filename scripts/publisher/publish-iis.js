#!/usr/bin/env node
import { copyFileSync, cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = resolve(__dirname, '../..');
const configPath = join(__dirname, 'publisher.config.json');
const exampleConfigPath = join(__dirname, 'publisher.config.example.json');
const packageJson = JSON.parse(readFileSync(join(rootDir, 'package.json'), 'utf8'));
const publishRoot = join(rootDir, 'publish');
const iisPublishDir = join(publishRoot, 'iis', 'HabitFlow-IIS');
const zipPath = join(publishRoot, `HabitFlow-IIS-v${packageJson.version}.zip`);

function loadConfig() {
  const fallback = JSON.parse(readFileSync(exampleConfigPath, 'utf8'));
  if (!existsSync(configPath)) return fallback;
  return { ...fallback, ...JSON.parse(readFileSync(configPath, 'utf8')) };
}

function parseZipMode(config) {
  const args = new Set(process.argv.slice(2));
  if (args.has('--no-zip')) return false;
  if (args.has('--zip')) return true;
  return Boolean(config.generateZip);
}

function run(command, args, cwd = rootDir) {
  console.log(`> ${command} ${args.join(' ')}`);
  execFileSync(command, args, { cwd, stdio: 'inherit', shell: process.platform === 'win32' });
}

function copyRequiredFile(fileName) {
  const source = join(rootDir, fileName);
  if (!existsSync(source)) throw new Error(`Arquivo obrigatório não encontrado: ${fileName}`);
  copyFileSync(source, join(iisPublishDir, fileName));
}

function createReadme() {
  const content = `HabitFlow - Publicação IIS\n\n` +
    `Este diretório foi gerado localmente pelo publicador IIS.\n` +
    `Copie o conteúdo desta pasta para o site configurado no IIS.\n\n` +
    `Atenção: os arquivos em publish/ e pacotes .zip são artefatos locais de publicação. Eles não devem ser versionados no Git.\n` +
    `O repositório deve conter apenas código-fonte, scripts, web.config e documentação.\n`;
  writeFileSync(join(iisPublishDir, 'README_PUBLICACAO_IIS.txt'), content, 'utf8');
}

function createZip() {
  if (existsSync(zipPath)) rmSync(zipPath, { force: true });
  if (process.platform === 'win32') {
    run('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', `Compress-Archive -Path "${iisPublishDir}\\*" -DestinationPath "${zipPath}" -Force`]);
    return;
  }
  run('zip', ['-r', zipPath, 'iis/HabitFlow-IIS'], publishRoot);
}

function copyToIis(config) {
  if (!config.copyToIis) return;
  if (!config.iisPath) throw new Error('copyToIis=true exige iisPath no publisher.config.json.');
  mkdirSync(config.iisPath, { recursive: true });
  cpSync(iisPublishDir, config.iisPath, { recursive: true, force: true });
  console.log(`Arquivos copiados para IIS: ${config.iisPath}`);
}

const config = loadConfig();
const shouldGenerateZip = parseZipMode(config);

console.log('Iniciando pacote local para IIS...');
run('npm', ['run', 'build']);

rmSync(iisPublishDir, { recursive: true, force: true });
mkdirSync(iisPublishDir, { recursive: true });
cpSync(join(rootDir, 'dist'), iisPublishDir, { recursive: true, force: true });
copyRequiredFile('web.config');
createReadme();
copyToIis(config);

if (shouldGenerateZip) {
  mkdirSync(publishRoot, { recursive: true });
  createZip();
  console.log(`ZIP gerado localmente: ${zipPath}`);
} else {
  console.log('ZIP desativado. Se quiser compactar localmente, execute npm run publish:iis:zip ou compacte manualmente a pasta publish/iis/HabitFlow-IIS.');
}

console.log(`Pasta IIS gerada localmente: ${iisPublishDir}`);
console.log('Pacote gerado localmente. Os arquivos em publish/ não devem ser versionados.');
