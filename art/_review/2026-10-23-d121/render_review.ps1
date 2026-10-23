$ErrorActionPreference = 'Stop'
$reviewRoot = $PSScriptRoot
$chromeExe = 'C:/Program Files/Google/Chrome/Application/chrome.exe'
$renderProfile = Join-Path $env:LOCALAPPDATA 'PitCrew-D121-Headless'
$pages = @(
    @('mockup-orders',1280,720), @('mockup-call',1280,720),
    @('mockup-packing',1280,720), @('mockup-receipt',1280,720),
    @('mockup-overview',2560,1440), @('cargo-sheet',1600,1510)
)
foreach ($page in $pages) {
    $destination = Join-Path $reviewRoot ($page[0]+'.png')
    $sourceUri = ([Uri](Join-Path $reviewRoot ($page[0]+'.html'))).AbsoluteUri
    $arguments = @('--headless','--disable-gpu','--no-first-run','--hide-scrollbars','--allow-file-access-from-files',
        ('--user-data-dir="'+$renderProfile+'"'), ('--window-size='+$page[1]+','+$page[2]),
        ('--screenshot="'+$destination+'"'),'--virtual-time-budget=2000',$sourceUri)
    Start-Process -FilePath $chromeExe -ArgumentList $arguments -Wait -WindowStyle Hidden
    if (!(Test-Path -LiteralPath $destination)) { throw "PNG missing: $destination" }
}
