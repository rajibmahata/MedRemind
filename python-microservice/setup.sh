#!/bin/bash
# Setup script for Python microservice (Unix/macOS)

echo "?? Setting up MedRemind Python Microservice..."

# Check if uv is installed
if ! command -v uv &> /dev/null; then
    echo "?? Installing uv..."
    curl -LsSf https://astral.sh/uv/install.sh | sh
    export PATH="$HOME/.cargo/bin:$PATH"
fi

# Create virtual environment
echo "?? Creating virtual environment..."
uv venv

# Activate virtual environment
echo "? Activating virtual environment..."
source .venv/bin/activate

# Install dependencies
echo "?? Installing dependencies..."
uv pip install -r requirements.txt

# Create .env from example
if [ ! -f .env ]; then
    echo "?? Creating .env file..."
    cp .env.example .env
    echo "??  Please edit .env and add your API keys!"
fi

# Create storage directory
mkdir -p storage/parsed_prescriptions

echo ""
echo "? Setup complete!"
echo ""
echo "Next steps:"
echo "1. Edit .env and add your API keys"
echo "2. Run: source .venv/bin/activate"
echo "3. Run: uvicorn app.main:app --reload"
echo "4. Visit: http://localhost:8000/docs"
