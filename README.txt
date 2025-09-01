===============================
Fila de Sinuca - .NET MAUI (Android)
Regras + Remoções + Confirmações + Persistência (Preferences/SQLite)
===============================

O que esta versão entrega
-------------------------
- **Confirmação** antes de remover (fila, P1/P2, limpar mesa, limpar fila, limpar tudo).
- **Limpar mesa**, **limpar fila** e **limpar TUDO (mesa + fila)**.
- **Persistência local**:
  - **SQLite (sqlite-net-pcl)**: fila, mesa e vitórias consecutivas.
  - **Preferences**: configuração de regras (limite de vitórias consecutivas).
- Regras finais:
  1) Registrar sempre o **ganhador** (Vitória P1 / Vitória P2).
  2) **Perdedor sai** e vai **para o fim da fila**.
  3) Se o **ganhador atingir 3 vitórias consecutivas**, **AMBOS saem** da mesa e entram no fim **em ordem**: ganhador, depois perdedor.
  4) Sempre que alguém sai da mesa, ele vai para o **fim da fila**.
- Remover pessoas da fila por **seleção, swipe, nome, ID, índice** e **remover P1/P2**.

Pré-requisitos
--------------
- .NET 8 SDK
- Workload MAUI: `dotnet workload install maui`
- NuGet: **sqlite-net-pcl** (>= 1.9.0)
  - CLI: `dotnet add package sqlite-net-pcl`

