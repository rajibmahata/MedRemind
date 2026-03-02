# Docker Deployment Guide - MedRemind Python API

This guide covers deploying the MedRemind Python Microservice using Docker.

## Prerequisites

- Docker Desktop installed ([Download](https://www.docker.com/products/docker-desktop/))
- OpenAI API key
- (Optional) LangSmith API key for tracing

## Quick Start

### 1. Build the Docker Image

```bash
cd python-microservice
docker build -t medremind-python-api .
```

### 2. Create Environment File

Create a `.env` file with your configuration:

```env
# Required
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_MODEL=gpt-4o-mini
OPENAI_ENABLED=true

# Optional - Additional LLM providers
DEEPSEEK_API_KEY=your_deepseek_key_here
DEEPSEEK_MODEL=deepseek-chat
DEEPSEEK_ENABLED=false

CLAUDE_API_KEY=your_claude_key_here
CLAUDE_MODEL=claude-3-5-sonnet-20241022
CLAUDE_ENABLED=false

# Server Configuration
HOST=0.0.0.0
PORT=8000
LOG_LEVEL=INFO
CORS_ORIGINS=http://localhost:5000,https://localhost:7000

# Storage
STORAGE_PATH=./storage/parsed_prescriptions
ENABLE_FILE_STORAGE=true

# OCR Configuration
OPENAI_VISION_MODEL=gpt-4o

# Tracing (Optional)
LANGCHAIN_TRACING_V2=false
LANGCHAIN_API_KEY=your_langsmith_api_key_here
LANGCHAIN_PROJECT=medremind-prescription-parser
```

### 3. Run the Container

```bash
docker run -d -p 8000:8000 --env-file .env --name medremind-api medremind-python-api
```

## Deployment Options

### Option A: Basic Deployment

```bash
docker run -d \
  -p 8000:8000 \
  --env-file .env \
  --name medremind-api \
  medremind-python-api
```

### Option B: With Persistent Storage

Mount a volume to persist parsed prescriptions:

```bash
docker run -d \
  -p 8000:8000 \
  --env-file .env \
  -v $(pwd)/storage:/app/storage \
  --name medremind-api \
  medremind-python-api
```

**Windows PowerShell:**
```powershell
docker run -d `
  -p 8000:8000 `
  --env-file .env `
  -v ${PWD}/storage:/app/storage `
  --name medremind-api `
  medremind-python-api
```

### Option C: Pass Environment Variables Directly

```bash
docker run -d \
  -p 8000:8000 \
  -e OPENAI_API_KEY="sk-your-key-here" \
  -e OPENAI_MODEL="gpt-4o-mini" \
  -e OPENAI_ENABLED="true" \
  -e OPENAI_VISION_MODEL="gpt-4o" \
  --name medremind-api \
  medremind-python-api
```

## Docker Compose Deployment

### 1. Create `docker-compose.yml`

```yaml
version: '3.8'

services:
  medremind-api:
    build: .
    container_name: medremind-python-api
    ports:
      - "8000:8000"
    env_file:
      - .env
    volumes:
      - ./storage:/app/storage
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### 2. Run with Docker Compose

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Production Deployment

### docker-compose.prod.yml

```yaml
version: '3.8'

services:
  medremind-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: medremind-python-api
    ports:
      - "8000:8000"
    env_file:
      - .env.production
    volumes:
      - prescription-storage:/app/storage
    restart: always
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

volumes:
  prescription-storage:
```

Run production:
```bash
docker-compose -f docker-compose.prod.yml up -d
```

## Container Management

### View Running Containers

```bash
docker ps
```

### View Logs

```bash
# All logs
docker logs medremind-api

# Follow logs (live)
docker logs -f medremind-api

# Last 100 lines
docker logs --tail 100 medremind-api
```

### Stop and Remove Container

```bash
# Stop container
docker stop medremind-api

# Remove container
docker rm medremind-api

# Stop and remove
docker rm -f medremind-api
```

### Restart Container

```bash
docker restart medremind-api
```

### Enter Container Shell

```bash
docker exec -it medremind-api /bin/bash
```

## Health Check & Testing

### Verify Service is Running

```bash
# Health check
curl http://localhost:8000/health

# Expected response:
# {"status": "healthy", "service": "medremind-prescription-parser", ...}
```

### Test Prescription Parsing

```bash
curl -X POST http://localhost:8000/api/prescription/parse \
  -H "Content-Type: application/json" \
  -d '{
    "ocr_text": "Date: 09/02/26\nDr. Smith\nPatient: John Doe, Age 45\nRx:\n1. Tab Aspirin 75mg - Once daily",
    "prescription_id": "test_001",
    "save_result": false
  }'
```

### Test OCR Extraction

```bash
# First, encode your image to base64
base64_image=$(base64 -w0 prescription.jpg)

curl -X POST http://localhost:8000/api/ocr/extract \
  -H "Content-Type: application/json" \
  -d "{\"document_base64\": \"$base64_image\", \"language_hint\": \"auto\"}"
```

### Access Swagger UI

Open in browser: http://localhost:8000/docs

## Updating the Container

### Rebuild and Restart

```bash
# Stop existing container
docker stop medremind-api

# Remove existing container
docker rm medremind-api

# Rebuild image
docker build -t medremind-python-api .

# Start new container
docker run -d -p 8000:8000 --env-file .env -v $(pwd)/storage:/app/storage --name medremind-api medremind-python-api
```

### With Docker Compose

```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## Multi-Container Setup with .NET

If running alongside the .NET MedRemind application:

### docker-compose.full.yml

```yaml
version: '3.8'

services:
  python-api:
    build: ./python-microservice
    container_name: medremind-python-api
    ports:
      - "8000:8000"
    env_file:
      - ./python-microservice/.env
    volumes:
      - python-storage:/app/storage
    restart: unless-stopped
    networks:
      - medremind-network

  dotnet-app:
    build: ./MedRemind.Web
    container_name: medremind-dotnet
    ports:
      - "5000:80"
      - "7000:443"
    environment:
      - PYTHON_API_URL=http://python-api:8000
    depends_on:
      - python-api
    restart: unless-stopped
    networks:
      - medremind-network

networks:
  medremind-network:
    driver: bridge

volumes:
  python-storage:
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs for errors
docker logs medremind-api

# Common issues:
# - Missing OPENAI_API_KEY
# - Port 8000 already in use
# - Invalid .env file format
```

### Port Already in Use

```bash
# Find process using port 8000
# Linux/Mac:
lsof -i :8000

# Windows:
netstat -ano | findstr :8000

# Use a different port
docker run -d -p 8001:8000 --env-file .env --name medremind-api medremind-python-api
```

### Out of Memory

Add memory limits to your run command:
```bash
docker run -d -p 8000:8000 --env-file .env \
  --memory=2g \
  --memory-swap=2g \
  --name medremind-api medremind-python-api
```

### Permission Issues on Storage Volume

```bash
# Fix permissions (Linux/Mac)
sudo chown -R 1000:1000 ./storage

# Or run container as root (not recommended for production)
docker run -d -p 8000:8000 --env-file .env --user root --name medremind-api medremind-python-api
```

### Image Build Fails

```bash
# Clean build cache and rebuild
docker builder prune -a
docker build --no-cache -t medremind-python-api .
```

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OPENAI_API_KEY` | Yes | - | OpenAI API key |
| `OPENAI_MODEL` | No | gpt-4o-mini | Model for text processing |
| `OPENAI_VISION_MODEL` | No | gpt-4o | Model for OCR |
| `OPENAI_ENABLED` | No | true | Enable OpenAI |
| `DEEPSEEK_API_KEY` | No | - | DeepSeek API key |
| `CLAUDE_API_KEY` | No | - | Anthropic API key |
| `HOST` | No | 0.0.0.0 | Server host |
| `PORT` | No | 8000 | Server port |
| `LOG_LEVEL` | No | INFO | Logging level |
| `CORS_ORIGINS` | No | * | Allowed CORS origins |
| `LANGCHAIN_TRACING_V2` | No | false | Enable LangSmith tracing |
| `LANGCHAIN_API_KEY` | No | - | LangSmith API key |

## Security Best Practices

1. **Never commit `.env` files to git**
2. **Use Docker secrets for production**:
   ```yaml
   services:
     api:
       secrets:
         - openai_key
   secrets:
     openai_key:
       external: true
   ```
3. **Run as non-root user** (already configured in Dockerfile)
4. **Use specific image tags** instead of `latest`
5. **Scan images for vulnerabilities**:
   ```bash
   docker scout quickview medremind-python-api
   ```

## API Endpoints Summary

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/api/prescription/parse` | POST | Parse prescription text |
| `/api/prescription/{id}` | GET | Get parsed prescription |
| `/api/ocr/extract` | POST | Extract text from image/PDF |
| `/api/ocr/extract-and-parse` | POST | OCR + parse in one call |
| `/docs` | GET | Swagger UI |

---

For more information, see the main [README.md](README.md).
