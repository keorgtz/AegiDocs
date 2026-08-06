<#
.SYNOPSIS
Ejecuta las comprobaciones locales obligatorias de AegiDocs.

.DESCRIPTION
Restaura las dependencias y verifica formato, compilación Release y pruebas de
la solución. La raíz se determina a partir de la ubicación de este script, por
lo que puede invocarse desde cualquier directorio. El script termina en el
primer fallo y conserva el código de salida de dotnet.

.PARAMETER SkipTests
Omite la ejecución de pruebas. Útil únicamente para una comprobación rápida de
formato y compilación; CI y la integración normal no deben usarlo.

.EXAMPLE
.\eng\verify.ps1

.EXAMPLE
& 'C:\ruta\a\AegiDocs\eng\verify.ps1' -SkipTests
#>
[CmdletBinding()]
param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path
$solutionPath = Join-Path -Path $repositoryRoot -ChildPath 'AegiDocs.slnx'

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    Write-Error "No se encontró AegiDocs.slnx en '$repositoryRoot'."
    exit 1
}

function Invoke-DotnetVerificationStep {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    Write-Host "==> $Name"
    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Push-Location -LiteralPath $repositoryRoot

try {
    Invoke-DotnetVerificationStep -Name 'Restaurando dependencias' -Arguments @('restore', $solutionPath)
    Invoke-DotnetVerificationStep -Name 'Verificando formato' -Arguments @('format', $solutionPath, '--verify-no-changes', '--no-restore')
    Invoke-DotnetVerificationStep -Name 'Compilando Release' -Arguments @('build', $solutionPath, '--configuration', 'Release', '--no-restore')

    if (-not $SkipTests) {
        Invoke-DotnetVerificationStep -Name 'Ejecutando pruebas Release' -Arguments @('test', $solutionPath, '--configuration', 'Release', '--no-build', '--no-restore')
    }
    else {
        Write-Host '==> Pruebas omitidas por -SkipTests'
    }
}
finally {
    Pop-Location
}
