# Correção de build do módulo de clientes

## Causa

O erro `CS0509` ocorria porque `UpdateClientRequest` herdava diretamente de `CreateClientRequest`, mas `CreateClientRequest` estava declarado como `sealed`. Em C#, um tipo `sealed` não pode ser usado como classe base.

Quando esse arquivo deixa de compilar, o compilador e o Razor Source Generator também deixam de enxergar `CreateClientRequest`, `UpdateClientRequest`, `ClientListItemDto`, `ClientDetailDto` e `ClientFilter`, gerando erros em cascata nas views e no controller.

## Correção aplicada

Foi criada a classe abstrata e não selada `ClientRequestBase`, contendo os campos e validações compartilhados por criação e edição de clientes. `CreateClientRequest` e `UpdateClientRequest` agora são classes `sealed` independentes que herdam da base comum.

O `ClientService` valida `ClientRequestBase`, mantendo as assinaturas públicas específicas:

- `CreateAsync(CreateClientRequest request, User adminUser, CancellationToken ct)`
- `UpdateAsync(Guid id, UpdateClientRequest request, User adminUser, CancellationToken ct)`

## Por que `UpdateClientRequest` não pode herdar de `sealed`

`sealed` indica que a classe é final e não permite derivação. Se uma classe precisa compartilhar propriedades com outra, a base deve ser não selada, abstrata ou uma composição explícita. Neste módulo, a classe abstrata `ClientRequestBase` evita duplicação e preserva contratos claros para Razor, controller e serviço.

## Limpeza de cache Razor/bin/obj

Se o Razor Source Generator continuar exibindo erros antigos depois da correção, limpe os artefatos locais e recompile:

```bash
dotnet clean
rm -rf src/HabitFlow.Web/bin src/HabitFlow.Web/obj
rm -rf src/HabitFlow.Application/bin src/HabitFlow.Application/obj
dotnet restore
dotnet build
```

No Windows, use o equivalente com `rmdir /s /q` para as pastas `bin` e `obj`.

Não versione `bin/`, `obj/` ou `publish/`.
