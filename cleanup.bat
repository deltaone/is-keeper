@echo off
rd /s /q obj
rd /s /q .vs
for %%X in (.pdb,.config,.vshost.exe,.manifest,.log,.suo,.csproj.user) do del /s /q /a ".\*%%X"
pause

