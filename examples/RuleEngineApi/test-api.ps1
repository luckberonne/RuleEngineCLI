# Test script for RuleEngine API
param(
    [string]$ApiUrl = "http://localhost:5001",
    [string]$RulesPath = "../../examples/rules.json",
    [string]$InputJson = '{"name":"John Doe","age":25,"email":"john@example.com","username":"johndoe","balance":1500.50,"startDate":"2024-01-01","endDate":"2024-12-31","emailDomain":"company.com","transactionAmount":5000,"isActive":true,"isVerified":true}',
    [switch]$TestInvalidData,
    [switch]$TestAdvancedEndpoint
)

if ($TestInvalidData) {
    $InputJson = '{"name":"John Doe","age":15,"username":"","balance":-100,"startDate":"2024-12-31","endDate":"2024-01-01","emailDomain":"gmail.com","transactionAmount":15000,"isActive":false,"isVerified":false}'
    Write-Host "Testing with INVALID data (should FAIL)..." -ForegroundColor Yellow
} elseif ($TestAdvancedEndpoint) {
    Write-Host "Testing ADVANCED endpoint..." -ForegroundColor Cyan
} else {
    Write-Host "Testing with VALID data (should PASS)..." -ForegroundColor Green
}

$endpoint = if ($TestAdvancedEndpoint) { "validate/advanced" } else { "validate" }

$body = @{
    rulesFilePath = $RulesPath
    inputJson = $InputJson
} | ConvertTo-Json

if ($TestAdvancedEndpoint) {
    $bodyObject = $body | ConvertFrom-Json
    $bodyObject | Add-Member -MemberType NoteProperty -Name "onlyEnabledRules" -Value $true
    $body = $bodyObject | ConvertTo-Json
}

Write-Host "URL: $ApiUrl/api/rules/$endpoint" -ForegroundColor Gray
Write-Host "Body: $body" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/rules/$endpoint" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Body:" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    Write-Host "Status: $($result.status) | Passed: $($result.totalPassed) | Failed: $($result.totalFailed)" -ForegroundColor $(if ($result.status -eq "PASS") { "Green" } else { "Red" })
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.ReadToEnd() | ConvertFrom-Json | ConvertTo-Json -Depth 5
    }
}

$body = @{
    rulesFilePath = $RulesPath
    inputJson = $InputJson
} | ConvertTo-Json

Write-Host "Testing RuleEngine API..." -ForegroundColor Cyan
Write-Host "URL: $ApiUrl/api/rules/validate" -ForegroundColor Gray
Write-Host "Body: $body" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/rules/validate" -Method POST -Body $body -ContentType "application/json"
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Body:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.ReadToEnd() | ConvertFrom-Json | ConvertTo-Json -Depth 10
    }
}