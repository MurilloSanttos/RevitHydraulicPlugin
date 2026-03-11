@echo off
REM ═══════════════════════════════════════════════════════════════
REM  RevitHydraulicPlugin — Script de Build
REM ═══════════════════════════════════════════════════════════════
REM
REM  Este script compila o plugin e copia os arquivos necessários
REM  para a pasta build\dist\, pronta para instalação.
REM
REM  Uso:
REM    build\build.bat
REM
REM  Requisitos:
REM    - .NET SDK instalado (dotnet CLI disponível no PATH)
REM    - Autodesk Revit instalado (para as referências da API)
REM
REM  Você pode especificar a versão do Revit:
REM    set REVIT_API_PATH=C:\Program Files\Autodesk\Revit 2025
REM    build\build.bat
REM ═══════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

echo.
echo  ╔══════════════════════════════════════════════╗
echo  ║   RevitHydraulicPlugin — Build Script        ║
echo  ╚══════════════════════════════════════════════╝
echo.

REM Define diretórios
set "ROOT_DIR=%~dp0.."
set "SRC_DIR=%ROOT_DIR%\RevitHydraulicPlugin"
set "DIST_DIR=%ROOT_DIR%\build\dist"
set "CSPROJ=%SRC_DIR%\RevitHydraulicPlugin.csproj"

REM Verifica se o .csproj existe
if not exist "%CSPROJ%" (
    echo  [ERRO] Arquivo de projeto nao encontrado:
    echo         %CSPROJ%
    exit /b 1
)

REM Limpa pasta de distribuição
echo  [1/4] Limpando pasta de distribuicao...
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%"
mkdir "%DIST_DIR%\RevitHydraulicPlugin"

REM Compila o projeto
echo  [2/4] Compilando o plugin (Release)...
echo.

dotnet build "%CSPROJ%" --configuration Release --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo.
    echo  ══════════════════════════════════════════════
    echo  [ERRO] Falha na compilacao!
    echo.
    echo  Verifique:
    echo    1. O Revit esta instalado?
    echo    2. O caminho do RevitAPI.dll esta correto?
    echo    3. Voce pode definir manualmente:
    echo       set REVIT_API_PATH=C:\Program Files\Autodesk\Revit 2025
    echo  ══════════════════════════════════════════════
    exit /b 1
)

echo.
echo  [3/4] Copiando arquivos para distribuicao...

REM Copia a DLL compilada
copy "%SRC_DIR%\bin\Release\RevitHydraulicPlugin.dll" "%DIST_DIR%\RevitHydraulicPlugin\" >nul 2>&1

if %ERRORLEVEL% neq 0 (
    REM Tenta caminho alternativo com subpasta net48
    copy "%SRC_DIR%\bin\Release\net48\RevitHydraulicPlugin.dll" "%DIST_DIR%\RevitHydraulicPlugin\" >nul 2>&1
)

REM Copia o arquivo .addin
copy "%SRC_DIR%\RevitHydraulicPlugin.addin" "%DIST_DIR%\RevitHydraulicPlugin.addin" >nul

echo  [4/4] Verificando arquivos...

REM Verifica se os arquivos foram copiados
set "DLL_OK=0"
set "ADDIN_OK=0"

if exist "%DIST_DIR%\RevitHydraulicPlugin\RevitHydraulicPlugin.dll" set "DLL_OK=1"
if exist "%DIST_DIR%\RevitHydraulicPlugin.addin" set "ADDIN_OK=1"

echo.
echo  ══════════════════════════════════════════════
echo  RESULTADO DO BUILD
echo  ══════════════════════════════════════════════
echo.

if "%DLL_OK%"=="1" (
    echo    [OK]  RevitHydraulicPlugin.dll
) else (
    echo    [!!]  RevitHydraulicPlugin.dll NAO ENCONTRADA
)

if "%ADDIN_OK%"=="1" (
    echo    [OK]  RevitHydraulicPlugin.addin
) else (
    echo    [!!]  RevitHydraulicPlugin.addin NAO ENCONTRADO
)

echo.
echo  Pasta de distribuicao:
echo    %DIST_DIR%
echo.

if "%DLL_OK%"=="1" if "%ADDIN_OK%"=="1" (
    echo  ✓ BUILD CONCLUIDO COM SUCESSO!
    echo.
    echo  Proximo passo:
    echo    Execute  build\install.bat  para instalar no Revit.
    echo.
    exit /b 0
) else (
    echo  ✗ BUILD INCOMPLETO — verifique os erros acima.
    exit /b 1
)
