# Vínculos objetivo–hábito

Operações de listar, vincular, desvincular e substituir devem validar `clientId` e `userId` nos dois lados, objetivo ativo e hábito não arquivado. A unicidade do banco fornece idempotência; endpoints mutáveis exigem antiforgery. Nunca se aceita um identificador pertencente a outra conta.
