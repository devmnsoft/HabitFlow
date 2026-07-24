$ErrorActionPreference='Stop'
$name=Read-Host 'Nome do SuperAdmin'
$email=Read-Host 'E-mail do SuperAdmin'
$secure=Read-Host 'Senha' -AsSecureString
$plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
$hash=dotnet run --project src/HabitFlow.Web -- --hash-password "$plain"
$plain=$null
$sql="insert into habitflow.users(id,name,email,password_hash,role,account_status,plan,plan_status,created_at,updated_at) values(gen_random_uuid(),@name,@email,@hash,'SuperAdmin','Active','Free','Active',now(),now()) on conflict (email) do update set role='SuperAdmin', password_hash=excluded.password_hash, updated_at=now();"
Write-Host 'Execute o SQL abaixo com parâmetros seguros no PostgreSQL. A senha não foi exibida.'
Write-Host $sql
Write-Host "name=$name email=$email hash=<gerado>"
