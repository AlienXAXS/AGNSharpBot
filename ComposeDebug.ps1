Set-Location ".."
$currentPath = Get-Location
Write-Host "Script running in location $currentPath"

$plugins = Get-Item -Path ".\Plugins\Binaries\*"
$pluginNames = Get-Item -Path ".\AGNSharpBot_v2\bin\Debug\netcoreapp3.1\Plugins\*" -Filter "*.dll"
$destPath = ".\AGNSharpBot_v2\bin\Debug\netcoreapp3.1\"

foreach ( $plugin in $plugins )
{
    $pluginPath = $plugin.FullName + "\bin\Debug\netcoreapp3.1\*"
    if ( [System.IO.Directory]::Exists($pluginPath) )
    {
        $files = Get-Item -Path $pluginPath -Filter "*.dll" | Where-object { $pluginNames.Name -notcontains $_.Name }

        foreach ( $file in $files )
        {
            if ( -not [System.IO.File]::Exists("$destPath\$($file.Name)") )
            {
                Copy-Item -Path $file.FullName -Destination $destPath
                Write-Host "Copied $($file.Name) to main directory";
            }
        }
    }
}