"""
Configuration loader
Reads from .env file and provides configuration objects
"""

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings"""
    
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False
    )
    
    # OpenAI Configuration
    openai_api_key: str
    openai_model: str = "gpt-4o-mini"
    openai_vision_model: str = "gpt-4o"  # Vision model for OCR
    openai_enabled: bool = True
    
    # DeepSeek Configuration
    deepseek_api_key: str | None = None
    deepseek_api_url: str = "https://api.deepseek.com/v1/chat/completions"
    deepseek_model: str = "deepseek-chat"
    deepseek_max_tokens: int = 5000
    deepseek_enabled: bool = False
    
    # Claude Configuration
    claude_api_key: str | None = None
    claude_model: str = "claude-3-5-sonnet-20241022"
    claude_max_tokens: int = 1500
    claude_enabled: bool = False
    
    # Server Configuration
    host: str = "0.0.0.0"
    port: int = 8000
    log_level: str = "INFO"
    cors_origins: str = "http://localhost:5000,https://localhost:7000"
    
    # Storage Configuration
    storage_path: str = "./storage/parsed_prescriptions"
    enable_file_storage: bool = True
    
    # Crew Configuration
    preferred_parser: str = "openai"
    max_retries: int = 2
    
    # Tracing Configuration (LangSmith)
    langchain_tracing_v2: bool = True
    langchain_endpoint: str = "https://api.smith.langchain.com"
    langchain_api_key: str | None = None
    langchain_project: str = "medremind-prescription-parser"
    
    @property
    def cors_origins_list(self) -> list[str]:
        """Parse CORS origins into list"""
        return [origin.strip() for origin in self.cors_origins.split(",")]


# Global settings instance
settings = Settings()
