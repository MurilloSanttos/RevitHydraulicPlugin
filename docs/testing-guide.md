# Guia de Testes — RevitHydraulicPlugin

Este documento é destinado ao **testador externo** que irá validar o plugin dentro do Autodesk Revit. Contém instruções detalhadas sobre cenários de teste, comportamentos esperados e como reportar problemas.

---

## Índice

1. [Preparação do Ambiente](#preparação-do-ambiente)
2. [Cenários de Teste](#cenários-de-teste)
3. [Comportamentos Esperados](#comportamentos-esperados)
4. [Como Coletar Logs](#como-coletar-logs)
5. [Como Reportar Bugs](#como-reportar-bugs)
6. [Checklist de Testes](#checklist-de-testes)

---

## Preparação do Ambiente

### Antes de começar:

1. Certifique-se de que o plugin está **instalado corretamente** (ver [README.md](../README.md) → "Como Instalar no Revit")
2. Crie uma **cópia do seu projeto** antes de testar (o plugin modifica o modelo)
3. Verifique se o projeto possui:
   - ✅ Rooms criados e nomeados
   - ✅ Plumbing Fixtures inseridos nos Rooms
   - ✅ Pelo menos 2 Levels (níveis) definidos
   - ✅ Piping System Types configurados ("Sanitary" e "Domestic Cold Water")
   - ✅ Pipe Types disponíveis (qualquer tipo serve, ex: "Default", "PVC")

### Projeto de Teste Recomendado:

Crie um projeto simples com:

```
Térreo (Level 0)
  ├── Banheiro (Room)
  │   ├── 1× Toilet (Vaso Sanitário)
  │   ├── 1× Lavatory (Lavatório)
  │   └── 1× Shower (Chuveiro)
  ├── Cozinha (Room)
  │   └── 1× Kitchen Sink (Pia)
  └── Sala (Room)
      └── (sem equipamentos)

1º Pavimento (Level 1, elevação: 3000mm)
  ├── Banheiro Suíte (Room)
  │   ├── 1× Toilet
  │   └── 1× Lavatory
  └── Quarto (Room)
      └── (sem equipamentos)
```

---

## Cenários de Teste

### Teste A — Detecção de Ambientes

**Objetivo:** Verificar se o plugin identifica corretamente os Rooms hidráulicos.

**Passos:**
1. Abra o projeto de teste
2. Vá em `Add-Ins > External Tools > Detectar Ambientes Hidráulicos`
3. Observe o diálogo exibido

**Resultado esperado:**
- ✅ "Banheiro" detectado como **Banheiro**
- ✅ "Cozinha" detectada como **Cozinha**
- ✅ "Banheiro Suíte" detectado como **Banheiro**
- ❌ "Sala" e "Quarto" **NÃO** devem aparecer na lista

**Verifique também:**
- O número total de ambientes detectados está correto?
- Rooms com nomes em inglês ("Bathroom", "Kitchen") também são reconhecidos?
- O command executou sem erros?

---

### Teste B — Identificação de Equipamentos

**Objetivo:** Verificar se o plugin reconhece equipamentos dentro dos Rooms.

**Passos:**
1. Vá em `Add-Ins > External Tools > Identificar Equipamentos Hidráulicos`
2. Observe o relatório detalhado

**Resultado esperado:**
- ✅ Cada equipamento listado com seu tipo correto
- ✅ Equipamentos agrupados por Room
- ✅ Conta total: 5 equipamentos (2 banheiros + cozinha)

**Verifique também:**
- Equipamentos na "Sala" ou "Quarto" **NÃO** devem aparecer
- Os tipos estão corretos? (VasoSanitario, Lavatorio, Chuveiro, Pia)
- As famílias do seu template são reconhecidas?

**⚠️ Atenção:** Se os equipamentos **não forem detectados**, possivelmente:
- As FamilyInstances não são da categoria "Plumbing Fixtures"
- O nome da família não contém palavras-chave reconhecíveis
- Anote o nome exato da família para incluirmos no mapeamento

---

### Teste C — Criação de Colunas

**Objetivo:** Verificar se as colunas hidráulicas são criadas corretamente.

**Passos:**
1. Vá em `Add-Ins > External Tools > Criar Colunas Hidráulicas`
2. **Confirme** quando o diálogo de confirmação aparecer
3. Mude para uma vista 3D

**Resultado esperado:**
- ✅ Colunas verticais visíveis no modelo 3D
- ✅ Uma coluna de água fria (Ø50mm) e uma de esgoto (Ø100mm) por grupo de ambientes
- ✅ As colunas atravessam todos os níveis
- ✅ Posicionadas próximas ao centro dos Rooms hidráulicos

**Se NÃO aparecerem colunas:**
- Verifique os filtros de visibilidade (Pipe está visível na vista?)
- O Piping System Type "Sanitary" e "Domestic Cold Water" existem no projeto?
- Existe pelo menos um Pipe Type disponível?

---

### Teste D — Geração de Ramais

**Objetivo:** Verificar se os ramais conectam equipamentos às colunas.

**Passos:**
1. Vá em `Add-Ins > External Tools > Gerar Ramais Hidráulicos`
2. **Confirme** quando o diálogo aparecer
3. Observe o resultado em vista de planta e 3D

**Resultado esperado:**
- ✅ Cada equipamento conectado à coluna mais próxima
- ✅ Ramais de esgoto com inclinação visível
- ✅ Ramais de água fria horizontais (sem inclinação)
- ✅ Diâmetros corretos: Vaso=Ø100mm, Demais=Ø50mm (esgoto), Todos=Ø25mm (AF)

---

### Teste E — Pipeline Completo

**Objetivo:** Verificar a execução completa de ponta a ponta.

**Passos:**
1. Abra um projeto **LIMPO** (sem colunas/ramais do plugin)
2. Vá em `Add-Ins > External Tools > Automação Hidráulica Completa`
3. **Confirme** quando o diálogo aparecer
4. Leia o relatório de resultado

**Resultado esperado:**
- ✅ Todas as etapas executam em sequência
- ✅ Relatório indica quantos ambientes, equipamentos, colunas e ramais
- ✅ Elementos visíveis no modelo

---

### Teste F — Undo/Redo

**Objetivo:** Verificar que as alterações podem ser desfeitas.

**Passos:**
1. Execute qualquer comando que modifica o modelo
2. Pressione `Ctrl+Z` (Undo)

**Resultado esperado:**
- ✅ Todas as colunas e ramais criados são removidos com um único Undo
- ✅ O modelo volta ao estado anterior

---

## Comportamentos Esperados

### Classificação de Rooms

O plugin reconhece os seguintes nomes (case-insensitive):

| Nome do Room | Classificação |
|-------------|---------------|
| Banheiro, WC, Sanitário, Bathroom, Toilet | Banheiro |
| Lavabo, Powder Room, Half Bath | Lavabo |
| Cozinha, Kitchen, Copa | Cozinha |
| Área de Serviço, A.S., Service Area | Área de Serviço |
| Lavanderia, Laundry | Lavanderia |
| Sala, Quarto, Escritório, etc. | **Ignorado** |

### Classificação de Equipamentos

O plugin reconhece equipamentos por palavras-chave no nome da família:

| Palavra-chave | Tipo |
|---------------|------|
| vaso, toilet, bacia | Vaso Sanitário |
| lavatório, lavatory, sink, basin | Lavatório |
| chuveiro, shower, ducha | Chuveiro |
| pia, kitchen sink | Pia |
| tanque, laundry | Tanque |
| ralo, drain, floor drain | Ralo |

---

## Como Coletar Logs

O plugin gera logs automaticamente em:

```
C:\Users\SEU_USUARIO\Documents\RevitHydraulicPlugin\Logs\
```

Cada dia gera um arquivo: `RevitHydraulicPlugin_YYYY-MM-DD.log`

### Se o Revit travar ou exibir erro:

1. Feche o Revit
2. Navegue até a pasta de logs acima
3. Abra o arquivo do dia atual
4. Copie as últimas linhas (especialmente linhas com `[ERROR]`)

### Logs do Revit

Além dos logs do plugin, os logs do próprio Revit ficam em:

```
C:\Users\SEU_USUARIO\AppData\Local\Autodesk\Revit\Autodesk Revit 20XX\Journals\
```

Envie o arquivo mais recente (ordenado por data) se houver crash.

---

## Como Reportar Bugs

### Ao encontrar um problema, registre:

1. **Título claro** — Ex: "Colunas não aparecem no nível Térreo"

2. **Versão do ambiente:**
   - Versão do Revit (ex: 2025.1)
   - Versão do Windows
   - Template usado (Architecture, MEP, etc.)

3. **Passos para reproduzir:**
   - O que você fez (passo a passo)
   - Qual comando executou
   - Em qual projeto

4. **Resultado esperado** vs **Resultado obtido:**
   - O que deveria ter acontecido
   - O que realmente aconteceu (erro? nada? resultado parcial?)

5. **Evidências:**
   - Screenshot do erro ou do modelo
   - Arquivo de log do dia (ver seção acima)
   - Se possível, o arquivo `.rvt` usado no teste

### Modelo de Issue

```markdown
## Bug: [Título]

**Ambiente:**
- Revit: 2025
- Windows: 11
- Template: Architecture

**Passos para reproduzir:**
1. Abri o projeto "Teste_Hidraulico.rvt"
2. Executei "Automação Hidráulica Completa"
3. Confirmei a execução
4. [Descreva o que aconteceu]

**Esperado:** Colunas e ramais criados no modelo
**Obtido:** Mensagem de erro "PipeType não encontrado"

**Anexos:**
- [Screenshot]
- [Arquivo de log]
```

---

## Checklist de Testes

Use esta lista para rastrear o progresso dos testes:

```
[ ] Teste A — Detecção de Ambientes
    [ ] Rooms hidráulicos reconhecidos corretamente
    [ ] Rooms não-hidráulicos ignorados
    [ ] Nomes em português funcionam
    [ ] Nomes em inglês funcionam

[ ] Teste B — Identificação de Equipamentos
    [ ] Equipamentos reconhecidos por tipo
    [ ] Agrupamento por Room correto
    [ ] Equipamentos fora de Rooms hidráulicos ignorados

[ ] Teste C — Criação de Colunas
    [ ] Colunas criadas nos locais corretos
    [ ] Colunas atravessam todos os níveis
    [ ] Diâmetros corretos (AF=50mm, ES=100mm)

[ ] Teste D — Geração de Ramais
    [ ] Ramais de esgoto com inclinação
    [ ] Ramais de água fria horizontais
    [ ] Diâmetros corretos por equipamento
    [ ] Conecta ao equipamento correto

[ ] Teste E — Pipeline Completo
    [ ] Todas as etapas executam sem erro
    [ ] Relatório exibido ao final
    [ ] Modelo modificado corretamente

[ ] Teste F — Undo/Redo
    [ ] Undo remove todas as modificações
    [ ] Modelo retorna ao estado anterior

[ ] Observações Gerais
    [ ] Plugin aparece na aba Add-Ins
    [ ] Diálogos de confirmação funcionam
    [ ] Sem crashes ou travamentos
    [ ] Logs gerados corretamente
```

---

## Contato

Ao encontrar problemas ou ter dúvidas, entre em contato:
- Abra uma **Issue** no repositório do GitHub
- Inclua o máximo de informações possível (logs, screenshots, passos)
