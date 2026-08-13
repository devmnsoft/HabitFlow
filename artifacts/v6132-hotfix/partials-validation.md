# Validação preventiva de partials

A chamada global foi inventariada com `rg -n '<partial name="' src/HabitFlow.Web/Views`. Chamadas curtas foram conferidas como arquivos irmãos ou partial compartilhado; chamadas em diretórios `Partials` já usam caminho explícito ou absoluto, salvo o defeito abaixo.

| Partial | Chamador | Arquivo existente | Correção | Status |
|---|---|---|---|---|
| `_HabitStatusBadge` | `Habits/Partials/_HabitCard.cshtml` | `Habits/Partials/_HabitStatusBadge.cshtml` (criado) | Nome alterado para `Partials/_HabitStatusBadge` | Corrigido |
| `_HabitStatusBadge` | `Habits/Detail.cshtml` | `Habits/Partials/_HabitStatusBadge.cshtml` | Já usava `Partials/_HabitStatusBadge` | Válido |
| Demais partials de Habits | `Habits/Index.cshtml`, `Detail.cshtml` e `Editor.cshtml` | respectivos arquivos sob `Habits/Partials` | Nenhuma | Válidos por caminho explícito |
| `_GoalProgressBar` | `Goals/Detail.cshtml`, `Goals/Partials/_GoalCard.cshtml` | `Goals/Partials/_GoalProgressBar.cshtml` | Nenhuma; já usa `Partials/_GoalProgressBar` | Válido |
| Partials curtos das demais áreas | views das próprias áreas | arquivos irmãos ou `_ValidationScriptsPartial` compartilhado | Nenhuma mudança fora do escopo | Válidos no mecanismo padrão do Razor |
