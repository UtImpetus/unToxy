set msbuildpath=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c%%a%%b)
For /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a%%b)
%msbuildpath% /property:Configuration=Release;Platform=x86 ./../Toxy.sln
%msbuildpath% /property:Configuration=Release;Platform=x64 ./../Toxy.sln
XCOPY ..\Toxy\bin\x86\Release unToxy_x86\ /s /e
XCOPY ..\Toxy\bin\x64\Release unToxy_x64\ /s /e
del unToxy_x86\data
del unToxy_x86\config.xml
del unToxy_x64\log.txt
del unToxy_x64\data
del unToxy_x64\config.xml
del unToxy_x64\log.txt
zip unToxy_x86_%mydate%_%mytime%.zip unToxy_x86\*
zip unToxy_x64_%mydate%_%mytime%.zip unToxy_x64\*
rmdir unToxy_x86 /s /q
rmdir unToxy_x64 /s /q