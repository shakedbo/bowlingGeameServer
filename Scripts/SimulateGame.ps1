# Bowling Game Simulator Script
# Usage: .\SimulateGame.ps1 -PlayerName "John"

param(
    [Parameter(Mandatory=$true)]
    [string]$PlayerName,
    
    [string]$BaseUrl = "http://localhost:5254"
)

$ErrorActionPreference = "Stop"

function Write-FrameHeader {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host "            BOWLING GAME SIMULATOR                              " -ForegroundColor Cyan
    Write-Host "================================================================" -ForegroundColor Cyan
}

function Get-RandomPins {
    param([int]$MaxPins = 10)
    return Get-Random -Minimum 0 -Maximum ($MaxPins + 1)
}

function Show-Roll {
    param(
        [int]$Frame,
        [int]$RollInFrame,
        [int]$Pins,
        [int]$RemainingPins
    )
    
    if ($Pins -eq 10 -and $RollInFrame -eq 1) {
        $pinDisplay = "X (STRIKE!)"
    }
    elseif ($RemainingPins -eq 0 -and $RollInFrame -eq 2) {
        $pinDisplay = "/ (SPARE!)"
    }
    else {
        $pinDisplay = "$Pins pins"
    }
    
    Write-Host "  Frame $Frame, Roll $RollInFrame : $pinDisplay" -ForegroundColor Yellow
}

# Main Script
Write-FrameHeader
Write-Host ""
Write-Host "Player: $PlayerName" -ForegroundColor Green
Write-Host "Starting new game..." -ForegroundColor Gray
Write-Host ""

# Start a new game
$body = @{ playerName = $PlayerName } | ConvertTo-Json
$createResponse = $null
try {
    $createResponse = Invoke-RestMethod -Uri "$BaseUrl/api/games" -Method Post -ContentType "application/json" -Body $body
}
catch {
    Write-Host "Failed to create game: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$gameId = $createResponse.id
Write-Host "Game created with ID: $gameId" -ForegroundColor Green
Write-Host ""
Write-Host "----------------------------------------------------------------" -ForegroundColor DarkGray

# Simulate the game
$frame = 1
$rollInFrame = 1
$pinsRemaining = 10
$isComplete = $false

while (-not $isComplete) {
    # Determine max pins for this roll
    $maxPins = $pinsRemaining
    
    # Random roll
    $pins = Get-RandomPins -MaxPins $maxPins
    
    # Show the roll
    Show-Roll -Frame $frame -RollInFrame $rollInFrame -Pins $pins -RemainingPins ($pinsRemaining - $pins)
    
    # Send roll to API
    $rollBody = @{ pins = $pins } | ConvertTo-Json
    $rollResponse = $null
    try {
        $rollResponse = Invoke-RestMethod -Uri "$BaseUrl/api/games/$gameId/roll" -Method Post -ContentType "application/json" -Body $rollBody
    }
    catch {
        Write-Host "Failed to record roll: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    
    $isComplete = $rollResponse.isComplete
    $currentScore = $rollResponse.score
    $currentFrame = $rollResponse.currentFrame
    
    Write-Host "    Current Score: $currentScore" -ForegroundColor Cyan
    
    # Update frame tracking
    if ($frame -lt 10) {
        # Frames 1-9
        if ($pins -eq 10) {
            # Strike - move to next frame
            $frame++
            $rollInFrame = 1
            $pinsRemaining = 10
            Write-Host ""
        }
        elseif ($rollInFrame -eq 2) {
            # Second roll done - move to next frame
            $frame++
            $rollInFrame = 1
            $pinsRemaining = 10
            Write-Host ""
        }
        else {
            # First roll, not a strike
            $rollInFrame = 2
            $pinsRemaining = 10 - $pins
        }
    }
    else {
        # Frame 10 - special rules
        if ($rollInFrame -eq 1) {
            if ($pins -eq 10) {
                # Strike in 10th - pins reset
                $rollInFrame = 2
                $pinsRemaining = 10
            }
            else {
                $rollInFrame = 2
                $pinsRemaining = 10 - $pins
            }
        }
        elseif ($rollInFrame -eq 2) {
            $firstRollPins = 10 - $pinsRemaining
            if ($pinsRemaining -eq 10) {
                # Previous was a strike
                if ($pins -eq 10) {
                    # Another strike
                    $rollInFrame = 3
                    $pinsRemaining = 10
                }
                else {
                    $rollInFrame = 3
                    $pinsRemaining = 10 - $pins
                }
            }
            elseif (($pinsRemaining - $pins) -eq 0 -or $firstRollPins -eq 10) {
                # Spare or strike before
                $rollInFrame = 3
                $pinsRemaining = 10
            }
            else {
                # Open frame in 10th - game should be complete
                $rollInFrame = 3
            }
        }
        else {
            $rollInFrame = 4
        }
    }
    
    Start-Sleep -Milliseconds 200
}

Write-Host ""
Write-Host "----------------------------------------------------------------" -ForegroundColor DarkGray

# Get final score
$scoreResponse = $null
try {
    $scoreResponse = Invoke-RestMethod -Uri "$BaseUrl/api/games/$gameId/score" -Method Get
}
catch {
    Write-Host "Failed to get final score: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "                      GAME COMPLETE!                            " -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

Write-Host ""
Write-Host "  Player: $($scoreResponse.playerName)" -ForegroundColor Yellow

$finalScore = $scoreResponse.score
if ($finalScore -eq 300) {
    Write-Host "  Final Score: $finalScore - PERFECT GAME!" -ForegroundColor Magenta
}
elseif ($finalScore -ge 200) {
    Write-Host "  Final Score: $finalScore - Excellent!" -ForegroundColor Green
}
elseif ($finalScore -ge 150) {
    Write-Host "  Final Score: $finalScore - Good game!" -ForegroundColor Cyan
}
else {
    Write-Host "  Final Score: $finalScore" -ForegroundColor White
}

Write-Host "  Rolls: $($scoreResponse.rolls -join ', ')" -ForegroundColor Gray

# Show leaderboard
Write-Host ""
Write-Host "----------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host ""
Write-Host "LEADERBOARD (Top 5)" -ForegroundColor Magenta

$leaderboard = $null
try {
    $leaderboard = Invoke-RestMethod -Uri "$BaseUrl/api/games/leaderboard?count=5" -Method Get
}
catch {
    Write-Host "  Could not load leaderboard." -ForegroundColor Gray
    $leaderboard = @()
}

if ($leaderboard.Count -eq 0) {
    Write-Host "  No completed games yet." -ForegroundColor Gray
}
else {
    foreach ($entry in $leaderboard) {
        $rank = $entry.rank
        if ($rank -eq 1) { $medal = "[1st]" }
        elseif ($rank -eq 2) { $medal = "[2nd]" }
        elseif ($rank -eq 3) { $medal = "[3rd]" }
        else { $medal = "[${rank}th]" }
        
        Write-Host "  $medal $($entry.playerName): $($entry.score) pts" -ForegroundColor Cyan
    }
}

Write-Host ""
