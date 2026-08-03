[CmdletBinding(DefaultParameterSetName='Generate')]
param(
  [string]$Email = 'superadmin@habitflow.local',
  [string]$Name = 'Administrador HabitFlow',
  [Parameter(ParameterSetName='Generate')][switch]$GeneratePassword,
  [Parameter(ParameterSetName='Prompt')][switch]$PromptPassword,
  [switch]$ResetExisting
)
$ErrorActionPreference = 'Stop'
if ($env:ASPNETCORE_ENVIRONMENT -ne 'Development') { throw 'Provisionamento de desenvolvimento recusado: ASPNETCORE_ENVIRONMENT deve ser Development.' }
if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected -or [Console]::IsErrorRedirected) { throw 'É necessário um terminal interativo sem redirecionamento.' }
$argsList = @('run','--project','src/HabitFlow.Web','--','admin','create-dev-superadmin','--email',$Email,'--name',$Name)
if ($PromptPassword) { $argsList += '--prompt-password' } else { $argsList += '--generate-password' }
if ($ResetExisting) { $argsList += '--reset-existing' }
& dotnet @argsList
if ($LASTEXITCODE -ne 0) { throw "O provisionamento falhou com código $LASTEXITCODE." }
