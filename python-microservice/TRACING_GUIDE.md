# 🔍 LangSmith Tracing Guide for MedRemind

## Overview

LangSmith tracing is now enabled in the MedRemind prescription parser to help you:
- **Monitor agent execution** in real-time
- **Debug issues** with agent workflows
- **Track costs** and token usage
- **Optimize performance** of each agent
- **Visualize** the entire processing pipeline

## Quick Setup

### 1. Get LangSmith API Key

1. Visit [smith.langchain.com](https://smith.langchain.com/)
2. Sign up or log in with your account
3. Go to **Settings** → **API Keys**
4. Create a new API key
5. Copy the key

### 2. Configure Environment

Add to your `.env` file:

```env
# Enable LangSmith Tracing
LANGCHAIN_TRACING_V2=true
LANGCHAIN_ENDPOINT=https://api.smith.langchain.com
LANGCHAIN_API_KEY=lsv2_pt_your_api_key_here
LANGCHAIN_PROJECT=medremind-prescription-parser
```

### 3. Restart Server

```bash
uv run uvicorn app.main:app --reload --port 8000
```

You should see:
```
✅ LangSmith tracing enabled - Project: medremind-prescription-parser
```

## What Gets Tracked

### Agent-Level Tracking

Each agent execution is tracked with:
- **Input**: OCR text or previous agent output
- **Output**: Agent's response
- **Duration**: Time taken for execution
- **Success/Failure**: Status of execution

### LLM Call Tracking

For each LLM call:
- **Prompt**: The exact prompt sent to the LLM
- **Response**: The LLM's complete response
- **Model**: Which model was used (GPT-4, Claude, etc.)
- **Tokens**: Input and output token counts
- **Cost**: Estimated cost per call
- **Latency**: Response time

### Complete Prescription Processing Trace

Example trace structure:
```
📋 Prescription Parse Request
├── 🧹 Agent 1: OCR Normalizer
│   ├── Input: Raw OCR text
│   ├── LLM Call: Text normalization
│   └── Output: Cleaned text
├── 🔍 Agent 2: Data Extractor
│   ├── Input: Normalized text
│   ├── LLM Call: JSON extraction
│   └── Output: Structured data
├── 💊 Agent 3: Medicine Validator
│   ├── Input: Medications list
│   ├── LLM Call: Drug interaction analysis
│   └── Output: Validation results
└── ✅ Agent 4: Safety Validator
    ├── Input: Complete prescription data
    ├── LLM Call: Safety checks
    └── Output: Final validation
```

## Viewing Traces

### LangSmith Dashboard

1. Go to [smith.langchain.com](https://smith.langchain.com/)
2. Select your project: **medremind-prescription-parser**
3. Click on any trace to see details

### Trace Details Page

You'll see:
- **Timeline**: Visual representation of agent execution
- **Input/Output**: Full data for each step
- **Metadata**: Prescription ID, timestamps, etc.
- **Feedback**: Option to add feedback for improvement

## Common Use Cases

### 1. Debugging Agent Failures

If an agent fails:
1. Find the failed trace in LangSmith
2. Check the error message
3. View the exact input that caused failure
4. See the LLM's response before failure

### 2. Optimizing Prompts

1. Find traces with low-quality outputs
2. Review the prompts sent to LLMs
3. Identify patterns in failures
4. Adjust prompts in agent files

### 3. Cost Analysis

1. View **Usage** tab in LangSmith
2. See total tokens and costs
3. Identify most expensive operations
4. Optimize by switching models or reducing prompt size

### 4. Performance Monitoring

1. Check **Latency** metrics
2. Identify slow agents
3. Look for bottlenecks
4. Optimize sequential vs parallel execution

## Advanced Features

### Tagging Traces

Add custom tags in your code:

```python
from langsmith import traceable

@traceable(
    run_type="prescription",
    tags=["production", "high-priority"]
)
def process_prescription(data):
    # Your code
    pass
```

### Filtering Traces

In LangSmith UI:
- Filter by date range
- Filter by status (success/error)
- Filter by tags
- Search by prescription ID

### Setting Up Alerts

1. Go to **Monitoring** in LangSmith
2. Create alert rules:
   - High error rate
   - Slow response times
   - Cost thresholds
3. Get notifications via email/Slack

## Disabling Tracing

### Temporarily Disable

Set in `.env`:
```env
LANGCHAIN_TRACING_V2=false
```

### For Specific Environments

Use different `.env` files:
- `.env.development` - Tracing enabled
- `.env.production` - Tracing disabled (for privacy/cost)

## Privacy & Security

### What Data is Sent

- Agent inputs/outputs
- LLM prompts and responses
- Metadata (timestamps, IDs)

⚠️ **Warning**: Patient data and prescription details will be sent to LangSmith servers.

### Recommendations

1. **Development**: Enable tracing for debugging
2. **Production**: Consider disabling or using data anonymization
3. **HIPAA Compliance**: Review LangSmith's compliance docs

## Troubleshooting

### Tracing Not Working

**Check console output**:
```
✅ LangSmith tracing enabled - Project: medremind-prescription-parser
```

If you see:
```
⚠️ LangSmith tracing enabled but LANGCHAIN_API_KEY not set
```

→ Check your `.env` file has the correct API key

### API Key Invalid

Error: `Authentication failed`

→ Verify API key is correct and active in LangSmith settings

### No Traces Appearing

1. Verify `LANGCHAIN_TRACING_V2=true`
2. Check API key is set
3. Ensure project name matches
4. Wait a few seconds (traces may be delayed)

## Best Practices

1. **Use Meaningful Project Names**: e.g., `medremind-prod`, `medremind-dev`
2. **Add Metadata**: Include prescription IDs in traces
3. **Review Regularly**: Check traces weekly for issues
4. **Monitor Costs**: Set up cost alerts
5. **Clean Up Old Traces**: Delete old development traces

## Support

- **LangSmith Docs**: [docs.smith.langchain.com](https://docs.smith.langchain.com/)
- **Discord**: [LangChain Discord](https://discord.gg/langchain)
- **GitHub Issues**: Report bugs in MedRemind repo

---

**Happy Tracing! 🔍✨**
