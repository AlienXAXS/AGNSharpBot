@echo off
echo Removing old release dir
rmdir /Q /S C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder > NUL

echo Creating Release_Builder directory
mkdir C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder > NUL

echo Copying new files...
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\AGNSharpBot\bin\Release C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder > NUL

echo Creating plugins directory
mkdir C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins > NUL

echo Copying Plugins...
echo   - GameToRole
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\GameToRole\bin\Release\GameToRole.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins

echo   - HomeLabReporting
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\HomeLabReporting\bin\Release\HomeLabReporting.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\HomeLabReporting\bin\Release\SnmpSharpNet.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins

echo   - JoinQuitLogger
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\JoinQuitLogger\bin\Release\JoinQuitLogger.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins

echo   - Responses (AdminPlugin)
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\Responses\bin\Release\Responses.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins

echo   - DiscordMenu (Plugin Reference)
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\DiscordMenu\bin\Release\DiscordMenu.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins

echo   - SpotifyStats
copy /Y C:\Users\AlienX\source\repos\AGNSharpBot\SpotifyStats\bin\Release\SpotifyStats.dll C:\Users\AlienX\source\repos\AGNSharpBot\Release_Builder\Plugins
echo 
echo Copy complete, release ready in 'Release_Builder' folder