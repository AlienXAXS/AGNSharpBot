Set-Location "C:\Users\markp\Documents\GitHub\AGNSharpBot"
$currentPath = Get-Location
Write-Host "Script running in location $currentPath"

$plugins = Get-Item -Path ".\Plugins\Binaries\*"
$pluginNames = Get-Item -Path ".\AGNSharpBot_v2\bin\Release\net6.0\Plugins\*" -Filter "*.dll"
$destPath = $currentPath.Path + "\AGNSharpBot_v2\bin\Release\net6.0\"

foreach ( $plugin in $plugins )
{
    $pluginPath = $plugin.FullName + "\bin\Release\net6.0"
    if ( [System.IO.Directory]::Exists($pluginPath) )
    {
        $filesToMain = Get-Item -Path "$pluginPath\*" -Filter "*.dll" | Where-object { $_.Name -ne $($plugin.Name + ".dll") }
        #$pluginFile = Get-Item -Path "$pluginPath\*" -Filter "*.dll" | Where-object { $pluginNames.Name -contains $_.Name }
        $pluginFile = Get-Item -Path "$pluginPath\*" -Filter "*.dll" | Where-object { $_.Name -eq $($plugin.Name + ".dll") }

        foreach ( $file in $filesToMain )
        {
            if ( [System.IO.File]::Exists("$currentPath" + "$destPath$($file.Name)") -eq $false )
            {
                Copy-Item -Path $file.FullName -Destination $destPath
                Write-Host "Copied $($file.Name) library to Main Working Directory";
            }
        }

        foreach ( $file in $pluginFile )
        {
            Copy-Item -Path $file.FullName -Destination "$destPath\Plugins\"
            Write-Host "Copied $($file.Name) library to Plugin Directory";
        }
    }
}