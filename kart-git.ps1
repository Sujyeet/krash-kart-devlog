# RESEARCH_ML -- Git Control Center
# Interactive terminal app & CLI tool.
# Run interactively: .\kart-git.ps1
# Run commands:      .\kart-git.ps1 save "message" | status | history | revert-to <tag/hash> | reset-hard | etc.

param(
    [Parameter(Position=0)]
    [string]$Command,

    [Parameter(Position=1, ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ProjectRoot

# ============================================================
# STYLE HELPERS
# ============================================================

function Write-Header {
    Clear-Host
    Write-Host ""
    Write-Host "  ##############################################################" -ForegroundColor Cyan
    Write-Host "  ##                                                          ##" -ForegroundColor Cyan
    Write-Host "  ##        K R A S H   K A R T   --  GIT CONTROL           ##" -ForegroundColor Cyan
    Write-Host "  ##                                                          ##" -ForegroundColor Cyan
    Write-Host "  ##############################################################" -ForegroundColor Cyan
    Write-Host ""

    $branch = git branch --show-current 2>$null
    $status = git status --porcelain 2>$null
    if ($status) {
        $dirty = " [UNSAVED CHANGES]"
        $dirtyColor = "Yellow"
    } else {
        $dirty = " [ALL SAVED]"
        $dirtyColor = "Green"
    }
    Write-Host "  Branch : " -NoNewline -ForegroundColor DarkGray
    Write-Host $branch -NoNewline -ForegroundColor White
    Write-Host $dirty -ForegroundColor $dirtyColor
    Write-Divider
    Write-Host ""
}

function Write-Divider {
    Write-Host "  --------------------------------------------------------------" -ForegroundColor DarkGray
}

function Prompt-Input ($label) {
    Write-Host ""
    Write-Host "  > ${label}: " -NoNewline -ForegroundColor Yellow
    $val = Read-Host
    if ($val) { return $val.Trim() }
    return ""
}

function Confirm ($msg) {
    Write-Host ""
    Write-Host "  ! $msg" -ForegroundColor Red
    Write-Host "    Type YES to confirm: " -NoNewline -ForegroundColor DarkGray
    $r = Read-Host
    if (-not $r) { return $false }
    $trimmed = $r.Trim()
    return ($trimmed.ToUpper() -eq "YES" -or $trimmed.ToUpper() -eq "Y")
}

function Press-Any {
    Write-Host ""
    Write-Host "  [ Press ENTER to continue ]" -ForegroundColor DarkGray
    $null = Read-Host
}

# ============================================================
# SAFE RESTORE ENGINE
# ============================================================

function Safe-CheckoutRestoreBranch ($target, $description) {
    Write-Host ""
    Write-Host "  Validating target '$target'..." -ForegroundColor DarkGray
    $exists = git rev-parse --verify $target 2>$null
    if (-not $exists) {
        Write-Host "  Target '$target' not found in Git repository." -ForegroundColor Red
        return $false
    }

    # Check if working copy is dirty
    $dirty = git status --porcelain 2>$null
    if ($dirty) {
        Write-Host ""
        Write-Host "  [!] You have unsaved changes in your working copy." -ForegroundColor Yellow
        Write-Host "      Git cannot safely switch versions while uncommitted edits exist." -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "    [S] Stash changes safely (save them to a restore stash pocket)" -ForegroundColor Cyan
        Write-Host "    [D] Discard all unsaved changes and restore $target cleanly" -ForegroundColor Red
        Write-Host "    [C] Cancel operation" -ForegroundColor DarkGray
        $choice = Prompt-Input "Choose an option [S/D/C]"

        if ($choice -imatch "^s$") {
            $stashName = "Auto-stash before restoring to $target on $(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')"
            git stash push -u -m $stashName 2>&1 | Out-Null
            Write-Host "  Unsaved work saved to stash: $stashName" -ForegroundColor Green
        } elseif ($choice -imatch "^d$") {
            if (Confirm "Are you sure you want to PERMANENTLY DISCARD all unsaved edits?") {
                git reset --hard HEAD 2>&1 | Out-Null
                git clean -fd -e "Paper/" 2>&1 | Out-Null
                Write-Host "  All unsaved edits discarded." -ForegroundColor Yellow
            } else {
                Write-Host "  Cancelled." -ForegroundColor DarkGray
                return $false
            }
        } else {
            Write-Host "  Restore cancelled." -ForegroundColor DarkGray
            return $false
        }
    }

    $safeName = ($target -replace '[^a-zA-Z0-9]', '-').ToLower().TrimEnd('-')
    $restoreBranch = "restore/$safeName"

    $existingBranch = git branch --list $restoreBranch 2>$null
    if ($existingBranch) {
        Write-Host "  Branch '$restoreBranch' already exists. Switching to it..." -ForegroundColor Cyan
        git checkout $restoreBranch
    } else {
        Write-Host "  Creating isolated branch '$restoreBranch' from $target..." -ForegroundColor Cyan
        git checkout -b $restoreBranch $target
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "  SUCCESS: Project is now loaded at save: $target" -ForegroundColor Green
        Write-Host "  Active branch: $restoreBranch" -ForegroundColor Green
        Write-Host "  (To return to active develop branch at any time: git checkout develop)" -ForegroundColor Yellow
        return $true
    } else {
        Write-Host ""
        Write-Host "  ERROR: Git could not checkout '$target'. See above messages." -ForegroundColor Red
        return $false
    }
}

function Safe-DiscardAllUncommittedChanges {
    Write-Host ""
    Write-Host "  DISCARD ALL UNSAVED CHANGES (HARD RESET)" -ForegroundColor Red
    Write-Host "  This will revert every modified script/scene and delete uncommitted files," -ForegroundColor Yellow
    Write-Host "  bringing your working project back to the exact state of your last save." -ForegroundColor Yellow
    Write-Host ""

    if (Confirm "Wipe all current changes and restore to last saved commit?") {
        git reset --hard HEAD
        git clean -fd -e "Paper/"
        Write-Host ""
        Write-Host "  SUCCESS: Working directory restored cleanly to last save (HEAD)." -ForegroundColor Green
        return $true
    } else {
        Write-Host ""
        Write-Host "  Cancelled. No changes were made." -ForegroundColor DarkGray
        return $false
    }
}

# ============================================================
# MAIN MENU
# ============================================================

function Show-MainMenu {
    while ($true) {
        Write-Header

        Write-Host "  MAIN MENU" -ForegroundColor White
        Write-Host ""
        Write-Host "    [1]  Quick Save          Save your current work with a message" -ForegroundColor Cyan
        Write-Host "    [2]  Save History        Browse all past saves, load any of them" -ForegroundColor Cyan
        Write-Host "    [3]  Snapshots           Create and restore named checkpoints" -ForegroundColor Cyan
        Write-Host "    [4]  Features            Start, switch, and merge feature branches" -ForegroundColor Cyan
        Write-Host "    [5]  Sync GitHub         Push or pull from GitHub" -ForegroundColor Cyan
        Write-Host "    [6]  Emergency Revert    Undo files, load saves, or hard-reset" -ForegroundColor Red
        Write-Host "    [0]  Exit" -ForegroundColor DarkGray
        Write-Host ""
        Write-Divider
        $choice = Prompt-Input "Enter a number"

        switch ($choice) {
            "1" { Menu-QuickSave }
            "2" { Menu-History }
            "3" { Menu-Snapshots }
            "4" { Menu-Features }
            "5" { Menu-Sync }
            "6" { Menu-Revert }
            "0" { Clear-Host; Write-Host ""; Write-Host "  Goodbye." -ForegroundColor DarkGray; Write-Host ""; exit }
            default {
                Write-Host "  Invalid option. Try again." -ForegroundColor Red
                Start-Sleep 1
            }
        }
    }
}

# ============================================================
# QUICK SAVE
# ============================================================

function Menu-QuickSave {
    Write-Header
    Write-Host "  QUICK SAVE" -ForegroundColor White
    Write-Host ""

    $status = git status --porcelain 2>$null
    if (-not $status) {
        Write-Host "  Nothing to save -- no files have changed since last save." -ForegroundColor Green
        Press-Any
        return
    }

    Write-Host "  Files changed since last save:" -ForegroundColor DarkGray
    Write-Host ""
    $status | ForEach-Object {
        Write-Host "    $_" -ForegroundColor Yellow
    }
    Write-Host ""

    $msg = Prompt-Input "Describe what you changed (your save message)"
    if ($msg -eq "" -or $msg -eq $null) {
        Write-Host ""
        Write-Host "  Cancelled -- no message entered." -ForegroundColor DarkGray
        Press-Any
        return
    }

    git add -A 2>$null
    git commit -m $msg 2>$null | Out-Null

    Write-Host ""
    Write-Host "  SAVED: $msg" -ForegroundColor Green
    Write-Host "  Your work is recorded." -ForegroundColor DarkGray
    Press-Any
}

# ============================================================
# SAVE HISTORY BROWSER
# ============================================================

function Menu-History {
    while ($true) {
        Write-Header
        Write-Host "  SAVE HISTORY  (most recent 20 saves)" -ForegroundColor White
        Write-Host ""

        $rawLog = git log --pretty=format:"%h|||%ar|||%s" -20 2>$null
        if (-not $rawLog) {
            Write-Host "  No saves found." -ForegroundColor DarkGray
            Press-Any
            return
        }

        $entries = @()
        $i = 1
        foreach ($line in $rawLog) {
            $parts   = $line -split "\|\|\|", 3
            $hash    = $parts[0].Trim()
            $time    = $parts[1].Trim()
            $subject = $parts[2].Trim()
            $entries += [pscustomobject]@{
                Index   = $i
                Hash    = $hash
                Time    = $time
                Subject = $subject
            }

            $num     = "[$i]".PadRight(5)
            $timeStr = $time.PadRight(20)
            Write-Host "    $num" -NoNewline -ForegroundColor Cyan
            Write-Host $timeStr -NoNewline -ForegroundColor DarkGray
            Write-Host $subject -ForegroundColor White
            $i++
        }

        Write-Host ""
        Write-Divider
        Write-Host "  Enter a NUMBER to load that save   |   [0] Back to Main Menu" -ForegroundColor DarkGray
        $choice = Prompt-Input "Your choice"

        if ($choice -eq "0" -or $choice -eq "" -or $choice -eq $null) {
            return
        }

        $num = 0
        if (-not [int]::TryParse($choice, [ref]$num)) {
            Write-Host "  Enter a number from the list above." -ForegroundColor Red
            Start-Sleep 1
            continue
        }

        $idx = $num - 1
        if ($idx -lt 0 -or $idx -ge $entries.Count) {
            Write-Host "  Number out of range." -ForegroundColor Red
            Start-Sleep 1
            continue
        }

        $entry = $entries[$idx]
        Write-Host ""
        Write-Host "  Selected : $($entry.Subject)" -ForegroundColor Yellow
        Write-Host "  Saved    : $($entry.Time)" -ForegroundColor DarkGray
        Write-Host "  ID       : $($entry.Hash)" -ForegroundColor DarkGray
        Write-Host ""

        if (Confirm "Open this save in a restore branch?") {
            Safe-CheckoutRestoreBranch $entry.Hash $entry.Subject
            Press-Any
            return
        } else {
            Write-Host "  Cancelled." -ForegroundColor DarkGray
            Start-Sleep 1
        }
    }
}

# ============================================================
# SNAPSHOTS
# ============================================================

function Menu-Snapshots {
    while ($true) {
        Write-Header
        Write-Host "  SNAPSHOTS  (named save points you can always come back to)" -ForegroundColor White
        Write-Host ""

        $tagLines = git tag --sort=-creatordate 2>$null
        $entries  = @()
        $i = 1

        if ($tagLines) {
            foreach ($tag in $tagLines) {
                $tagMsg  = git tag -l $tag --format="%(subject)" 2>$null
                $tagDate = git tag -l $tag --format="%(creatordate:relative)" 2>$null
                $entries += [pscustomobject]@{
                    Index = $i
                    Tag   = $tag
                    Msg   = $tagMsg
                    Time  = $tagDate
                }

                $num     = "[$i]".PadRight(5)
                $tagStr  = $tag.PadRight(32)
                Write-Host "    $num" -NoNewline -ForegroundColor Cyan
                Write-Host $tagStr -NoNewline -ForegroundColor White
                Write-Host $tagDate -ForegroundColor DarkGray
                $i++
            }
        } else {
            Write-Host "  No snapshots yet." -ForegroundColor DarkGray
            Write-Host "  Create your first one by pressing [N] below." -ForegroundColor DarkGray
        }

        Write-Host ""
        Write-Divider
        Write-Host "    [N]  Create a new snapshot right now" -ForegroundColor Green
        Write-Host "    [#]  Enter a number to restore that snapshot" -ForegroundColor Cyan
        Write-Host "    [0]  Back to Main Menu" -ForegroundColor DarkGray
        $choice = Prompt-Input "Your choice"

        if ($choice -eq "0" -or $choice -eq "" -or $choice -eq $null) {
            return
        }

        if ($choice -imatch "^n$") {
            $name = Prompt-Input "Snapshot name (letters and dashes only, e.g. before-drift-refactor)"
            if ($name -eq "" -or $name -eq $null) { continue }
            $name = ($name -replace '[^a-zA-Z0-9]', '-').ToLower().TrimEnd('-')
            git add -A 2>$null
            git commit -m "snapshot: $name" --allow-empty 2>$null | Out-Null
            git tag -a $name -m "Manual snapshot: $name" 2>$null
            Write-Host ""
            Write-Host "  Snapshot created: $name" -ForegroundColor Green
            Write-Host "  It will appear in this list next time." -ForegroundColor DarkGray
            Press-Any
            continue
        }

        $num = 0
        if (-not [int]::TryParse($choice, [ref]$num)) {
            Write-Host "  Enter N to create, or a number to restore." -ForegroundColor Red
            Start-Sleep 1
            continue
        }

        $idx = $num - 1
        if ($idx -lt 0 -or $idx -ge $entries.Count) {
            Write-Host "  Number out of range." -ForegroundColor Red
            Start-Sleep 1
            continue
        }

        $entry = $entries[$idx]
        Write-Host ""
        Write-Host "  Snapshot : $($entry.Tag)" -ForegroundColor Yellow
        Write-Host "  Created  : $($entry.Time)" -ForegroundColor DarkGray
        Write-Host ""

        if (Confirm "Restore snapshot '$($entry.Tag)'?") {
            Safe-CheckoutRestoreBranch $entry.Tag "Snapshot $($entry.Tag)"
        } else {
            Write-Host "  Cancelled." -ForegroundColor DarkGray
        }
        Press-Any
    }
}

# ============================================================
# FEATURES
# ============================================================

function Menu-Features {
    while ($true) {
        Write-Header
        Write-Host "  FEATURE BRANCHES" -ForegroundColor White
        Write-Host ""
        Write-Host "  Use these when working on a risky new feature." -ForegroundColor DarkGray
        Write-Host "  If it breaks, delete the branch -- develop is safe." -ForegroundColor DarkGray
        Write-Host ""

        $branches = git branch --format="%(refname:short)" 2>$null | Where-Object { $_ -match "^feature/" }
        $entries  = @()
        $i = 1

        if ($branches) {
            Write-Host "  Active feature branches:" -ForegroundColor White
            Write-Host ""
            foreach ($b in $branches) {
                $entries += [pscustomobject]@{ Index=$i; Name=$b }
                $num = "[$i]".PadRight(5)
                Write-Host "    $num $b" -ForegroundColor Cyan
                $i++
            }
        } else {
            Write-Host "  No active feature branches." -ForegroundColor DarkGray
        }

        Write-Host ""
        Write-Divider
        Write-Host "    [N]  Start a new feature branch" -ForegroundColor Green
        Write-Host "    [M]  Merge a finished feature into develop" -ForegroundColor Cyan
        Write-Host "    [S]  Switch to a feature branch" -ForegroundColor Cyan
        Write-Host "    [0]  Back to Main Menu" -ForegroundColor DarkGray
        $choice = Prompt-Input "Your choice"

        if ($choice -eq "0" -or $choice -eq "" -or $choice -eq $null) { return }

        if ($choice -imatch "^n$") {
            $name = Prompt-Input "Feature name (e.g. drift-boost, garage-ui, item-system)"
            if ($name -eq "" -or $name -eq $null) { continue }
            $name = ($name -replace '[^a-zA-Z0-9]', '-').ToLower().TrimEnd('-')
            $current = git branch --show-current 2>$null
            if ($current -ne "develop") {
                git stash 2>$null | Out-Null
                git checkout develop 2>$null | Out-Null
            }
            git checkout -b "feature/$name" 2>$null | Out-Null
            Write-Host ""
            Write-Host "  Created : feature/$name" -ForegroundColor Green
            Write-Host "  You are now working in this isolated branch." -ForegroundColor DarkGray
            Write-Host "  Save as normal. When the feature works, come back and choose Merge." -ForegroundColor Yellow
            Press-Any
            return
        }

        if ($choice -imatch "^s$") {
            if ($entries.Count -eq 0) {
                Write-Host "  No feature branches to switch to." -ForegroundColor DarkGray
                Press-Any; continue
            }
            $num = 0
            [int]::TryParse((Prompt-Input "Enter feature number to switch to"), [ref]$num) | Out-Null
            $idx = $num - 1
            if ($idx -ge 0 -and $idx -lt $entries.Count) {
                git checkout $entries[$idx].Name 2>$null | Out-Null
                Write-Host "  Switched to: $($entries[$idx].Name)" -ForegroundColor Green
            } else {
                Write-Host "  Invalid number." -ForegroundColor Red
            }
            Press-Any; continue
        }

        if ($choice -imatch "^m$") {
            if ($entries.Count -eq 0) {
                Write-Host "  No feature branches to merge." -ForegroundColor DarkGray
                Press-Any; continue
            }
            $num = 0
            [int]::TryParse((Prompt-Input "Enter feature number to merge"), [ref]$num) | Out-Null
            $idx = $num - 1
            if ($idx -lt 0 -or $idx -ge $entries.Count) {
                Write-Host "  Invalid number." -ForegroundColor Red
                Press-Any; continue
            }
            $feature = $entries[$idx].Name
            if (Confirm "Merge '$feature' into develop?") {
                git checkout develop 2>$null | Out-Null
                git merge --no-ff $feature -m "feat: merge $feature into develop" 2>$null | Out-Null
                Write-Host ""
                Write-Host "  Merged : $feature into develop" -ForegroundColor Green
                Write-Host "  The feature branch is kept as a record." -ForegroundColor DarkGray
            } else {
                Write-Host "  Cancelled." -ForegroundColor DarkGray
            }
            Press-Any; continue
        }
    }
}

# ============================================================
# SYNC
# ============================================================

function Menu-Sync {
    Write-Header
    Write-Host "  GITHUB SYNC" -ForegroundColor White
    Write-Host ""
    $branch = git branch --show-current 2>$null
    Write-Host "  Current branch : $branch" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    [1]  Push  --  Upload your saves to GitHub" -ForegroundColor Cyan
    Write-Host "    [2]  Pull  --  Download latest from GitHub" -ForegroundColor Cyan
    Write-Host "    [0]  Back" -ForegroundColor DarkGray
    $choice = Prompt-Input "Your choice"

    switch ($choice) {
        "1" {
            Write-Host ""
            Write-Host "  Uploading to GitHub. This may take a moment..." -ForegroundColor Cyan
            Write-Host ""
            git push origin $branch --follow-tags
            Write-Host ""
            Write-Host "  Upload complete." -ForegroundColor Green
            Press-Any
        }
        "2" {
            Write-Host ""
            Write-Host "  Downloading from GitHub..." -ForegroundColor Cyan
            Write-Host ""
            git pull origin $branch
            Write-Host ""
            Write-Host "  Download complete." -ForegroundColor Green
            Press-Any
        }
        default { return }
    }
}

# ============================================================
# EMERGENCY REVERT
# ============================================================

function Menu-Revert {
    Write-Header
    Write-Host "  EMERGENCY REVERT" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Choose how you want to undo something:" -ForegroundColor White
    Write-Host ""
    Write-Host "    [1]  Undo one file         -- Revert a single script back to last save" -ForegroundColor Yellow
    Write-Host "    [2]  Undo last save        -- Remove the last save record (keep files)" -ForegroundColor Yellow
    Write-Host "    [3]  Jump to old version   -- Open any old save or snapshot safely" -ForegroundColor Yellow
    Write-Host "    [4]  Reset to last save    -- Discard all current uncommitted changes" -ForegroundColor Red
    Write-Host "    [0]  Back" -ForegroundColor DarkGray
    $choice = Prompt-Input "Your choice"

    switch ($choice) {
        "1" {
            Write-Host ""
            Write-Host "  Files changed since last save:" -ForegroundColor DarkGray
            Write-Host ""
            git status --short 2>$null | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
            Write-Host ""
            $file = Prompt-Input "Paste the file path exactly as shown above"
            if ($file -eq "" -or $file -eq $null) { return }
            $file = $file -replace '^[MADRCU? ]+', ''
            git checkout HEAD -- $file 2>$null
            Write-Host ""
            Write-Host "  Reverted : $file" -ForegroundColor Green
            Write-Host "  The file is now back to its last saved version." -ForegroundColor DarkGray
            Press-Any
        }
        "2" {
            if (Confirm "Remove the last save record? Your file changes will NOT be lost.") {
                git reset --soft HEAD~1 2>$null
                Write-Host ""
                Write-Host "  Last save removed. Files are unchanged on disk." -ForegroundColor Green
            } else {
                Write-Host "  Cancelled." -ForegroundColor DarkGray
            }
            Press-Any
        }
        "3" {
            Write-Host ""
            Write-Host "  Named snapshots available:" -ForegroundColor White
            Write-Host ""
            git tag --sort=-creatordate 2>$null | ForEach-Object {
                $d = git tag -l $_ --format="%(creatordate:relative)" 2>$null
                Write-Host ("    " + $_.PadRight(32) + $d) -ForegroundColor Cyan
            }
            Write-Host ""
            Write-Host "  Or enter a Save ID from History (e.g. eafc819)." -ForegroundColor DarkGray
            $target = Prompt-Input "Enter snapshot name or save ID"
            if ($target -eq "" -or $target -eq $null) { return }
            Safe-CheckoutRestoreBranch $target "Manual target $target"
            Press-Any
        }
        "4" {
            Safe-DiscardAllUncommittedChanges
            Press-Any
        }
        default { return }
    }
}

# ============================================================
# COMMAND DISPATCHER (CLI & INTERACTIVE)
# ============================================================

if ($Command) {
    switch -Regex ($Command.ToLower()) {
        "^(save|commit)$" {
            $msg = if ($Arguments) { $Arguments -join " " } else { "Quick save on $(Get-Date -Format 'yyyy-MM-dd HH:mm')" }
            git add -A
            git commit -m $msg
            Write-Host "Saved: $msg" -ForegroundColor Green
        }
        "^status$" {
            git status
        }
        "^history$" {
            git log --oneline -20
        }
        "^(snapshot|tag)$" {
            $name = if ($Arguments) { $Arguments[0] } else { Prompt-Input "Snapshot name" }
            if ($name) {
                $name = ($name -replace '[^a-zA-Z0-9]', '-').ToLower().TrimEnd('-')
                git add -A 2>$null
                git commit -m "snapshot: $name" --allow-empty 2>$null | Out-Null
                git tag -a $name -m "Manual snapshot: $name" 2>$null
                Write-Host "Snapshot created: $name" -ForegroundColor Green
            }
        }
        "^snapshots$" {
            git tag --sort=-creatordate -n1
        }
        "^(new-feature|feature)$" {
            $name = if ($Arguments) { $Arguments[0] } else { Prompt-Input "Feature name" }
            if ($name) {
                $name = ($name -replace '[^a-zA-Z0-9]', '-').ToLower().TrimEnd('-')
                git checkout -b "feature/$name"
                Write-Host "Created branch: feature/$name" -ForegroundColor Green
            }
        }
        "^finish-feature$" {
            $name = if ($Arguments) { $Arguments[0] } else { (git branch --show-current) }
            git checkout develop
            git merge --no-ff $name -m "feat: merge $name into develop"
            Write-Host "Merged $name into develop" -ForegroundColor Green
        }
        "^revert-file$" {
            $file = if ($Arguments) { $Arguments[0] } else { Prompt-Input "File path" }
            if ($file) {
                $file = $file -replace '^[MADRCU? ]+', ''
                git checkout HEAD -- $file
                Write-Host "Reverted: $file" -ForegroundColor Green
            }
        }
        "^revert-last$" {
            git reset --soft HEAD~1
            Write-Host "Reverted last save record (files preserved on disk)." -ForegroundColor Green
        }
        "^(revert-to|restore)$" {
            $target = if ($Arguments) { $Arguments[0] } else { Prompt-Input "Snapshot name or commit hash" }
            if ($target) {
                Safe-CheckoutRestoreBranch $target "Target $target"
            }
        }
        "^(reset-hard|discard-changes)$" {
            Safe-DiscardAllUncommittedChanges
        }
        "^push$" {
            $branch = git branch --show-current
            git push origin $branch --follow-tags
        }
        "^pull$" {
            $branch = git branch --show-current
            git pull origin $branch
        }
        default {
            Write-Host "Unknown command: $Command" -ForegroundColor Red
            Write-Host "Available CLI commands:" -ForegroundColor Yellow
            Write-Host "  .\kart-git.ps1 save 'message'       - Save all current work" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 status              - Check git status" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 history             - View recent saves" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 snapshot <name>     - Create a named milestone" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 snapshots          - List all milestones" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 revert-to <target>  - Open any previous save cleanly" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 reset-hard          - Wipe uncommitted changes and reload last save" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 revert-file <path>  - Restore single file" -ForegroundColor DarkGray
            Write-Host "  .\kart-git.ps1 push / pull         - Sync with GitHub" -ForegroundColor DarkGray
        }
    }
} else {
    Show-MainMenu
}
