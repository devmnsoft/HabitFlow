# Contratos dos doubles de repositório

Doubles `MissingDatabase` devem lançar a falha canônica em toda leitura e escrita. Doubles `Counting` incrementam exclusivamente a operação observada; `InMemory` deve refletir mutações em seu estado; `NoOp` só representa sucesso quando isso fizer parte explícita do cenário. Interfaces de produção não recebem implementação padrão para mascarar CS0535. Toda evolução de contrato exige busca nos projetos unitário, integração, funcional e adaptadores internos.
