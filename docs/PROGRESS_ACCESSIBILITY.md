# Acessibilidade do progresso

Cada dia é um link com nome acessível contendo data, estado e contagens. Estados combinam texto, símbolo, borda e cor. A grade declara `role=grid`, a navegação é rotulada, o foco do dia atual é visível e a lista mobile não depende de tooltip. As células usam altura mínima, quebra de texto e grade `repeat(7,minmax(0,1fr))` para evitar corte e overflow.
