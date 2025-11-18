# AI Control Panel Architecture

## Overview
Private admin endpoint allowing users to interface with Claude AI (or Ollama) to make modifications to their EcommerceStarter instance.

## Core Components

### 1. Backend API (`/api/ai/*`)
- **Endpoints:**
  - `POST /api/ai/chat` - Send message, get response
  - `POST /api/ai/apply-change` - Apply code modification
  - `GET /api/ai/history` - Get conversation history
  - `POST /api/ai/config` - Configure API keys

- **Authentication:**
  - JWT token from user login
  - Role-based: Admin only
  - CORS: Restricted to same origin

### 2. Smart Routing Engine
```
RequestAnalyzer
  ├─ DetectRequestType()
  │  ├─ IsCodeModification? → Claude
  │  ├─ IsCreative? → Ollama
  │  └─ IsQuestion? → Ollama
  ├─ ExtractContext()
  │  ├─ File paths
  │  ├─ Code snippets
  │  └─ Feature scope
  └─ RouteToBackend()
     ├─ Ollama (if local)
     └─ Claude (if code-related)
```

### 3. API Integrations

**Claude (Anthropic SDK)**
- NuGet: `Anthropic.SDK`
- Endpoint: Proprietary API
- Cost: Pay-per-token
- Quality: High
- For: Code generation, complex changes

**Ollama (HTTP REST)**
- Endpoint: `http://localhost:11434/api/chat`
- Cost: Free (local)
- Quality: Good for creative tasks
- For: Suggestions, explanations, brainstorming

### 4. Safety Layer
- **Code Preview:** Show changes before applying
- **Git Integration:** Auto-commit before changes
- **Rollback:** Revert to previous commit
- **Rate Limiting:** Prevent abuse
- **Audit Log:** Track all modifications

### 5. Frontend Component
- Chat interface (message history)
- Code preview panel
- Deploy/Apply buttons
- Configuration panel for API keys
- Cost estimator (if using Claude)

## Database Schema

```sql
-- Store API keys securely (encrypted)
CREATE TABLE AdminAIConfig (
    Id INT PRIMARY KEY,
    AdminId UNIQUEIDENTIFIER,
    ClaudeApiKey NVARCHAR(MAX), -- Encrypted
    OllamaEndpoint NVARCHAR(MAX),
    PreferredBackend NVARCHAR(50), -- "Claude" or "Ollama"
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);

-- Track AI requests and modifications
CREATE TABLE AIModificationLog (
    Id BIGINT PRIMARY KEY IDENTITY,
    AdminId UNIQUEIDENTIFIER,
    RequestText NVARCHAR(MAX),
    BackendUsed NVARCHAR(50),
    ResponseText NVARCHAR(MAX),
    FilesModified NVARCHAR(MAX),
    CommitHash NVARCHAR(40),
    Status NVARCHAR(50), -- "Applied", "Rejected", "Pending"
    CreatedAt DATETIME,
    AppliedAt DATETIME
);

-- Store conversation history
CREATE TABLE AIChatHistory (
    Id BIGINT PRIMARY KEY IDENTITY,
    AdminId UNIQUEIDENTIFIER,
    UserMessage NVARCHAR(MAX),
    AIResponse NVARCHAR(MAX),
    BackendUsed NVARCHAR(50),
    MessageType NVARCHAR(50), -- "Chat", "CodeGeneration", "Review"
    CreatedAt DATETIME
);
```

## Folder Structure
```
EcommerceStarter/
├── AI/
│  ├── Services/
│  │  ├── IAIBackendService.cs
│  │  ├── ClaudeAIService.cs
│  │  ├── OllamaService.cs
│  │  └── RequestRouter.cs
│  ├── Models/
│  │  ├── AIRequest.cs
│  │  ├── AIResponse.cs
│  │  └── ModificationRequest.cs
│  └── Controllers/
│     └── AIController.cs
├── Pages/
│  ├── Admin/
│  │  ├── AIControlPanel.cshtml
│  │  └── AIControlPanel.cshtml.cs
│  └── ...
└── ...
```

## Security Considerations

1. **API Keys:** Encrypted in database, never exposed to frontend
2. **Authentication:** Admin-only access via existing Identity system
3. **Authorization:** Check user permissions before applying changes
4. **Rate Limiting:** Max requests per hour to prevent abuse
5. **Audit Trail:** Log all modifications with admin + timestamp
6. **Git Safety:** All changes committed before applied
7. **Rollback:** Easy revert via git history
8. **Content Filtering:** Don't allow deletion of critical files
9. **CORS:** Restrict to same origin only
10. **Input Validation:** Sanitize all user input

## User Flow

```
Admin User
  ↓
[Logs into website]
  ↓
[Navigates to /Admin/AIControlPanel]
  ↓
[Chat interface appears]
  ↓
Admin: "Add a testimonials section to homepage"
  ↓
RequestRouter: "This is code modification → Use Claude"
  ↓
Claude: [Generates React component + styling]
  ↓
[Preview panel shows code changes]
  ↓
Admin: [Reviews and clicks "Apply"]
  ↓
Backend: [Git commit + Apply changes + Rebuild]
  ↓
✅ Changes live!
```

## Configuration

Users would set this in `appsettings.json`:
```json
{
  "AI": {
    "Enabled": true,
    "AdminOnly": true,
    "DefaultBackend": "Ollama",
    "Backends": {
      "Claude": {
        "ApiKey": "sk-...", // Or from Azure KeyVault
        "Model": "claude-3-sonnet-20240229",
        "Enabled": true
      },
      "Ollama": {
        "Endpoint": "http://localhost:11434",
        "Model": "neural-chat",
        "Enabled": true
      }
    }
  }
}
```

## Deployment

For open-source:
1. User deploys EcommerceStarter
2. Navigates to Admin Settings
3. Configures AI backend (Claude API key or local Ollama)
4. AI Control Panel becomes available
5. Start chatting!

## MVP Features (Phase 1)
- ✅ Basic chat interface
- ✅ Smart routing (Ollama/Claude)
- ✅ Simple code generation requests
- ✅ Code preview
- ✅ Manual apply with confirmation

## Future Enhancements (Phase 2+)
- Auto-suggestions for common tasks
- Template system for common modifications
- A/B testing UI changes
- Analytics on which features users request
- Scheduled AI tasks (cleanup, optimization)
- Team collaboration on modifications
