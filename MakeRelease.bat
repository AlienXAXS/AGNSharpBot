@echo off
cd C:\Users\AlienX\Documents\GitHub\AGNSharpBot
echo Removing old release dir
rmdir /Q /S .\Release_Builder > NUL

echo Creating directories
mkdir .\Release_Builder > NUL
mkdir .\Release_Builder\net472 > NUL
mkdir .\Release_Builder\net472\Plugins > NUL
mkdir .\Release_Builder\netstandard2.0 > NUL
mkdir .\Release_Builder\netstandard2.0\Plugins > NUL
mkdir .\Release_Builder\netcoreapp3.1 > NUL
mkdir .\Release_Builder\netcoreapp3.1\Plugins > NUL

echo Copying new files (net472)...
copy /Y .\AGNSharpBot_v2\bin\Release\net472 .\Release_Builder\net472 > NUL
xcopy AGNSharpBot_v2\html_docs Release_Builder\net472\html_docs /E /I

echo Copying new files (netstandard)...
copy /Y .\AGNSharpBot_v2\bin\Release\netcoreapp3.1 .\Release_Builder\netcoreapp3.1 > NUL
xcopy AGNSharpBot_v2\html_docs Release_Builder\netstandard2.0\html_docs /E /I

echo Copying new files (netstandard)...
copy /Y .\AGNSharpBot_v2\bin\Release\netcoreapp3.1 .\Release_Builder\netcoreapp3.1 > NUL
xcopy AGNSharpBot_v2\html_docs Release_Builder\netcoreapp3.1\html_docs /E /I

echo Copying Plugins...

echo   - ASCIIArt
copy /Y .\Plugins\Binaries\ASCIIArt\bin\Release\net472\ASCIIArt.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\ASCIIArt\bin\Release\netstandard2.0\ASCIIArt.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\ASCIIArt\bin\Release\netstandard2.0\ASCIIArt.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - CatDog
copy /Y .\Plugins\Binaries\CatDog\bin\Release\net472\CatDog.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\CatDog\bin\Release\netstandard2.0\CatDog.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\CatDog\bin\Release\netstandard2.0\CatDog.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - GameGiveaway
copy /Y .\Plugins\Binaries\GameGiveaway\bin\Release\net472\GameGiveaway.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\GameGiveaway\bin\Release\netstandard2.0\GameGiveaway.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\GameGiveaway\bin\Release\netstandard2.0\GameGiveaway.dll .\Release_Builder\netcoreapp3.1\Plugins

echo   - GameWatcher
copy /Y .\Plugins\Binaries\GameWatcher\bin\Release\net472\GameWatcher.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\GameWatcher\bin\Release\netstandard2.0\GameWatcher.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\GameWatcher\bin\Release\netstandard2.0\GameWatcher.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - Auditor
copy /Y .\Plugins\Binaries\Auditor\bin\Release\net472\Auditor.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\Auditor\bin\Release\netstandard2.0\Auditor.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\Auditor\bin\Release\netstandard2.0\Auditor.dll .\Release_Builder\netcoreapp3.1\Plugins

echo   - HomeLabReporting
copy /Y .\Plugins\Binaries\HomeLabReporting\bin\Release\net472\HomeLabReporting.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\HomeLabReporting\bin\Release\netstandard2.0\HomeLabReporting.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\HomeLabReporting\bin\Release\netstandard2.0\HomeLabReporting.dll .\Release_Builder\netcoreapp3.1\Plugins

echo   - Responses (AdminPlugin)
copy /Y .\Plugins\Binaries\Responses\bin\Release\net472\Responses.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\Responses\bin\Release\netstandard2.0\Responses.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\Responses\bin\Release\netstandard2.0\Responses.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - SpotifyStats
copy /Y .\Plugins\Binaries\SpotifyStats\bin\Release\net472\SpotifyStats.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\SpotifyStats\bin\Release\netstandard2.0\SpotifyStats.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\SpotifyStats\bin\Release\netstandard2.0\SpotifyStats.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - HARATSeATSRPNotification
copy /Y .\Plugins\Binaries\HARATSeATSRP\bin\Release\net472\HARATSeATSRP.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\HARATSeATSRP\bin\Release\netstandard2.0\HARATSeATSRP.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\HARATSeATSRP\bin\Release\netstandard2.0\HARATSeATSRP.dll .\Release_Builder\netcoreapp3.1\Plugins


echo   - PUBGWeekly
copy /Y .\Plugins\Binaries\PUBGWeekly\bin\Release\net472\PUBGWeekly.dll .\Release_Builder\net472\Plugins
copy /Y .\Plugins\Binaries\PUBGWeekly\bin\Release\netstandard2.0\PUBGWeekly.dll .\Release_Builder\netstandard2.0\Plugins
copy /Y .\Plugins\Binaries\PUBGWeekly\bin\Release\netstandard2.0\PUBGWeekly.dll .\Release_Builder\netcoreapp3.1\Plugins

echo 
echo Copy complete, release ready in 'Release_Builder' folder
pause