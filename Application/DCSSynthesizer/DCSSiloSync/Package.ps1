param (
    $Conf = "Release",
	$BaseName = "DCSSiloSync"
)

if ($Conf -eq "Release")
{
    $TargetDir = (Join-Path $PSScriptRoot "bin\$Conf")

    $Ver = (Get-Command (Join-Path $TargetDir "$BaseName.exe")).Version.ToString()

    Compress-Archive -Path (("*.dll", "*.exe", "*.config") | % {Join-Path $TargetDir $_}) -DestinationPath (Join-Path $PSScriptRoot "$($BaseName)V$Ver.zip") -Force
}