# Git Aliases & Helper Functions for CV Analyzer (PowerShell)
# Usage: .\scripts\git-aliases.ps1; then call functions directly in session.

function gsts { git status -sb }
function glg  { git log --oneline --graph --decorate --all }
function gco  { param($branch) git switch $branch }
function gnew { param($branch) git switch -c $branch }
function gsync { git fetch origin; git rebase origin/main }
function gundo { param($n=1) git reset HEAD~$n }
function gdiff { git diff --word-diff }
function gstage { git add -p }
function gunstage { param($path='.') git restore --staged $path }
function glast { git log -1 --stat }
function gtags { git tag --list --sort=-creatordate }
function grelease { param($tag, $msg) git tag -a $tag -m $msg; git push origin $tag }

# Show commit template path if configured
function gtemplate { git config commit.template }

Write-Host "Loaded git helper aliases/functions." -ForegroundColor Green