@echo off
set "PROJECT_ROOT=%~dp0"

cd dll\task1\server && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task1GUI\bin\Debug\net10.0-windows\task1_server.dll" . && cd /d "%PROJECT_ROOT%"
cd dll\task1\client && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task1GUI\bin\Debug\net10.0-windows\task1_client.dll" . && cd /d "%PROJECT_ROOT%"
cd dll\task2\controller && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task2Controller\bin\Debug\net10.0\task2_controller.dll" . && cd /d "%PROJECT_ROOT%"
cd dll\task2\dispatcher && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task2Client\bin\Debug\net10.0-windows\task2_dispatcher.dll" . && cd /d "%PROJECT_ROOT%"
cd dll\task3\controller && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task3Controller\bin\Debug\net10.0\task3_controller.dll" . && cd /d "%PROJECT_ROOT%"
cd dll\task3\dispatcher && go build -buildmode=c-shared -o "%PROJECT_ROOT%GUI\TimpLab4Sharp\Task3Client\bin\Debug\net10.0-windows\task3_dispatcher.dll" . && cd /d "%PROJECT_ROOT%"

echo Done.