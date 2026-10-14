#!/usr/bin/env pwsh
#requires -Version 7.0
<#
.SYNOPSIS
    End-to-end smoke test for GameScoringAPI.

.DESCRIPTION
    Exercises a running (deployed or local) API end to end:
      * Games CRUD (POST/GET/PUT/DELETE /game)
      * Match creation (POST /match)
      * The PL/pgSQL triggers that maintain Games.MatchesCount and Matches.PlayerCount

    The created game is named SMOKE_<timestamp> and is deleted at the end - in a
    finally block, so a mid-run failure still cleans up. Every other call is a
    read or operates only on the game this script created.

    Exit code is 0 when every step passes, non-zero otherwise.

.PARAMETER BaseUrl
    Base URL of the API, e.g. https://gamescoringapi.azurewebsites.net or
    https://localhost:7191.

.PARAMETER TimeoutSec
    Per-request timeout in seconds. Default 30.

.PARAMETER SkipCertificateCheck
    Skip TLS certificate validation (needed for the local self-signed dev cert).

.EXAMPLE
    pwsh ./scripts/smoke-test.ps1 -BaseUrl https://localhost:7191 -SkipCertificateCheck

.EXAMPLE
    pwsh ./scripts/smoke-test.ps1 -BaseUrl https://gamescoringapi.azurewebsites.net
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [int]$TimeoutSec = 30,

    [switch]$SkipCertificateCheck
)

$ErrorActionPreference = 'Stop'

$BaseUrl = $BaseUrl.TrimEnd('/')

$script:Failures = 0
$script:GameId = $null

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [hashtable]$Body
    )

    $params = @{
        Method             = $Method
        Uri                = "$BaseUrl$Path"
        TimeoutSec         = $TimeoutSec
        SkipHttpErrorCheck = $true
    }
    if ($SkipCertificateCheck) { $params.SkipCertificateCheck = $true }

    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }

    $response = Invoke-WebRequest @params

    $json = $null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
        try { $json = $response.Content | ConvertFrom-Json } catch { $json = $null }
    }

    [pscustomobject]@{
        Status = [int]$response.StatusCode
        Body   = $json
        Raw    = $response.Content
    }
}

function Record {
    param(
        [Parameter(Mandatory = $true)][string]$Step,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [string]$Detail = ''
    )

    if ($Passed) {
        Write-Host ("[PASS] {0}  {1}" -f $Step, $Detail) -ForegroundColor Green
    }
    else {
        Write-Host ("[FAIL] {0}  {1}" -f $Step, $Detail) -ForegroundColor Red
        $script:Failures++
    }
}

Write-Host "GameScoringAPI smoke test against $BaseUrl" -ForegroundColor Cyan
Write-Host ('=' * 64)

try {
    # 1. Health
    $r = Invoke-Api -Method 'GET' -Path '/health'
    Record -Step '1. GET /health' -Passed ($r.Status -eq 200 -and $r.Body.status -eq 'Healthy') -Detail "status=$($r.Status) body=$($r.Raw)"

    # 2. Create game
    $gameName = "SMOKE_$(Get-Date -Format 'yyyyMMddHHmmss')"
    $r = Invoke-Api -Method 'POST' -Path '/game' -Body @{
        gameName        = $gameName
        gameDescription = 'Automated smoke test - safe to delete'
        minPlayers      = 2
        maxPlayers      = 8
        averageDuration = 60
    }
    $gameCreated = ($r.Status -eq 201 -and $null -ne $r.Body -and $null -ne $r.Body.id)
    Record -Step '2. POST /game' -Passed $gameCreated -Detail "status=$($r.Status) id=$($r.Body.id)"
    if ($gameCreated) { $script:GameId = [int]$r.Body.id }

    # 3. Read the game back
    if ($script:GameId) {
        $r = Invoke-Api -Method 'GET' -Path "/game/$($script:GameId)"
        Record -Step '3. GET /game/{id}' -Passed ($r.Status -eq 200 -and $r.Body.gameName -eq $gameName) -Detail "status=$($r.Status) gameName=$($r.Body.gameName)"
    }
    else {
        Record -Step '3. GET /game/{id}' -Passed $false -Detail 'skipped - no game id'
    }

    # 4. Update the game (this endpoint returns 200 with the updated game, not 204)
    if ($script:GameId) {
        $updatedName = "$gameName-updated"
        $r = Invoke-Api -Method 'PUT' -Path "/game/$($script:GameId)" -Body @{
            gameName        = $updatedName
            gameDescription = 'Updated by smoke test'
            minPlayers      = 2
            maxPlayers      = 10
            averageDuration = 45
        }
        Record -Step '4. PUT /game/{id}' -Passed ($r.Status -eq 200 -and $r.Body.gameName -eq $updatedName) -Detail "status=$($r.Status) gameName=$($r.Body.gameName)"
    }
    else {
        Record -Step '4. PUT /game/{id}' -Passed $false -Detail 'skipped - no game id'
    }

    # 5. List games
    $r = Invoke-Api -Method 'GET' -Path '/games'
    Record -Step '5. GET /games' -Passed ($r.Status -eq 200) -Detail "status=$($r.Status)"

    # 6. Create a match for the game
    $matchId = $null
    if ($script:GameId) {
        $r = Invoke-Api -Method 'POST' -Path '/match' -Body @{
            gameId     = $script:GameId
            matchDate  = (Get-Date).ToUniversalTime().ToString('o')
            notes      = 'Smoke test match'
            isFinished = $false
        }
        $matchCreated = ($r.Status -eq 201 -and $null -ne $r.Body -and $null -ne $r.Body.matchId)
        Record -Step '6. POST /match' -Passed $matchCreated -Detail "status=$($r.Status) matchId=$($r.Body.matchId)"
        if ($matchCreated) { $matchId = [int]$r.Body.matchId }
    }
    else {
        Record -Step '6. POST /match' -Passed $false -Detail 'skipped - no game id'
    }

    # 7. Trigger: Games.MatchesCount is incremented by the match insert
    if ($script:GameId) {
        $r = Invoke-Api -Method 'GET' -Path "/game/$($script:GameId)"
        Record -Step '7. trigger Games.MatchesCount' -Passed ($r.Status -eq 200 -and $r.Body.matchesCount -ge 1) -Detail "status=$($r.Status) matchesCount=$($r.Body.matchesCount)"
    }
    else {
        Record -Step '7. trigger Games.MatchesCount' -Passed $false -Detail 'skipped - no game id'
    }

    # 8. Create a data point for the match
    if ($matchId) {
        $r = Invoke-Api -Method 'POST' -Path "/match-data-point/$matchId" -Body @{
            playerName        = 'Smoke Tester'
            gamePoints        = 42
            pointsDescription = 'Automated smoke test data point'
        }
        Record -Step '8. POST /match-data-point/{matchId}' -Passed ($r.Status -eq 201) -Detail "status=$($r.Status) id=$($r.Body.id)"
    }
    else {
        Record -Step '8. POST /match-data-point/{matchId}' -Passed $false -Detail 'skipped - no match id'
    }

    # 9. Trigger: Matches.PlayerCount is incremented by the data point insert
    if ($matchId) {
        $r = Invoke-Api -Method 'GET' -Path "/match/$matchId"
        Record -Step '9. trigger Matches.PlayerCount' -Passed ($r.Status -eq 200 -and $r.Body.playerCount -ge 1) -Detail "status=$($r.Status) playerCount=$($r.Body.playerCount)"
    }
    else {
        Record -Step '9. trigger Matches.PlayerCount' -Passed $false -Detail 'skipped - no match id'
    }

    # 10. Delete the game (cascade removes the match + data point) and confirm it is gone
    if ($script:GameId) {
        $idToDelete = $script:GameId
        $deleteResponse = Invoke-Api -Method 'DELETE' -Path "/game/$idToDelete"
        $getAfterDelete = Invoke-Api -Method 'GET' -Path "/game/$idToDelete"
        $deleteOk = ($deleteResponse.Status -eq 204 -and $getAfterDelete.Status -eq 404)
        if ($deleteOk) { $script:GameId = $null }  # confirmed gone; skip the finally cleanup
        Record -Step '10. DELETE /game/{id}' -Passed $deleteOk -Detail "delete=$($deleteResponse.Status) getAfterDelete=$($getAfterDelete.Status)"
    }
    else {
        Record -Step '10. DELETE /game/{id}' -Passed $false -Detail 'skipped - no game id'
    }
}
finally {
    if ($script:GameId) {
        Write-Host "Cleaning up game $($script:GameId)..." -ForegroundColor Yellow
        try { Invoke-Api -Method 'DELETE' -Path "/game/$($script:GameId)" | Out-Null } catch { }
    }
}

Write-Host ('=' * 64)
$total = 10
if ($script:Failures -eq 0) {
    Write-Host "SMOKE TEST PASSED ($total/$total steps)" -ForegroundColor Green
    exit 0
}

Write-Host "SMOKE TEST FAILED ($($total - $script:Failures)/$total steps passed)" -ForegroundColor Red
exit 1
