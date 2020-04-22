@echo off
set Build="%SYSTEMDRIVE%\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MsBuild.exe"
if exist publish rd /s /q publish
%Build% "NET20/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET40/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET45/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET451/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET452/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET46/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET461/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET462/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET47/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET471/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET472/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
%Build% "NET48/Afx.Data/Afx.Data.csproj" /t:Rebuild /p:Configuration=Release
dotnet build "NETStandard2.0/Afx.Data/Afx.Data.csproj" -c Release 
cd publish
del /q/s *.pdb
del /q/s net20\Afx.Base.dll
del /q/s net20\Afx.Base.xml
pause