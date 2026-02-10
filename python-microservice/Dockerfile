FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Install system dependencies (including MuPDF for PDF processing)
RUN apt-get update && apt-get install -y \
    gcc \
    libmupdf-dev \
    mupdf-tools \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY app/ ./app/

# Create storage directory
RUN mkdir -p storage/parsed_prescriptions

# Expose port
EXPOSE 8000

# Run application
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
