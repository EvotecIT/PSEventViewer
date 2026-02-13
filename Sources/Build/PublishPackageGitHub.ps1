Import-Module PSPublishModule -Force -ErrorAction Stop

$GitHubAccessToken = Get-Content -Raw 'C:\Support\Important\GithubAPI.txt'

$publishGitHubReleaseAssetSplat = @{
    ProjectPath             = "$PSScriptRoot\..\EventViewerX"
    GitHubAccessToken       = $GitHubAccessToken
    GitHubUsername          = "EvotecIT"
    GitHubRepositoryName    = "PSEventViewer"
    IsPreRelease            = $false
    IncludeProjectNameInTag = $true
    GenerateReleaseNotes    = $true
}

Publish-GitHubReleaseAsset @publishGitHubReleaseAssetSplat
