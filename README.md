**Plugin interno para Autodesk Revit** — Automação das etapas iniciais de modelagem hidráulica predial.

![Revit](https://img.shields.io/badge/Autodesk%20Revit-2024%20|%202025%20|%202026-blue)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-purple)
![Status](https://img.shields.io/badge/Status-Em%20Desenvolvimento-orange)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Descrição

Este plugin automatiza as **etapas iniciais** da modelagem de instalações hidráulicas em projetos BIM dentro do Autodesk Revit. Destinado a edifícios residenciais e comerciais de pequeno e médio porte.

### O que o plugin faz:

| Funcionalidade | Descrição |
|----------------|-----------|
| **Detectar Ambientes** | Identifica automaticamente os Rooms hidráulicos do modelo (banheiros, cozinhas, lavanderias, etc.) |
| **Identificar Equipamentos** | Localiza equipamentos MEP (vasos sanitários, pias, chuveiros) nos ambientes detectados |
| **Criar Colunas** | Gera colunas verticais de água fria e esgoto atravessando os níveis do projeto |
| **Gerar Ramais** | Cria ramais de tubulação conectando cada equipamento à coluna mais próxima |
| **Pipeline Completo** | Executa todas as etapas de uma vez, com confirmação do usuário |

### O que o plugin NÃO faz (nesta versão):

- Dimensionamento hidráulico completo
- Cálculo de pressão e vazão
- Geração automática de conexões/fittings
- Projeto de água quente ou pluvial

---

## Requisitos

### Para compilar:

- **Visual Studio 2022** (Community, Professional ou Enterprise)
- **.NET Framework 4.8** (geralmente já incluído no Windows 10/11)
- **.NET SDK** (para compilação via linha de comando)
- **Autodesk Revit 2024, 2025 ou 2026** instalado (necessário para as referências da API)

### Para executar:

- **Autodesk Revit 2024, 2025 ou 2026**
- Um projeto Revit contendo:
  - **Rooms** (ambientes) com nomes em português ou inglês
  - **Plumbing Fixtures** (equipamentos hidráulicos) posicionados nos ambientes
  - **Piping System Types** configurados (Sanitary, Domestic Cold Water)
  - **Pipe Types** disponíveis (PVC ou similar)

---

## Estrutura do Projeto

```
RevitHydraulicPlugin/
│
├── RevitHydraulicPlugin/                 ← Código-fonte do plugin
│   ├── Commands/                          ← Pontos de entrada (IExternalCommand)
│   │   ├── DetectRoomsCommand.cs
│   │   ├── IdentifyEquipmentCommand.cs
│   │   ├── CreateColumnsCommand.cs
│   │   ├── GenerateBranchesCommand.cs
│   │   └── RunFullPipelineCommand.cs
│   ├── Models/                            ← Modelos de domínio
│   ├── Services/                          ← Orquestração e criação de elementos
│   ├── Detection/                         ← Análise do modelo existente
│   ├── Routing/                           ← Cálculo de rotas de tubulação
│   ├── Configuration/                     ← Regras e parâmetros configuráveis
│   ├── Utilities/                         ← Helpers e utilitários
│   ├── RevitHydraulicPlugin.addin         ← Manifesto de registro no Revit
│   └── RevitHydraulicPlugin.csproj        ← Arquivo de projeto
│
├── RevitHydraulicPlugin.TestEnvironment/  ← Ambiente de testes local (sem Revit)
│   ├── Mocks/                             ← Classes simuladas
│   ├── Services/                          ← Lógica de teste
│   ├── Tests/                             ← Testes automatizados
│   └── Program.cs                         ← Console runner
│
├── build/                                 ← Scripts de compilação e instalação
│   ├── build.bat                          ← Compila e gera distribuição
│   └── install.bat                        ← Instala no Revit
│
├── docs/                                  ← Documentação
│   └── testing-guide.md                   ← Guia de testes para o testador
│
├── RevitHydraulicPlugin.sln               ← Solution do Visual Studio
├── README.md                              ← Este arquivo
├── LICENSE                                ← Licença MIT
└── .gitignore                             ← Exclusões do Git
```

---

## Como Compilar

### Opção 1: Via Script (Recomendado)

```cmd
cd RevitHydraulicPlugin
build\build.bat
```

O script compila automaticamente e copia os arquivos para `build\dist\`.

### Opção 2: Via Visual Studio

1. **Clone o repositório:**
   ```cmd
   git clone https://github.com/SEU_USUARIO/RevitHydraulicPlugin.git
   cd RevitHydraulicPlugin
   ```

2. **Abra a Solution:**
   - Dê duplo-clique em `RevitHydraulicPlugin.sln`
   - Ou abra pelo Visual Studio: `File > Open > Project/Solution`

3. **Verifique as referências do Revit API:**

   O projeto auto-detecta a versão do Revit instalada. Se necessário, ajuste manualmente no `.csproj`:
   ```xml
   <RevitApiPath>C:\Program Files\Autodesk\Revit 2025</RevitApiPath>
   ```
   
   Ou defina a variável de ambiente antes de compilar:
   ```cmd
   set REVIT_API_PATH=C:\Program Files\Autodesk\Revit 2025
   ```

4. **Compile:**
   - Selecione a configuração **Release**
   - Pressione `Ctrl+Shift+B` ou vá em `Build > Build Solution`
   - A DLL será gerada em `RevitHydraulicPlugin\bin\Release\`

### Opção 3: Via Linha de Comando

```cmd
dotnet build RevitHydraulicPlugin\RevitHydraulicPlugin.csproj --configuration Release
```

---

## Como Instalar no Revit

### Instalação Automática (Recomendada)

Após compilar com `build\build.bat`:

```cmd
build\install.bat
```

O script auto-detecta a versão do Revit. Para especificar:

```cmd
build\install.bat 2025
```

### Instalação Manual

1. **Copie o arquivo `.addin`** para a pasta de Addins do Revit:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\20XX\RevitHydraulicPlugin.addin
   ```

2. **Crie a pasta do plugin** dentro de Addins:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\20XX\RevitHydraulicPlugin\
   ```

3. **Copie o `.dll`** compilado para essa pasta:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\20XX\RevitHydraulicPlugin\RevitHydraulicPlugin.dll
   ```

4. **Reinicie o Revit.**

> **Nota:** Substitua `20XX` pela versão do Revit (2024, 2025 ou 2026).

---

## Como Executar no Revit

### Localização dos comandos:

1. Abra o Revit e carregue um projeto
2. Vá para a aba **Add-Ins** na Ribbon
3. Clique em **External Tools**
4. Você verá 5 comandos disponíveis:

| Comando | Ação | Modifica o Modelo? |
|---------|------|--------------------|
| Detectar Ambientes Hidráulicos | Lista os Rooms reconhecidos | ❌ Não |
| Identificar Equipamentos Hidráulicos | Lista equipamentos encontrados | ❌ Não |
| Criar Colunas Hidráulicas | Gera colunas verticais | ✅ Sim (pede confirmação) |
| Gerar Ramais Hidráulicos | Conecta equipamentos às colunas | ✅ Sim (pede confirmação) |
| **Automação Hidráulica Completa** | Executa todas as etapas | ✅ Sim (pede confirmação) |

### Recomendação de uso:

1. **Primeiro**, execute "Detectar Ambientes" para verificar se os Rooms foram reconhecidos
2. **Depois**, execute "Identificar Equipamentos" para confirmar os equipamentos
3. **Por fim**, execute "Automação Hidráulica Completa" para gerar colunas e ramais

> **⚠️ IMPORTANTE:** Os comandos que modificam o modelo pedem confirmação antes de executar. Se algo der errado, use `Ctrl+Z` (Undo) no Revit.

---

## Cenário de Teste Sugerido

Para validar o plugin, crie o seguinte modelo de teste no Revit:

### 1. Configuração do Projeto

- Crie um novo projeto usando o template padrão de arquitetura
- Certifique-se de que existem pelo menos 2 níveis (ex: Térreo e 1º Pavimento)

### 2. Criação dos Ambientes

No **Térreo**, crie os seguintes Rooms (com paredes ao redor):

| Room | Nome | Área aprox. |
|------|------|-------------|
| 1 | **Banheiro** | ~5 m² |
| 2 | **Cozinha** | ~10 m² |
| 3 | **Sala** | ~20 m² |

### 3. Inserção de Equipamentos

No **Banheiro**, insira (categoria Plumbing Fixtures):
- 1× Vaso sanitário (Toilet / Water Closet)
- 1× Lavatório (Lavatory / Wash Basin)
- 1× Chuveiro (Shower)

Na **Cozinha**, insira:
- 1× Pia (Kitchen Sink)

### 4. Execução e Validação

1. Execute **"Detectar Ambientes Hidráulicos"**
   - ✅ Esperado: Banheiro e Cozinha detectados, Sala ignorada

2. Execute **"Identificar Equipamentos Hidráulicos"**
   - ✅ Esperado: 4 equipamentos listados (vaso, lavatório, chuveiro, pia)

3. Execute **"Automação Hidráulica Completa"**
   - ✅ Confirme quando o diálogo aparecer
   - ✅ Esperado: Colunas verticais e ramais de tubulação criados

### 5. Verificação Visual

Após a execução, mude para vista 3D e verifique:
- Colunas verticais atravessando os níveis
- Ramais horizontais conectando equipamentos às colunas
- Esgoto: tubo de Ø100mm (vaso) e Ø50mm (demais)
- Água fria: tubo de Ø25mm

---

## Testes Locais (sem Revit)

O projeto inclui um ambiente de testes que roda sem o Revit:

```cmd
dotnet run --project RevitHydraulicPlugin.TestEnvironment\RevitHydraulicPlugin.TestEnvironment.csproj
```

Os testes validam:
1. Detecção de ambientes por regex
2. Classificação de equipamentos por keyword
3. Regras de dimensionamento hidráulico
4. Pipeline completo de geração de ramais

---

## Regras Hidráulicas Padrão

| Equipamento | Esgoto Ø | Inclinação | Água Fria Ø |
|-------------|----------|------------|-------------|
| Vaso sanitário | 100 mm | 1% | 25 mm |
| Lavatório | 50 mm | 2% | 25 mm |
| Chuveiro | 50 mm | 2% | 25 mm |
| Pia | 50 mm | 2% | 25 mm |
| Tanque | 50 mm | 2% | 25 mm |
| Ralo | 50 mm | 2% | — |

As regras são configuráveis em `Configuration/HydraulicRules.cs`.

---

## Logs

O plugin gera logs em:
```
%USERPROFILE%\Documents\RevitHydraulicPlugin\Logs\
```

Formato: `RevitHydraulicPlugin_YYYY-MM-DD.log`

---

## Problemas Conhecidos

- O plugin espera que os Rooms tenham nomes reconhecíveis (ex: "Banheiro", "Cozinha"). Nomes genéricos como "Room 1" não serão classificados.
- Os equipamentos devem ser FamilyInstances da categoria **Plumbing Fixtures** com nomes de família que contenham palavras-chave reconhecíveis.
- Pipe Types e System Types devem estar configurados no template do projeto (PVC, Sanitary, Domestic Cold Water).

---

## Contribuindo

1. Faça um Fork do repositório
2. Crie uma branch de feature: `git checkout -b feature/minha-melhoria`
3. Commit suas alterações: `git commit -m "Adiciona nova funcionalidade"`
4. Push para a branch: `git push origin feature/minha-melhoria`
5. Abra um Pull Request

Para reportar bugs, consulte o [Guia de Testes](docs/testing-guide.md).

---

## Licença

Este projeto está licenciado sob a [MIT License](LICENSE).
