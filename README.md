<div align="center">

# 🏎️ Bezier Runner 3D: O Labirinto Geométrico

![Unity](https://img.shields.io/badge/Engine-Unity%206-black?logo=unity)
![C#](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![Computer Graphics](https://img.shields.io/badge/UFRR-Computa%C3%A7%C3%A3o%20Gr%C3%A1fica-0055A4)
![Platform](https://img.shields.io/badge/Platform-Windows%20(.exe)-0078D6?logo=windows)

</div>

Jogo 3D interativo desenvolvido na **Unity 6** como **Trabalho Final** da disciplina de **Computação Gráfica** do curso de Ciência da Computação da **Universidade Federal de Roraima (UFRR)**, ministrada pelo **Prof. Dr. Luciano Ferreira Silva**.

O projeto combina uma mecânica de jogo *arcade* tridimensional estilo *Low Poly* com um laboratório visual interativo que permite demonstrar e alternar, em tempo real, as principais técnicas do *pipeline* de Computação Gráfica 3D.

---
## 🎮 Sobre o Jogo
Em **Bezier Runner 3D**, você pilota um **Carro Esportivo Low Poly Azul** dentro de uma arena tridimensional arborizada com **duas fileiras de três Árvores Low Poly** (6 árvores no total).
Seu objetivo é coletar os **6 Cristais de Energia** (octaedros verdes brilhantes) espalhados pelo mapa antes de perder suas **3 vidas**, desviando de:
- **Barreiras Móveis Oscilantes:** blocos laranjas que se deslocam em movimento harmônico nos corredores da arena.
- **Drones Quadricópteros 3D de Bézier:** drones com 4 hélices rotativas que patrulham o cenário de uma parede à outra seguindo trajetórias sinuosas em formato de **"S"** entre as árvores e passando sobre os cristais, guiados matematicamente por **Curvas de Bézier Cúbicas**.
---
## 🖥️ Técnicas de Computação Gráfica Aplicadas
| Técnica de CG | Aplicação no Jogo |
| :--- | :--- |
| **1. Sistemas de Coordenadas** | Conversão contínua entre Coordenadas Locais (hierarquia de peças do carro, copas das árvores e hélices dos drones), Espaço do Mundo (*World Space*) e Espaço da Câmera (`LookAt` / `Lerp`). |
| **2. Primitivas 3D & Modelagem Low Poly** | Construção de modelos 3D compostos por malhas triangulares (Carro esportivo com chassi, cabine, aerofólio, faróis e 4 rodas; 6 Árvores em camadas; Drones quadricópteros e Cristais). |
| **3. Curvas de Bézier Cúbicas (Grau 3)** | Trajetórias de patrulha em formato de **"S"** calculadas pela Equação Paramétrica de Bernstein com 4 pontos de controle: $B(t) = (1-t)^3P_0 + 3(1-t)^2tP_1 + 3(1-t)t^2P_2 + t^3P_3$. |
| **4. Transformações Geométricas 3D** | **Translação** (movimento do carro e barreiras), **Rotação** (giro contínuo dos cristais nos 3 eixos, rodas do carro e hélices dos drones) e **Escala** (pulsação senoidal dos cristais). |
| **5. Projeções Geométricas** | Alternância em tempo real (**Tecla `P`**) entre **Projeção Perspectiva** (*Viewing Frustum* piramidal com ponto de fuga) e **Projeção Paralela Ortográfica** (volume paralelepípedo sem distorção de profundidade). |
| **6. Algoritmo de Recorte 3D (*Clipping*)** | Demonstração ao vivo do *Viewing Frustum Clipping* (**Tecla `C`**), alterando o plano de recorte traseiro (*Far Clipping Plane*) de `100 m` para `27 m` para recortar os polígonos ao fundo da arena. |
| **7. Superfícies Escondidas** | Aplicação de **Back-Face Culling** (descarte de faces ocultas pela normal $\vec{N} \cdot \vec{V} \ge 0$) e **Z-Buffer (*Depth Buffer*)** na oclusão entre o carro, as copas das árvores e os drones. |
| **8. Iluminação e Sombreamento** | Combinação de **Luz Ambiente**, **Reflexão Difusa de Lambert** (visível nas faces das árvores *Low Poly*), **Reflexão Especular de Phong** (brilho metálico no carro e drones), **Luz Pontual** no veículo e sombras projetadas (*Soft Shadows*) com rotação dinâmica do vetor de luz (**Tecla `L`**). |
---
## ⌨️ Controles e Atalhos de Demonstração
| Tecla | Ação |
| :---: | :--- |
| **`W, A, S, D`** ou **`Setas`** | Controla a movimentação (Translação e Rotação 3D) do Carro Low Poly Azul |
| **`Tecla P`** | Alterna a câmera entre **Projeção Perspectiva** e **Projeção Ortográfica** |
| **`Tecla C`** | Ativa/Desativa o **Recorte 3D (*Far Clipping Plane* em 27m vs. 100m)** |
| **`Tecla B`** | Mostra/Oculta o traçado em **"S"** das **Curvas de Bézier** no piso da arena |
| **`Segurar Tecla L`** | Gira o vetor da **Luz Direcional** (demonstra Sombreamento de Lambert/Phong e Sombras) |
| **`Tecla R`** | Reinicia a partida após tela de Vitória ou Fim de Jogo |
| **`Tecla ESC`** | Fecha o jogo executável (`.exe`) |
---
## 🔊 Sistema de Áudio (Livre de Direitos Autorais)
O jogo conta com 4 canais de áudio independentes:
1. **Música Instrumental de Fundo:** Trilha ambiente estilo *synthwave/arcade* em loop contínuo (100% original e livre de direitos autorais).
2. **Som do Motor do Carro:** Ativado dinamicamente apenas enquanto o veículo está em movimento.
3. **Som dos Drones:** Zumbido sutil das hélices que varia conforme a distância até o jogador.
4. **Efeitos Sonoros (SFX):** Sons de coleta de cristal, impacto de colisão, fanfarra musical de **Vitória** e tema de **Derrota (Game Over)**.
---
## 🚀 Como Baixar e Jogar
Primeiro, baixe o repositório clicando no botão verde **`<> Code`** (no topo da página) -> **`Download ZIP`** e extraia o arquivo `.zip` no seu computador. Dentro dele você encontrará duas pastas principais:
### Opção 1: Jogar Direto pelo Executável — Pasta `CG - Trabalho FInal`
*(Não precisa ter a Unity instalada)*
1. Abra a pasta **`CG - Trabalho FInal`** que você extraiu.
2. Dê dois cliques no arquivo **`My project.exe`** *(mantenha ele dentro da pasta junto com `My project_Data`, `UnityPlayer.dll` e `MonoBleedingEdge`)*.
3. O jogo abrirá imediatamente! Para sair do jogo, pressione a tecla **`ESC`**.
---
### Opção 2: Abrir o Projeto na Unity — Pasta `CG - Projeto`
1. Abra o **Unity Hub** (versão recomendada: **Unity 6 / 6000.x**).
2. No canto superior direito, clique em **`Add` -> `Add project from disk`**.
3. Selecione a pasta **`CG - Projeto`** (que contém as subpastas `Assets`, `Packages` e `ProjectSettings`) e clique em **`Add Project`**.
4. Clique sobre **`CG - Projeto`** na lista do Unity Hub para abri-lo *(no primeiro acesso, aguarde cerca de 1 minuto enquanto a Unity gera a biblioteca local `Library`)*.
5. Com o editor da Unity aberto, vá até a aba **`Project`** (na parte inferior da tela), entre na pasta **`Assets` -> `Scenes`** e dê **dois cliques** na cena **`SampleScene`** para carregar a arena 3D.
6. Pressione o botão **`Play (▶)`** no topo central da Unity para jogar!
## 🎓 Informações Acadêmicas
- **Instituição:** Universidade Federal de Roraima (UFRR)
- **Departamento:** Departamento de Ciência da Computação (DCC)
- **Disciplina:** Computação Gráfica
- **Docente:** Prof. Dr. Luciano Ferreira Silva
- **Discente:** Angelo Almeida Ferro
