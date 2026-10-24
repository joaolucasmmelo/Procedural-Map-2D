# Procedural Map 2D — Atividade 3 (Motores de Jogos)

Mundo 2D visto de cima, gerado por código: terreno em chunks, cavernas por autômato
celular dentro de buracos espalhados pelo mapa, itens coletáveis que não voltam,
inventário e salvar/carregar.

- **Motor:** Unity 6.3 LTS (6000.3.24f1), URP 2D
- **Integrantes:** João Lucas de Melo Macedo e Guilherme da Silva

## Como rodar
Abra `Assets/Scenes/SampleScene.unity` e aperte **Play**.

## Controles
| Tecla | Ação |
|---|---|
| WASD | andar |
| Shift | correr (para testar chunks e floating origin) |
| E | em cima de um buraco preto: entra na caverna. Na célula amarela da caverna: sai |
| Tab | mostra/esconde o inventário |
| F5 / F9 | salvar / carregar |
| N | mundo novo (semente aleatória) |
| R | regenera o terreno com os parâmetros atuais do Inspector |

## Arquitetura
Scripts em `Assets/_Proc2D/Scripts`.

| Parte | Script | Responsabilidade |
|---|---|---|
| Gerador de ruído | `Noise.cs` | `Fbm(x, y, ...)` → 0..1. Não sabe o que é chunk. |
| Gerador de chunk | `ChunkGenerator.cs` | `(cx, cy)` → malha, buraco e itens, sempre em coordenada global. |
| Gerenciador de chunks | `ChunkManager.cs` | Grade 3x3 ao redor do jogador, cria/apaga chunks e faz o floating origin. |
| Estado do mundo | `WorldState.cs` + `Inventory.cs` | Semente, coletados, inventário e posição global. É o único que vai para o save. |
| Interface | `InventoryUI.cs` | Só mostra. O inventário é redesenhado pelo evento `Inventory.Changed`. |

`CaveGenerator.cs` (autômato celular) e `CaveManager.cs` (entrar e sair) fazem as
cavernas. `GameManager.cs` trata as teclas de salvar, carregar, mundo novo e regenerar.

## Decisões de modelagem

### Ruído e terreno
- **Ruído:** `Mathf.PerlinNoise` em fBm, com **4 oitavas**, escala 0.05, persistência
  0.5 e lacunaridade 2.
- **Semente:** o `Mathf.PerlinNoise` não tem semente. A semente (`WorldState.seed`)
  gera o offset do ruído por `System.Random(semente)`.
- **Curva:** `AnimationCurve` aplicada ao valor do ruído antes das faixas.
- **Faixas:** água (< 0.35, não caminhável), areia (< 0.45), grama (< 0.70) e gelo
  (resto). Cada faixa tem uma cor. O jogador não entra na água.
- **Parâmetros:** ficam no Inspector do `World` (ChunkGenerator). Depois de mudar algum,
  aperte **R** em Play para regenerar.

### Chunks
- **Tamanho:** 32 x 32 células (1 célula = 1 unidade). Cada chunk é um `Mesh` próprio,
  com um quadrado por célula e cor por vértice.
- **Grade:** sempre 9 chunks vivos. Quando o jogador muda de chunk, quem saiu da grade é
  destruído (com a malha) e quem falta é criado.
- **Costura:** o ruído é amostrado em `gx = cx * 32 + x` (coordenada global), por isso
  não há emenda entre chunks.

### Floating origin
- **Quando dispara:** no `LateUpdate` do `ChunkManager`, quando o |x| ou o |y| do
  jogador **na cena** passa de **1000** (`Origin Limit`).
- **O que é deslocado:** todos os objetos raiz da cena: chunks (com os itens filhos),
  caverna, jogador com a câmera, HUD e luz. O deslocamento é a posição do jogador
  arredondada para inteiro.
- O total acumulado fica em `worldOrigin`. Posição verdadeira = posição na cena +
  `worldOrigin`. É ela que entra no cálculo do chunk, nas chaves e no save.
- O HUD mostra a posição verdadeira, a posição na cena e a origem.

### Cavernas (autômato celular)
- **Buracos:** cada chunk tem 50% de chance de ter um buraco (célula preta), sorteado por
  `System.Random(semente, cx, cy)`. Só nasce em célula caminhável.
- **Semente da caverna:** `semente * 31 + buracoX * 7919 + buracoY * 104729`. Por isso
  **cada buraco tem uma caverna diferente**, mas sempre a mesma.
- **Grade:** 48 x 32 células. Os passos:
  1. **Preencher:** 45% de paredes; a borda é sempre parede.
  2. **Suavizar:** 5 iterações da vizinhança de Moore: mais de 4 paredes vira parede,
     menos de 4 vira chão, exatamente 4 fica como está.
  3. **Flood fill:** BFS para achar cada sala.
  4. **Limpar:** salas com menos de 15 células viram parede.
  5. **Conectar:** cava um túnel de 3 células entre as salas mais próximas, até sobrar
     uma sala só.
- **Entrada e saída:** a célula de chão mais perto do centro fica amarela. O jogador
  aparece nela ao entrar e sai por ela com **E**. Como a caverna é uma sala só, a saída
  sempre é alcançável.

### Itens
- **Superfície:** um fBm de densidade com offset próprio (+5000) e escala 0.1. Os
  candidatos ficam a cada 3 células. O item nasce se a densidade for maior que 0.62 e a
  célula for caminhável. Densidade maior que 0.7 gera **gema** (rara); o resto gera **moeda**.
- **Caverna:** **cristal**, com densidade maior que 0.55, só em célula de chão.
- **Chave:** `semente:cx:cy:i` na superfície e `semente:cav:buracoX:buracoY:i` na caverna.
  O índice `i` avança em todo candidato aprovado, **inclusive nos já coletados**, então as
  chaves nunca mudam.
- **Coleta:** ao encostar (trigger 2D), a chave entra no `HashSet` de coletados. Ao gerar
  um chunk ou uma caverna, as chaves coletadas não são instanciadas.

### Inventário
`Inventory` é uma classe C# pura: 3 slots, com pilha máxima de 5 moedas e 2 gemas ou
cristais por slot. Ela dispara o evento `Changed` e a UI se inscreve uma única vez. Se o
item não cabe, aparece **"Inventário cheio!"**, a chave não entra em coletados e o item
fica no chão.

### Salvar e carregar
- **Arquivo:** `save.json` em `Application.persistentDataPath` (no Linux:
  `~/.config/unity3d/DefaultCompany/Procedural Map 2D/save.json`).
- **Conteúdo:** semente, lista de coletados, inventário e posição verdadeira do jogador.
  Se o save for feito dentro de uma caverna, a posição salva é a do buraco na superfície.
- **Ao carregar:** a semente é aplicada (o que regera o offset), todos os chunks são
  apagados, a origem é posta perto do jogador e os 9 chunks são recriados, já pulando os
  coletados. Isso também cobre um save com semente diferente (teste: F5, N, F9).

## Estados tratados
| Estado | Como |
|---|---|
| Cruzar a borda de um chunk | A grade se atualiza; os vizinhos já existiam. |
| Chunk sai e volta | Tudo vem da semente e da coordenada global, então volta idêntico. |
| Item coletado e chunk recriado | A chave está em coletados, então o item não é instanciado. |
| Floating origin em movimento | Desloca tudo junto, inclusive o jogador; o movimento continua igual. |
| Save com outra semente | O mundo é apagado e reconstruído com a semente do save. |
| Caverna sem saída | Limpeza + conexão até uma sala só; a saída fica no chão dessa sala. |
| Inventário cheio | Aviso na tela; o item fica no chão. |
