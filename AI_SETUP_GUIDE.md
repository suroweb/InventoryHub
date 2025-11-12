# 🤖 AI Integration Setup Guide

InventoryHub supports **100% open-source AI models** for intelligent inventory forecasting, smart recommendations, and natural language queries.

---

## 🎯 Supported AI Providers

### 1. **DeepSeek** (Recommended - Open Source)
- **Why:** Powerful, cost-effective, open-source models
- **Models:** deepseek-chat, deepseek-coder
- **Cost:** Very affordable (~$0.14 per million tokens)
- **Setup Time:** 5 minutes

### 2. **Ollama** (Local - 100% Free & Private)
- **Why:** Runs entirely on your server, no API calls
- **Models:** Llama 2, Llama 3, Mistral, CodeLlama, and 50+ more
- **Cost:** FREE (uses your hardware)
- **Setup Time:** 10 minutes
- **Perfect for:** Privacy-sensitive deployments, offline use

### 3. **OpenAI** (Optional)
- **Why:** Most powerful models (GPT-4)
- **Cost:** Higher (~$0.03-$0.06 per 1K tokens)
- **Use Case:** Maximum accuracy for enterprise customers

### 4. **Custom Local Models**
- **Why:** Full control, any model you want
- **Setup:** Point to your custom endpoint

---

## 🚀 Quick Start (5 minutes)

### Option A: DeepSeek (Cloud - Recommended)

1. **Get API Key** (FREE tier available)
   ```bash
   # Visit: https://platform.deepseek.com/
   # Sign up and get your API key
   ```

2. **Configure InventoryHub**
   ```json
   // appsettings.json
   {
     "AI": {
       "Provider": "DeepSeek",
       "ApiKey": "YOUR_DEEPSEEK_API_KEY",
       "BaseUrl": "https://api.deepseek.com/v1",
       "Model": "deepseek-chat",
       "MaxTokens": 2000,
       "Temperature": 0.7
     }
   }
   ```

3. **Done!** AI features are now active.

---

### Option B: Ollama (Local - 100% Free & Private)

1. **Install Ollama**
   ```bash
   # Linux/Mac
   curl -fsSL https://ollama.com/install.sh | sh

   # Windows
   # Download from: https://ollama.com/download
   ```

2. **Pull a Model**
   ```bash
   # Llama 2 (7B) - Good balance of speed/quality
   ollama pull llama2

   # Mistral (7B) - Faster, still high quality
   ollama pull mistral

   # Llama 3 (8B) - Latest and greatest
   ollama pull llama3

   # CodeLlama - Best for technical analysis
   ollama pull codellama
   ```

3. **Start Ollama Server**
   ```bash
   ollama serve
   # Runs on http://localhost:11434
   ```

4. **Configure InventoryHub**
   ```json
   // appsettings.json
   {
     "AI": {
       "Provider": "Ollama",
       "BaseUrl": "http://localhost:11434",
       "Model": "llama2",  // or "mistral", "llama3", etc.
       "MaxTokens": 2000,
       "Temperature": 0.7
     }
   }
   ```

5. **Done!** 100% private, 100% free AI.

---

### Option C: OpenAI (Optional)

```json
// appsettings.json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "sk-...",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4-turbo-preview",
    "MaxTokens": 2000,
    "Temperature": 0.7
  }
}
```

---

## 🎁 What You Get (AI-Powered Features)

### 1. **Demand Forecasting**
```http
GET /api/v1/ai/forecast/{productId}?days=30
```

**What it does:**
- Predicts future demand for next 30 days
- Analyzes historical patterns and trends
- Provides confidence levels
- Identifies seasonality

**Example Response:**
```json
{
  "productId": "...",
  "productName": "Laptop",
  "currentStock": 25,
  "forecast": [
    {
      "date": "2025-11-13",
      "predictedDemand": 3,
      "lowerBound": 2,
      "upperBound": 4
    },
    // ... 29 more days
  ],
  "confidence": 0.87,
  "trend": "increasing",
  "insights": {
    "ai_analysis": "Demand is steadily increasing due to seasonal factors..."
  }
}
```

---

### 2. **Smart Reorder Recommendations**
```http
GET /api/v1/ai/reorder-recommendation/{productId}
```

**What it does:**
- Tells you WHEN and HOW MUCH to reorder
- Calculates optimal order quantities
- Predicts stockout dates
- Provides AI-generated reasoning

**Example Response:**
```json
{
  "productId": "...",
  "productName": "Laptop",
  "shouldReorder": true,
  "recommendedQuantity": 45,
  "suggestedOrderDate": "2025-11-13",
  "expectedStockoutDate": "2025-11-20",
  "reasoning": "Current stock of 25 units will deplete in 7 days based on increasing demand trend. Recommended order of 45 units ensures 30-day supply with safety margin.",
  "estimatedCost": 54000.00
}
```

---

### 3. **Product Recommendations** (AI-Powered)
```http
GET /api/v1/ai/recommendations/{customerId}
```

**What it does:**
- Analyzes customer purchase history
- Recommends complementary products
- Uses AI to understand product relationships
- Increases cross-selling opportunities

**Example Response:**
```json
[
  {
    "productId": "...",
    "productName": "Laptop Bag",
    "relevanceScore": 0.92,
    "reason": "Customers who bought laptops often purchase protective bags within 2 weeks",
    "price": 49.99
  },
  {
    "productId": "...",
    "productName": "Wireless Mouse",
    "relevanceScore": 0.88,
    "reason": "Frequently purchased together with laptops for improved productivity",
    "price": 29.99
  }
]
```

---

### 4. **Natural Language Queries**
```http
POST /api/v1/ai/query
Content-Type: application/json

{
  "query": "What products are running low and need to be reordered?"
}
```

**What it does:**
- Ask questions in plain English
- AI understands your intent
- Provides actionable answers
- Perfect for dashboards and chatbots

**Example Response:**
```json
{
  "query": "What products are running low and need to be reordered?",
  "answer": "Currently, 5 products require immediate attention:\n\n1. Laptop (Stock: 25) - Expected stockout in 7 days. Recommend ordering 45 units.\n2. Headphones (Stock: 8) - Below reorder point of 10. Recommend ordering 50 units.\n3...",
  "confidence": 0.85,
  "model": "deepseek-chat"
}
```

---

### 5. **Seasonality Analysis**
```http
GET /api/v1/ai/seasonality/{productId}
```

**What it does:**
- Identifies seasonal demand patterns
- Finds peak periods (holidays, seasons)
- Calculates monthly multipliers
- Helps with inventory planning

---

### 6. **Stock Optimization**
```http
GET /api/v1/ai/optimize-stock/{locationId}
```

**What it does:**
- Analyzes all products at a location
- Identifies overstocked items
- Identifies understocked items
- Recommends optimal levels
- Calculates potential savings

**Example Response:**
```json
{
  "locationId": "...",
  "locationName": "Main Warehouse",
  "products": [
    {
      "productName": "Laptop",
      "currentStock": 80,
      "optimalStock": 45,
      "action": "reduce",
      "reasoning": "Based on 1.5 units/day demand forecast. Current stock is 77% above optimal."
    }
  ],
  "totalSavings": 42000.00,
  "itemsToReduce": 12,
  "itemsToIncrease": 8
}
```

---

## 🔧 Advanced Configuration

### Environment Variables (Recommended for Production)
```bash
export AI__Provider=DeepSeek
export AI__ApiKey=your-secret-key
export AI__Model=deepseek-chat
```

### Multiple Providers (Fallback)
You can configure fallback providers in code:
```csharp
// Primary: DeepSeek
// Fallback: Ollama (local)
// Final Fallback: Simple statistics
```

---

## 💰 Cost Comparison

| Provider | Model | Cost per Million Tokens | Speed | Privacy |
|----------|-------|------------------------|-------|---------|
| **DeepSeek** | deepseek-chat | $0.14 | Fast | Cloud |
| **Ollama (Local)** | llama2/mistral | **$0.00** | Medium | **100% Private** |
| OpenAI | gpt-4-turbo | $30.00 | Fast | Cloud |
| OpenAI | gpt-3.5-turbo | $0.50 | Very Fast | Cloud |

### Example Usage Costs (1000 customers)
- **DeepSeek:** ~$50/month (highly cost-effective)
- **Ollama:** $0/month + server costs (fully private)
- **OpenAI GPT-4:** ~$10,000/month (premium)

**Recommendation:** Start with DeepSeek for cloud or Ollama for on-premise. Both are excellent!

---

## 🎯 Ollama Model Recommendations

### For General Forecasting & Recommendations
```bash
ollama pull llama2        # 7B - Good balance
ollama pull mistral       # 7B - Faster inference
ollama pull llama3        # 8B - Best quality
```

### For Technical/Code Analysis
```bash
ollama pull codellama     # 7B - Code understanding
ollama pull deepseek-coder # 6.7B - Code generation
```

### For Speed (Real-time responses)
```bash
ollama pull phi          # 2.7B - Very fast
ollama pull tinyllama    # 1.1B - Lightning fast
```

### For Maximum Accuracy
```bash
ollama pull llama2:70b   # 70B - Highest quality (needs 40GB RAM)
ollama pull mixtral:8x7b # MoE - Great quality, less RAM
```

---

## 🐳 Docker Deployment (with Ollama)

```yaml
# docker-compose.yml
version: '3.8'

services:
  ollama:
    image: ollama/ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    restart: unless-stopped

  inventoryhub-api:
    build: .
    environment:
      AI__Provider: "Ollama"
      AI__BaseUrl: "http://ollama:11434"
      AI__Model: "llama2"
    depends_on:
      - ollama
      - postgres

volumes:
  ollama-data:
```

---

## 🔒 Security & Privacy

### DeepSeek
- Data sent to DeepSeek API (cloud)
- HTTPS encrypted
- No data retention (check their policy)
- API key required

### Ollama (Local)
- **100% private** - nothing leaves your server
- **GDPR compliant** - all data stays local
- **No API keys** - fully self-hosted
- Perfect for healthcare, finance, government

### Recommendation
- **Public data:** Use DeepSeek (cheaper, easier)
- **Sensitive data:** Use Ollama (fully private)
- **Hybrid:** Use both (Ollama for sensitive, DeepSeek for public)

---

## 🚀 Performance Tips

### 1. GPU Acceleration (Ollama)
```bash
# Install NVIDIA CUDA for GPU support
# Ollama automatically uses GPU if available
# 10-50x faster with GPU!
```

### 2. Model Selection
- **Llama 2 7B:** Best general purpose
- **Mistral 7B:** Faster, slightly less accurate
- **Phi 2.7B:** Real-time responses
- **Llama 3 8B:** Latest, best quality

### 3. Caching
InventoryHub automatically caches forecast results for 24 hours.

---

## 📊 API Endpoints Summary

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/ai/forecast/{productId}` | GET | Demand forecasting |
| `/api/v1/ai/reorder-recommendation/{productId}` | GET | Smart reorder |
| `/api/v1/ai/recommendations/{customerId}` | GET | Product recommendations |
| `/api/v1/ai/seasonality/{productId}` | GET | Seasonal analysis |
| `/api/v1/ai/optimize-stock/{locationId}` | GET | Stock optimization |
| `/api/v1/ai/query` | POST | Natural language query |
| `/api/v1/ai/analyze` | POST | Data analysis |

---

## ❓ Troubleshooting

### "AI service unavailable"
- Check `appsettings.json` configuration
- Verify API key (DeepSeek) or Ollama is running
- Check logs for error details

### "Ollama connection refused"
```bash
# Start Ollama
ollama serve

# Verify it's running
curl http://localhost:11434/api/tags
```

### "Out of memory" (Ollama)
- Use smaller models (phi, tinyllama)
- Reduce `num_ctx` parameter
- Add more RAM or use GPU

### "Slow responses"
- Use smaller models
- Enable GPU acceleration
- Use DeepSeek cloud API instead

---

## 🎉 Success!

Your InventoryHub now has enterprise-grade AI capabilities using **100% open-source models**!

**Features enabled:**
✅ Demand forecasting with AI
✅ Smart reorder recommendations
✅ Product recommendations
✅ Natural language queries
✅ Seasonality analysis
✅ Stock optimization

**Total cost:** $0-50/month (vs. competitors charging $500+/mo for AI features)

---

**Next Steps:**
1. Run the API: `dotnet run`
2. Test forecasting: `GET /api/v1/ai/forecast/{productId}`
3. Try natural language: `POST /api/v1/ai/query` with "Show me low stock items"
4. Explore Swagger docs: `/swagger`

**Questions?** Check `PROJECT_STATUS.md` or create an issue on GitHub.

---

*Built with ❤️ using DeepSeek, Ollama, and other open-source AI*
