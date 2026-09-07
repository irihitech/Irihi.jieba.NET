#Requires -Version 5.1
<#
.SYNOPSIS
  Run the fork-vs-upstream jieba.NET benchmark and print a combined comparison.

.EXAMPLE
  .\run.ps1                          # build + run both sides + comparison table
  .\run.ps1 -Side fork               # only this repo's library
  .\run.ps1 -Filter "*medium*"       # BDN filter for a scenario subset
#>
param(
    [ValidateSet("both", "fork", "upstream")]
    [string]$Side = "both",
    [string]$Filter = "*"
)

$ErrorActionPreference = "Stop"
$benchmarkDir = $PSScriptRoot

function Invoke-Bench([string]$project, [string]$label)
{
    Write-Host ""
    Write-Host "===== Building $label =====" -ForegroundColor Cyan
    dotnet build "$benchmarkDir\$project\$project.csproj" -c Release
    if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }

    Write-Host ""
    Write-Host "===== Running $label (a few minutes) =====" -ForegroundColor Cyan
    Push-Location "$benchmarkDir\$project"
    try
    {
        dotnet run --project "$benchmarkDir\$project\$project.csproj" -c Release --no-build -- --filter "$Filter"
        if ($LASTEXITCODE -ne 0) { throw "benchmark failed: $project" }
    }
    finally
    {
        Pop-Location
    }

    Write-Host ""
    Write-Host "===== $label cold start (3 fresh processes) =====" -ForegroundColor Cyan
    1..3 | ForEach-Object {
        $output = dotnet "$benchmarkDir\$project\bin\Release\net10.0\$project.dll" cold | Select-String "COLD_INIT_MS=(\d+)"
        if ($output.Matches.Success)
        {
            "cold #{0}: {1} ms" -f $_, $output.Matches[0].Groups[1].Value
        }
    }
}

function ConvertTo-Milliseconds([string]$mean)
{
    if ([string]::IsNullOrWhiteSpace($mean)) { return $null }
    if ($mean -match "^([\d.,]+)\s*(\S+)$")
    {
        $value = [double]($Matches[1] -replace ",", "")
        $unit = $Matches[2]
        $micro = [string][char]0x03BC + "s"
        switch ($unit)
        {
            "ns" { return $value / 1e6 }
            "us" { return $value / 1e3 }
            $micro { return $value / 1e3 }
            "ms" { return $value }
            "s" { return $value * 1e3 }
            default { return $value }
        }
    }
    return $null
}

if ($Side -in @("both", "fork")) { Invoke-Bench "JiebaNet.ForkBench" "fork (this repo)" }
if ($Side -in @("both", "upstream")) { Invoke-Bench "JiebaNet.UpstreamBench" "upstream (NuGet jieba.NET 0.42.2)" }

Write-Host ""
Write-Host "===== Raw reports =====" -ForegroundColor Cyan
foreach ($project in @("JiebaNet.ForkBench", "JiebaNet.UpstreamBench"))
{
    $report = Get-ChildItem "$benchmarkDir\$project\BenchmarkDotNet.Artifacts\results" -Filter "*BenchSuite-report-github.md" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($report)
    {
        Write-Host "--- $project ---"
        Get-Content $report.FullName | Out-Host
    }
}

$forkCsv = Get-ChildItem "$benchmarkDir\JiebaNet.ForkBench\BenchmarkDotNet.Artifacts\results" -Filter "*BenchSuite-report.csv" -ErrorAction SilentlyContinue | Select-Object -First 1
$upstreamCsv = Get-ChildItem "$benchmarkDir\JiebaNet.UpstreamBench\BenchmarkDotNet.Artifacts\results" -Filter "*BenchSuite-report.csv" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($forkCsv -and $upstreamCsv)
{
    $fork = Import-Csv $forkCsv.FullName -Encoding UTF8
    $upstream = Import-Csv $upstreamCsv.FullName -Encoding UTF8

    $rows = foreach ($row in $fork)
    {
        $other = $upstream | Where-Object { $_.Method -eq $row.Method }
        if ($null -eq $other) { continue }

        $forkMs = ConvertTo-Milliseconds $row.Mean
        $upstreamMs = ConvertTo-Milliseconds $other.Mean
        [pscustomobject]@{
            "Scenario" = $row.Method
            "fork ms" = [math]::Round($forkMs, 3)
            "upstream ms" = [math]::Round($upstreamMs, 3)
            "upstream / fork" = if ($forkMs -gt 0) { [math]::Round($upstreamMs / $forkMs, 2) } else { $null }
            "fork alloc" = $row.Allocated
            "upstream alloc" = $other.Allocated
        }
    }

    Write-Host ""
    Write-Host "===== Comparison (BDN Mean; ratio > 1 means the fork is faster) =====" -ForegroundColor Green
    $rows | Sort-Object "Scenario" | Format-Table -AutoSize | Out-Host
}
else
{
    Write-Host "Benchmark artifacts not found; run both sides to produce the comparison." -ForegroundColor Yellow
}
