@echo off
REM ==============================================================
REM  RevitHydraulicPlugin - Script de Instalacao
REM ==============================================================
REM
REM  Copia os arquivos compilados para a pasta de Addins do Revit.
REM
REM  Uso:
REM    build\install.bat          (auto-detecta versao do Revit)
REM    build\install.bat 2025     (especifica Revit 2025)
REM
REM  NOTA: Execute como Administrador se necessario.
REM ==============================================================

setlocal enabledelayedexpansion

echo.
echo  ==================================================
echo   RevitHydraulicPlugin - Instalador
echo  ==================================================
echo.

REM Define diretorios
set "ROOT_DIR=%~dp0.."
set "DIST_DIR=%ROOT_DIR%\build\dist"

REM Verifica se o build foi executado
if not exist "%DIST_DIR%\RevitHydraulicPlugin\RevitHydraulicPlugin.dll" (
    echo  [ERRO] Arquivos de build nao encontrados.
    echo         Execute primeiro:  build\build.bat
    exit /b 1
)

REM Auto-detecta ou recebe versao do Revit
set "REVIT_YEAR=%~1"

if "%REVIT_YEAR%"=="" (
    echo  [INFO] Auto-detectando versao do Revit...

    if exist "C:\Program Files\Autodesk\Revit 2026" (
        set "REVIT_YEAR=2026"
    ) else if exist "C:\Program Files\Autodesk\Revit 2025" (
        set "REVIT_YEAR=2025"
    ) else if exist "C:\Program Files\Autodesk\Revit 2024" (
        set "REVIT_YEAR=2024"
    ) else (
        echo  [ERRO] Nenhuma instalacao do Revit encontrada.
        echo         Especifique a versao: build\install.bat 2025
        exit /b 1
    )
)

echo  [INFO] Versao detectada: Revit %REVIT_YEAR%

set "ADDINS_DIR=C:\ProgramData\Autodesk\Revit\Addins\%REVIT_YEAR%"
set "PLUGIN_DIR=%ADDINS_DIR%\RevitHydraulicPlugin"

REM Verifica se a pasta de Addins existe
if not exist "%ADDINS_DIR%" (
    echo  [ERRO] Pasta de Addins nao encontrada:
    echo         %ADDINS_DIR%
    echo.
    echo  Verifique se o Revit %REVIT_YEAR% esta instalado.
    exit /b 1
)

echo.
echo  Instalando em:
echo    %ADDINS_DIR%
echo.

REM Cria pasta do plugin
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"

REM Copia arquivos
echo  [1/2] Copiando RevitHydraulicPlugin.dll...
copy /y "%DIST_DIR%\RevitHydraulicPlugin\RevitHydraulicPlugin.dll" "%PLUGIN_DIR%\" >nul

echo  [2/2] Copiando RevitHydraulicPlugin.addin...
copy /y "%DIST_DIR%\RevitHydraulicPlugin.addin" "%ADDINS_DIR%\" >nul

echo.
echo  ==================================================
echo  INSTALACAO CONCLUIDA
echo  ==================================================
echo.
echo  Arquivos instalados:
echo    %ADDINS_DIR%\RevitHydraulicPlugin.addin
echo    %PLUGIN_DIR%\RevitHydraulicPlugin.dll
echo.
echo  Proximo passo:
echo    1. Abra (ou reinicie) o Autodesk Revit %REVIT_YEAR%
echo    2. Abra um projeto com Rooms e Plumbing Fixtures
echo    3. Va em: Add-Ins - External Tools
echo    4. Execute "Automacao Hidraulica Completa"
echo.

exit /b 0
