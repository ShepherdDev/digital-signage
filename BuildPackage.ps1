#
# Generate a list of files under the given path. Used to generate the list
# of files to delete after building the content directory.
#
function GetFiles($path = $pwd, $dir = "")
{
    foreach ($item in Get-ChildItem $path)
    {
        if (!$item.PSIsContainer)
        {
            $dir + "\" + $item
        }
        if (Test-Path $item.FullName -PathType Container)
        {
            $newdir = $dir + "\" + $item.Name
            GetFiles $item.FullName $newdir
        }
    }
}

#
# Copy any Web User Controls into the staging content directory.
#
function CopyControls($org, $proj)
{
    if (Test-Path "Controls" -PathType Container)
    {
        $result = New-Item ("_Package\content\Plugins\" + $org + "\" + $proj) -ItemType Directory -Force
        $result = Copy-Item "Controls\*" -Destination ("_Package\content\Plugins\" + $org + "\" + $proj + "\") -Recurse
    }
}

#
# Copy any release DLLs into the staging content directory.
#
function CopyDLL()
{
    if (Test-Path "obj\Release\*.dll")
    {
        $result = New-Item ("_Package\content\bin") -ItemType Directory -Force
        $result = Copy-Item "obj\Release\*.dll" -Destination "_Package\content\bin\"
    }
}

#
# Copy any themes into the staging content directory.
#
function CopyThemes()
{
    if ((Test-Path "Themes") -and (Test-Path "Themes\*"))
    {
        $result = New-Item ("_Package\content\Themes") -ItemType Directory -Force
        $result = Copy-Item "Themes\*" -Destination "_Package\content\Themes\"
    }
}

#
# Copy any Webhooks into the staging content directory.
#
function CopyWebhooks()
{
    if ((Test-Path "Themes") -and (Test-Path "Themes\*"))
    {
        $result = New-Item ("_Package\content\Themes") -ItemType Directory -Force
        $result = Copy-Item "Themes\*" -Destination "_Package\content\Themes\"
    }
}

#
# Copy any install/uninstall SQL scripts into the staging directory.
#
function CopySQL($version)
{
    if (Test-Path ("Releases\" + $version + "\install.sql"))
    {
        $result = Copy-Item ("Releases\" + $version + "\install.sql") -Destination "_Package\install\run.sql"
    }

    if (Test-Path ("Releases\" + $version + "\uninstall.sql"))
    {
        $result = Copy-Item ("Releases\" + $version + "\uninstall.sql") -Destination "_Package\uninstall\run.sql"
    }
}

#
# Copy any extra files into the staging directory.
#
function CopyExtra($version)
{
    if (Test-Path ("Releases\" + $version + "\installdelete.txt"))
    {
        $result = Copy-Item ("Releases\" + $version + "\installdelete.txt") -Destination "_Package\install\deletefile.lst"
    }

    if (Test-Path ("Releases\" + $version + "\uninstalldelete.txt"))
    {
        $result = Copy-Item ("Releases\" + $version + "\uninstalldelete.txt") -Destination "_Package\uninstall\deletefile.lst"
    }
}

#
# Setup some useful variables for use later.
#
$ProjectPath = Split-Path (Get-Variable MyInvocation).Value.MyCommand.Path
$ProjectFullName = (Get-ChildItem -Path $ProjectPath -Filter *.csproj).Name
$ProjectFullName = $ProjectFullName.Substring(0, $ProjectFullName.Length - 7)
$ProjectOrganziation = $ProjectFullName.Substring(0, $ProjectFullName.LastIndexOf('.'))
$ProjectName = $ProjectFullName.Substring($ProjectFullName.LastIndexOf('.') + 1)
$ProjectSafeOrganization = $ProjectOrganziation.Replace(".", "_")

#
# Make sure stuff isn't left around from a failed run.
#
if (Test-Path "_Package" -PathType Container)
{
    Write-Host "Error: Package directory already exists."
    return
}

#
# Make sure the Releases directory exists.
#
if (!(Test-Path "Releases" -PathType Container))
{
    $result = New-Item "Releases" -ItemType Directory
}

#
# Get current version number.
#
$lastVersion = Get-ChildItem "Releases/" | ?{ $_.PSIsContainer } | Sort-Object | Select-Object -Last 1
if (!$lastVersion)
{
    Write-Host "No releases have been defined."
    return
}
$version = $lastVersion.Name
Write-Host "Building version 1"

#
# Build the project in Release mode.
#
$msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$proc = Start-Process -Wait -PassThru $msbuild "/P:Configuration=Release"
If ($proc.ExitCode -ne 0)
{
    Write-Host "Build failed."
    return
}

#
# Copy all files into the _Package directory for staging.
#
$result = New-Item "_Package" -ItemType Directory
$result = New-Item "_Package\content" -ItemType Directory
$result = New-Item "_Package\install" -ItemType Directory
$result = New-Item "_Package\uninstall" -ItemType Directory
CopyControls $ProjectSafeOrganization $ProjectName
CopyThemes
CopyWebhooks
CopyDLL
CopySQL $version
CopyExtra $version

#
# Get the list of files being installed and put them in the uninstall list.
#
GetFiles "_Package\content" | Out-File "_Package\uninstall\deletefile.lst" -Encoding ascii -Append

#
# Zip everything up.
#
Compress-Archive -Path "_Package\*" -DestinationPath ("Releases\" + $version + "\" + $ProjectName + ".zip")
If (Test-Path ("Releases\" + $version + "\" + $ProjectName + ".plugin"))
{
    Remove-Item -Path ("Releases\" + $version + "\" + $ProjectName + ".plugin")
}
Rename-Item -Path ("Releases\" + $version + "\" + $ProjectName + ".zip") -NewName ($ProjectName + ".plugin")
Remove-Item "_Package" -Force -Recurse

Write-Host "Package has been built"
