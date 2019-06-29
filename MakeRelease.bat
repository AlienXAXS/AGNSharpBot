@echo off
echo Removing old release dir
rmdir /Q /S .\Release_Builder > NUL

echo Creating Release_Builder directory
mkdir .\Release_Builder > NUL

echo Copying new files...
copy /Y .\AGNSharpBot\bin\Release .\Release_Builder > NUL
mkdir .\Release_Builder\x64 > NUL
mkdir .\Release_Builder\x86 > NUL
copy /Y .\AGNSharpBot\bin\Release\x64\* .\Release_Builder\x64\* > NUL
copy /Y .\AGNSharpBot\bin\Release\x86\* .\Release_Builder\x86\* > NUL

xcopy AGNSharpBot\html_docs Release_Builder\html_docs /E /I

echo Creating plugins directory 
mkdir .\Release_Builder\Plugins > NUL

echo Copying Plugins...

echo   - GameWatcher
copy /Y .\Plugins\GameWatcher\bin\Release\GameWatcher.dll .\Release_Builder\Plugins

echo   - Auditor
copy /Y .\Plugins\Auditer\bin\Release\Auditor.dll .\Release_Builder\Plugins

echo   - HomeLabReporting
copy /Y .\Plugins\HomeLabReporting\bin\Release\HomeLabReporting.dll .\Release_Builder\Plugins
copy /Y .\Plugins\HomeLabReporting\bin\Release\SnmpSharpNet.dll .\Release_Builder\Plugins

echo   - JoinQuitLogger
copy /Y .\Plugins\JoinQuitLogger\bin\Release\JoinQuitLogger.dll .\Release_Builder\Plugins

echo   - Responses (AdminPlugin)
copy /Y .\Plugins\Responses\bin\Release\Responses.dll .\Release_Builder\Plugins

echo   - DiscordMenu (Plugin Reference)
copy /Y .\DiscordMenu\bin\Release\DiscordMenu.dll .\Release_Builder\Plugins

echo   - SpotifyStats
copy /Y .\Plugins\SpotifyStats\bin\Release\SpotifyStats.dll .\Release_Builder\Plugins
echo 
echo Copy complete, release ready in 'Release_Builder' folder