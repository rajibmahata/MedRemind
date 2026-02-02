@echo off
REM Setup script for Python microservice (Windows)

echo ?? Setting up MedRemind Python Microservice...

REM Check if uv is installed
where uv >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ?? Installing uv...
    powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
)

REM Create virtual environment
echo ?? Creating virtual environment...
uv venv

REM Activate virtual environment
echo ? Activating virtual environment...
call .venv\Scripts\activate.bat

REM Install dependencies
echo ?? Installing dependencies...
uv pip install -r requirements.txt

REM Create .env from example
if not exist .env (
    echo ?? Creating .env file...
    copy .env.example .env
    echo ??  Please edit .env and add your API keys!
)

REM Create storage directory
if not exist storage\parsed_prescriptions mkdir storage\parsed_prescriptions

echo.
echo ? Setup complete!
echo.
echo Next steps:
echo 1. Edit .env and add your API keys
echo 2. Run: .venv\Scripts\activate
echo 3. Run: uvicorn app.main:app --reload
echo 4. Visit: http://localhost:8000/docs

pause
