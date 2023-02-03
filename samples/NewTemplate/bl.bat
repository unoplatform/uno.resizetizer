@echo Run ..\bl from target projects
@echo Run ..\bl "/p:RuntimeIdentifier=win10-x64" to build windows target

msbuild /bl /t:rebuild %1
.\msbuild.binlog
