$fcliPath = "${PSScriptRoot}\..\FCli"

if (Get-Command -Name dotnet.exe -ErrorAction SilentlyContinue) { }
else 
{
    Write-Host "Dotnet is not installed on this machine."
    Write-Host "Aborting..."
    Exit 0
}

dotnet.exe pack $fcliPath
Write-Host "Fcli packed and ready."

if (Get-Command -Name fcli -ErrorAction SilentlyContinue)
{
    dotnet.exe tool uninstall --global fcli
}

dotnet.exe tool install --global --add-source "${fcliPath}\nupkg" FCli
