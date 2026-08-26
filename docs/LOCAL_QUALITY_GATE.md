# Gate local de qualidade (v6.18.1)

## Execução

Na raiz do repositório, com .NET 10, Node.js 18+ e Python 3 disponíveis, execute:

```bash
scripts/validation/local-quality-gate.sh
```

O comando é deliberadamente estrito: para no primeiro erro e retorna código diferente de zero. Ele limpa, restaura, compila e testa a solução; instala e testa as dependências npm; executa os scans de segurança e audit; valida o diff e roda os validadores Razor/CSS, SQL, arquivos proibidos e migrations. Não use `skip` para contorná-lo.

## Razor e CSS

CSS de uma página deve ficar em `src/HabitFlow.Web/wwwroot/css`, escopado sob uma classe raiz específica da página. Componentes realmente globais pertencem ao design system/global CSS. Um `<style>` pequeno e excepcional pode permanecer em Razor, mas as diretivas precisam ser escapadas (`@@media`, `@@supports`, `@@keyframes`, `@@container`, `@@font-face` e `@@page`). Mova o bloco para `wwwroot/css` antes que ele ultrapasse 80 linhas ou 5.000 caracteres. Declare `@section` na coluna inicial e no nível superior da view.

Evite sobrescrever `.card`, `.btn`, `.dropdown-menu`, `.modal`, `.offcanvas`, `dialog`, `section`, `main` ou `.container` sem uma classe raiz. `!important` deve ser exceção documentada, não a estratégia de cascata. Dropdowns, modais e offcanvas devem ficar ocultos quando vazios; os testes Playwright existentes cobrem rotas e overlays públicos/autenticados.

## SQL com Dapper

Prefira raw strings para SQL multilinha e verbatim strings apenas quando forem mais legíveis:

```csharp
const string sql = """
    select * from habitflow.habits
    where client_id = @clientId and user_id = @userId
    """;
return db.QueryAsync<Habit>(sql, new { clientId, userId }, ct);
```

Valores são sempre parâmetros Dapper. Nunca use `$"...{input}..."` para dados recebidos do usuário. Se um identificador SQL precisar ser dinâmico, escolha-o de uma allowlist fechada antes de compor a consulta e documente a razão.

## Arquivos gerados e relatórios

Nunca versione `bin/`, `obj/`, `.vs/`, `*.g.cs`, logs, estados de storage ou resultados temporários. Screenshots de QA pertencem a `artifacts/`; credenciais e connection strings reais ficam fora do Git. Antes de commitar, confira `git status` e `git diff --check`.

Cada execução recria `artifacts/validation/razor-css-report.txt`, `sql-string-report.txt`, `forbidden-files-report.txt` e `css-global-report.txt`, além dos logs de cada comando. `STATUS: FAIL` lista erros bloqueantes; `REVIEW` registra dívida global auditada que não foi introduzida pelo diff. O workflow publica esses arquivos mesmo quando uma etapa falha, facilitando localizar a primeira causa real (inclusive quando um `CS0006` é apenas consequência).
