# Acessibilidade

O HabitFlow v5.0 inclui preferências de visualização em `/profile/accessibility`: modo padrão/alto contraste, fonte normal/maior e redução de movimento. As preferências são aplicadas imediatamente via JavaScript seguro com `classList` e salvas em `habitflow.user_ui_preferences` para usuários autenticados.

## v5.5 — Acessibilidade em feedback
O host de toast usa `aria-live="polite"`. O modal global usa `aria-labelledby`, `aria-describedby`, ESC quando apropriado e retorno de foco. Preferências respeitam alto contraste, fonte maior, reduzir movimento e reduzir pop-ups.
