# Gameoteca

# 🎮 Gameoteca

![Status](https://img.shields.io/badge/Status-Beta-purple)
![Plataforma](https://img.shields.io/badge/Plataforma-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)

A **Gameoteca** é um organizador e launcher de jogos de código aberto. Seu objetivo é unificar toda a sua biblioteca gamer em uma interface única, moderna e intuitiva. Chega de caçar atalhos na área de trabalho ou abrir emuladores manualmente: inicie seus jogos de PC (Steam, Epic, executáveis soltos) e suas ROMs clássicas a partir do mesmo lugar!

---

## ✨ Funcionalidades

* 🕹️ **Lançador Unificado:** Adicione e inicie jogos de PC (arquivos `.exe`, atalhos `.lnk` ou URLs `.url` da Steam/Epic).
* 👾 **Suporte a Emuladores:** Cadastre seus emuladores favoritos e associe-os facilmente aos seus jogos.
* 📂 **Scan de Pastas (Auto-Discovery):** Configure pastas específicas de ROMs, defina o emulador correspondente e as extensões (ex: `.zip`, `.iso`, `.sfc`). A Gameoteca faz a varredura e adiciona os jogos automaticamente.
* 🖼️ **Customização Visual:** Adicione capas e banners personalizados para cada jogo e emulador.
* 🚀 **Portátil e Rápido:** Banco de dados local em formato JSON.

---

## 📥 Download e Instalação (Versão Beta)

O projeto já conta com uma versão **Beta** pronta para uso! Você não precisa instalar nada complexo, o aplicativo é *Self-Contained* (já vem com o necessário embutido).

1. Acesse a aba de [Releases](https://github.com/LuizmarCardozo/Gameoteca/releases).
2. Baixe o arquivo **`Gameoteca.zip`** da versão mais recente.
3. Coloque o executável na pasta de sua preferência.
4. Dê dois cliques e comece a organizar sua biblioteca!

*(Nota: Na primeira vez que você adicionar um jogo ou emulador, o programa criará automaticamente um arquivo de configurações `.json` na mesma pasta para salvar seus dados).*

---

## 🛠️ Como usar

### Adicionando Jogos de PC
1. Na aba **Jogos**, clique em `Adicionar Jogo`.
2. Selecione o executável (`.exe`) ou atalho (`.lnk` / `.url`).
3. Clique com o botão direito no "card" do jogo para alterar a capa ou renomear.

### Adicionando Emuladores
1. Na aba **Emuladores**, clique em `Adicionar Emulador`.
2. Selecione o arquivo `.exe` do emulador (ex: `snes9x.exe`, `pcsx2.exe`).
3. Adicione uma imagem ou logo pelo menu de contexto (botão direito).

### Usando o Scan de Pastas para ROMs
1. Vá até a aba **Pastas (Scan)** e clique em `Adicionar`.
2. Escolha a pasta onde estão suas ROMs.
3. Na tabela, selecione qual plataforma/emulador será usado para aquela pasta.
4. Defina as extensões (ex: `.zip; .smc`). Se a extensão não estiver na lista, clique em `Outra...`.
5. Clique em **Scan** e veja a mágica acontecer na aba Jogos!

---

## 💻 Tecnologias Utilizadas

A Gameoteca foi desenvolvida focando em performance e arquitetura limpa:

* **C# / .NET 8.0**
* **WPF (Windows Presentation Foundation)** para a interface gráfica.
* **MVVM (Model-View-ViewModel)** utilizando o pacote `CommunityToolkit.Mvvm` para reatividade e organização de código.
* **Ookii.Dialogs.Wpf** para diálogos nativos do Windows.

---


