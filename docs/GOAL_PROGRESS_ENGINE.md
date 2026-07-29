# Motor de progresso de objetivos

Tipos canônicos ficam em `GoalTargetType`; parsing e textos públicos são centralizados. A atualização automática deverá sempre recalcular a fonte canônica, nunca somar cegamente. Objetivos concluídos permanecem concluídos após undo, preservando `completed_at`; a correção deve ser registrada no histórico e não deve repetir o marco inicial.

> Limitação desta entrega: a integração transacional completa do motor e eventos não foi implementada porque o ambiente não dispõe do SDK para validar uma alteração transversal segura.
