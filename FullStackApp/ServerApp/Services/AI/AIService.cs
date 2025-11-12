namespace ServerApp.Services.AI;

/// <summary>
/// AI Provider types - supports open source and proprietary models
/// </summary>
public enum AIProvider
{
    DeepSeek,      // DeepSeek open source models
    Ollama,        // Local Ollama (Llama, Mistral, etc.)
    OpenAI,        // Optional: OpenAI GPT models
    Anthropic,     // Optional: Claude models
    LocalModel     // Custom local model endpoint
}

/// <summary>
/// AI Service configuration
/// </summary>
public class AIServiceConfig
{
    public AIProvider Provider { get; set; } = AIProvider.DeepSeek;
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
    public string Model { get; set; } = "deepseek-chat";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// AI Request/Response models
/// </summary>
public class AIRequest
{
    public required string Prompt { get; set; }
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
    public Dictionary<string, object>? Context { get; set; }
}

public class AIResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
    public TimeSpan Duration { get; set; }
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Main AI Service interface - supports multiple open source providers
/// </summary>
public interface IAIService
{
    Task<AIResponse> GenerateTextAsync(AIRequest request);
    Task<AIResponse> AnalyzeDataAsync(string data, string analysisType);
    Task<string> GenerateSummaryAsync(string text, int maxLength = 200);
    Task<Dictionary<string, object>> ExtractStructuredDataAsync(string text);
}

/// <summary>
/// AI Service implementation supporting DeepSeek, Ollama, and other open source models
/// </summary>
public class AIService : IAIService
{
    private readonly AIServiceConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIService> _logger;

    public AIService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AIService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Load configuration
        _config = new AIServiceConfig
        {
            Provider = Enum.Parse<AIProvider>(configuration["AI:Provider"] ?? "DeepSeek"),
            ApiKey = configuration["AI:ApiKey"],
            BaseUrl = configuration["AI:BaseUrl"] ?? "https://api.deepseek.com/v1",
            Model = configuration["AI:Model"] ?? "deepseek-chat",
            MaxTokens = int.Parse(configuration["AI:MaxTokens"] ?? "2000"),
            Temperature = double.Parse(configuration["AI:Temperature"] ?? "0.7")
        };

        _logger.LogInformation($"AI Service initialized with provider: {_config.Provider}, model: {_config.Model}");
    }

    public async Task<AIResponse> GenerateTextAsync(AIRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            return _config.Provider switch
            {
                AIProvider.DeepSeek => await CallDeepSeekAsync(request),
                AIProvider.Ollama => await CallOllamaAsync(request),
                AIProvider.OpenAI => await CallOpenAIAsync(request),
                AIProvider.LocalModel => await CallLocalModelAsync(request),
                _ => throw new NotSupportedException($"Provider {_config.Provider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"AI generation failed with provider {_config.Provider}");
            return new AIResponse
            {
                Success = false,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<AIResponse> AnalyzeDataAsync(string data, string analysisType)
    {
        var prompt = analysisType.ToLower() switch
        {
            "sentiment" => $"Analyze the sentiment of this text and provide a score from -1 (negative) to 1 (positive):\n\n{data}",
            "summary" => $"Provide a concise summary of this data:\n\n{data}",
            "insights" => $"Extract key insights and patterns from this data:\n\n{data}",
            "trends" => $"Identify trends and anomalies in this data:\n\n{data}",
            _ => $"Analyze this data:\n\n{data}"
        };

        return await GenerateTextAsync(new AIRequest
        {
            Prompt = prompt,
            SystemPrompt = "You are a data analysis expert. Provide clear, structured insights.",
            Temperature = 0.3 // Lower temperature for analytical tasks
        });
    }

    public async Task<string> GenerateSummaryAsync(string text, int maxLength = 200)
    {
        var response = await GenerateTextAsync(new AIRequest
        {
            Prompt = $"Summarize the following text in {maxLength} characters or less:\n\n{text}",
            MaxTokens = maxLength / 2, // Rough estimate
            Temperature = 0.5
        });

        return response.Success ? response.Content ?? string.Empty : string.Empty;
    }

    public async Task<Dictionary<string, object>> ExtractStructuredDataAsync(string text)
    {
        var response = await GenerateTextAsync(new AIRequest
        {
            Prompt = $"Extract structured data from this text and return as JSON:\n\n{text}",
            SystemPrompt = "You are a data extraction expert. Return only valid JSON.",
            Temperature = 0.2
        });

        if (response.Success && !string.IsNullOrEmpty(response.Content))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content)
                    ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object> { { "raw", response.Content } };
            }
        }

        return new Dictionary<string, object>();
    }

    // DeepSeek API implementation (open source)
    private async Task<AIResponse> CallDeepSeekAsync(AIRequest request)
    {
        var startTime = DateTime.UtcNow;

        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        }

        var requestBody = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt ?? "You are a helpful AI assistant." },
                new { role = "user", content = request.Prompt }
            },
            max_tokens = request.MaxTokens,
            temperature = request.Temperature
        };

        var response = await client.PostAsJsonAsync("/chat/completions", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"DeepSeek API error: {response.StatusCode} - {responseContent}");
            throw new Exception($"DeepSeek API error: {response.StatusCode}");
        }

        var jsonResponse = System.Text.Json.JsonDocument.Parse(responseContent);
        var content = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var tokensUsed = jsonResponse.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return new AIResponse
        {
            Success = true,
            Content = content,
            TokensUsed = tokensUsed,
            Duration = DateTime.UtcNow - startTime,
            Model = _config.Model
        };
    }

    // Ollama API implementation (local open source models)
    private async Task<AIResponse> CallOllamaAsync(AIRequest request)
    {
        var startTime = DateTime.UtcNow;

        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.BaseUrl); // Default: http://localhost:11434
        client.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        var requestBody = new
        {
            model = _config.Model, // e.g., "llama2", "mistral", "codellama"
            prompt = $"{request.SystemPrompt}\n\n{request.Prompt}",
            stream = false,
            options = new
            {
                temperature = request.Temperature,
                num_predict = request.MaxTokens
            }
        };

        var response = await client.PostAsJsonAsync("/api/generate", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Ollama API error: {response.StatusCode} - {responseContent}");
            throw new Exception($"Ollama API error: {response.StatusCode}");
        }

        var jsonResponse = System.Text.Json.JsonDocument.Parse(responseContent);
        var content = jsonResponse.RootElement.GetProperty("response").GetString() ?? string.Empty;

        return new AIResponse
        {
            Success = true,
            Content = content,
            TokensUsed = 0, // Ollama doesn't return token count
            Duration = DateTime.UtcNow - startTime,
            Model = _config.Model
        };
    }

    // OpenAI API implementation (optional)
    private async Task<AIResponse> CallOpenAIAsync(AIRequest request)
    {
        var startTime = DateTime.UtcNow;

        using var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com/v1");
        client.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        }

        var requestBody = new
        {
            model = _config.Model, // e.g., "gpt-4", "gpt-3.5-turbo"
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt ?? "You are a helpful assistant." },
                new { role = "user", content = request.Prompt }
            },
            max_tokens = request.MaxTokens,
            temperature = request.Temperature
        };

        var response = await client.PostAsJsonAsync("/chat/completions", requestBody);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }

        var jsonResponse = System.Text.Json.JsonDocument.Parse(responseContent);
        var content = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var tokensUsed = jsonResponse.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return new AIResponse
        {
            Success = true,
            Content = content,
            TokensUsed = tokensUsed,
            Duration = DateTime.UtcNow - startTime,
            Model = _config.Model
        };
    }

    // Local model endpoint implementation
    private async Task<AIResponse> CallLocalModelAsync(AIRequest request)
    {
        // Similar to Ollama but with custom endpoint format
        // Can be adapted for any local model server
        return await CallOllamaAsync(request);
    }
}
