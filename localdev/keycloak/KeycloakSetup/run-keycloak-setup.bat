@echo off
echo Starting Keycloak Setup Tool...
echo.

REM Build the project
dotnet build

REM Check if build was successful
if %ERRORLEVEL% neq 0 (
    echo Build failed. Please check the errors above.
    pause
    exit /b 1
)

echo Build successful. Running setup...
echo.

REM Run the setup tool
dotnet run

REM Check if setup was successful
if %ERRORLEVEL% neq 0 (
    echo Setup failed. Please check the errors above.
    pause
    exit /b 1
)

echo.
echo Setup completed successfully!
pause