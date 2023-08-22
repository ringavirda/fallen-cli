param(
    # This parameter should point to a fcli instance that needs to be tested.
    [Parameter(Mandatory = $true)]
    [string] $fcli
)
# Priliminary setup.
$ErrorActionPreference = "Stop"
$test_results = @{}
$success = "success"
$failed = "failed"
    
<#
    Fail test and exit.
#>
function Exit-TestFailed {
    process {
        Write-Host "E2E Tests Failed!" -ForegroundColor Red
        exit
    }
}

<#
    Test template function.
#>
function Invoke-Test {
    param (
        [Parameter(Mandatory = $true)]
        [int] $test_number,
        [Parameter(Mandatory = $true)]
        $fcli_result,
        [Parameter(Mandatory = $true)]
        [string] $expected
    )
    if (-not (($fcli_result) | Select-String -SimpleMatch $expected)) {
        $test_results[$test_number] = $failed
    }
    else {
        $test_results[$test_number] = $success
    }
}

# Validate the given path.
Write-Host "Validating given path..." -ForegroundColor Green
if (-not (Test-Path -Path $fcli)) {
    
    Write-Host "This path is not valid." -ForegroundColor Red
    Write-Host $fcli -ForegroundColor Red
    Exit-TestFailed
}
else {
    Write-Host "Path $fcli is validated.`nTrying to evaluate fcli."
    if (-not ((& $fcli) -match "fcli")) {
        Write-Host "Given path didn't point to a fcli instance!"
        Exit-TestFailed
    }
}

# Setup fcli for testing.
try {
    $test_path = Join-Path -Path (Get-Location) -ChildPath "test"
    (& $fcli config --path $test_path) > $null
}
catch {
    Write-Host $_.Exception.Message
    Exit-TestFailed
}
# Setup the test file.
$testfile_fullname = "test.ps1"
$testfile_name = $testfile_fullname.Split('.')[0]
$testfile_message = "Test Succeded"
$testfile_path = Join-Path -Path $test_path -ChildPath $testfile_fullname 
New-Item -Path $testfile_path -type "file" `
    -Value "Write-Host '$testfile_message'" -Force > $null

# Perform actual testing.
Write-Host "FCli Command E2E testing start..." -ForegroundColor Green
try {
    # Test command creation.
    Invoke-Test 0 (& $fcli add $testfile_path) "saved"
    # Test listing the commands.
    Invoke-Test 1 (& $fcli list $testfile_name) $testfile_name
    # Test displaying the command's details.
    Invoke-Test 2 (& $fcli change $testfile_name) $testfile_path
    # Test command invocation.
    Invoke-Test 4 (& $fcli $testfile_name) $testfile_message
    # Test command deletion.
    Invoke-Test 5 (& $fcli remove $testfile_name --yes) "deleted"
}
catch {
    Write-Host $_.Exception.Message
    Exit-TestFailed
}
finally {
    # Cleanup the test files and restore fcli config.
    Remove-Item $test_path -Force -Recurse
    (& $fcli config --path default) > $null 
}

# Write report.
Write-Host "Tests completed. Compiling results:" -ForegroundColor Green
$test_results.Keys | Sort-Object | ForEach-Object -Process {
    $result = $test_results[$_]
    Write-Host "Test $_ : $result"
}
if ($test_results.Values -contains $failed) {
    Write-Host "Testing has failed!" -ForegroundColor Red
} else {
    Write-Host "Tests have completed successfully!" -ForegroundColor Green
}
