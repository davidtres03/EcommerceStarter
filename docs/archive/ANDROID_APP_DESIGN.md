# EcommerceStarter Mobile App - Design & Architecture

## Executive Summary

A native Android application that extends EcommerceStarter functionality to mobile devices, enabling users to manage their e-commerce website, monitor performance, and interact with AI assistance from anywhere. The app bridges desktop management capabilities to mobile with offline-first architecture.

**Target**: Android 8.0+ (API 26+), supporting 99% of active Android devices

---

## 1. Core Features & Use Cases

### 1.1 Dashboard (Primary Screen)
**Purpose**: Real-time overview of business metrics

**Display Elements**:
- Pending orders count (badge)
- Today's revenue
- Website status (online/offline with last check time)
- Traffic sparkline (last 24 hours)
- Inventory alert count
- Customer support queue length

**Interactions**:
- Tap order count → Navigate to Orders screen
- Tap revenue → Show daily/weekly/monthly breakdown
- Tap status indicator → Run health check immediately
- Pull-to-refresh → Force sync with backend
- Tap AI assistant → Open chat

**Data Sync**:
- Initial load: Full sync from `/api/mobile/dashboard`
- Background: Push notifications every 5 minutes (or SignalR if connected)
- Offline: Display cached data with timestamp

### 1.2 Website Management
**Purpose**: Control website without desktop

**Capabilities**:
- **Status Control**: Toggle website online/offline
- **Content Updates**: Publish blog posts, update homepage hero image
- **Product Management**: Add/edit/delete products (limited UI - full edit on desktop)
- **Order Processing**: View, mark as shipped, print labels
- **Customer Messages**: Respond to support requests
- **Settings**: Manage basic configurations

**Data Sync**:
- Changes → Upload immediately to backend
- Backend confirms → Update cache
- Offline → Queue actions, sync on reconnect

### 1.3 AI Chat Assistant
**Purpose**: Mobile-friendly AI interaction

**Features**:
- Chat interface (similar to web Admin Control Panel)
- Voice input: "Hey Catalyst, how many orders today?" → AI responds
- Suggested quick actions: "Check website status", "Recent orders", "Generate product description"
- Chat history searchable locally (sync with backend)
- Code generation with one-tap copy-to-clipboard

**Offline Support**:
- Limited mode: Can't generate new responses but can review history
- Shows "offline" badge when disconnected

### 1.4 Push Notifications
**Purpose**: Keep user informed without app open

**Events Triggering Notifications**:
- New customer order received
- Website status change (online → offline, offline → online)
- AI assistant response ready (long-running requests)
- System alert (update available, backup complete)

**Notification Payload**:
```json
{
  "title": "New Order #12345",
  "body": "3 items, $49.99 from Jane Doe",
  "route": "orders/12345",
  "data": {"orderId": "12345"}
}
```

---

## 2. Framework Selection & Justification

### Option A: Flutter ✅ RECOMMENDED
**Pros**:
- Single codebase = iOS + Android with 95%+ code sharing
- Fast development cycle with hot reload
- Excellent performance, smooth animations
- Growing ecosystem for business apps
- Google-backed, strong community

**Cons**:
- Slightly larger app size (~50MB for Hello World)
- Smaller ecosystem vs native for some specialized features
- Learning curve if team new to Dart

**Best For**: Rapid cross-platform deployment, future iOS expansion

### Option B: Kotlin (Native Android)
**Pros**:
- Maximum native performance
- Direct access to all Android APIs
- Mature ecosystem and libraries
- Smaller app size (~20MB)
- Extensive documentation and StackOverflow help

**Cons**:
- Android-only (can't easily port to iOS)
- Longer development time
- More boilerplate code for same features

**Best For**: Android-only focus, maximum performance

### Option C: React Native
**Pros**:
- Code sharing with web (JavaScript)
- Large ecosystem and community
- Fast to market

**Cons**:
- Performance issues with complex UIs
- Dependency on third-party libraries
- More unstable than Flutter
- Larger community means more conflicting solutions

**Best For**: Teams with strong JavaScript background, web-first development

### **RECOMMENDATION: Flutter**
- **Rationale**: Single codebase enables iOS launch later without rewriting
- **Timeline**: 40% faster development than native Kotlin
- **Maintenance**: One code base = 50% less ongoing maintenance
- **Skills**: Dart is similar to TypeScript/C# - easy for .NET team to learn

---

## 3. Architecture & Data Flow

### 3.1 App Architecture (MVVM + Repository Pattern)

```
┌─────────────────────────────────────────────────────┐
│              UI Layer (Widgets)                      │
│  Dashboard | Orders | Products | Chat | Settings   │
└────────────────────┬────────────────────────────────┘
                     │
         ┌───────────┴────────────┐
         │                        │
    ┌────▼──────────┐    ┌───────▼────────┐
    │  ViewModel    │    │   Bloc/State   │
    │  (Business    │    │   Management   │
    │   Logic)      │    │   (Cubit)      │
    └────┬──────────┘    └───────┬────────┘
         │                        │
         └───────────┬────────────┘
                     │
    ┌────────────────▼─────────────────┐
    │   Repository Layer               │
    │  • API calls                      │
    │  • Cache management              │
    │  • Offline queue                 │
    └────────────┬──────────────────────┘
                 │
    ┌────────────┴──────────────────────┐
    │     Data Layer                     │
    │  • LocalStorage (Hive)             │
    │  • Network (Dio HTTP client)       │
    │  • Push notifications (Firebase)   │
    └────────────────────────────────────┘
```

### 3.2 Data Flow - Dashboard Load

**Scenario**: User opens app

```
User Opens App
    ↓
Check Local Cache (Hive)
    ├─→ If fresh (<5 min): Display cached data
    └─→ If stale: Show cached data + loading spinner
    ↓
Network Request to /api/mobile/dashboard
    ├─→ Success: Update cache, refresh UI
    ├─→ Timeout (no internet): Use cached data
    └─→ Auth error: Redirect to login
    ↓
Display Dashboard with latest data
```

**Offline Behavior**:
- First load: Cache loaded from Hive local database
- Timestamp shown: "Last updated 2 min ago"
- Sync button available: "Tap to sync now"
- Actions queued: Orders marked "pending sync"

### 3.3 API Authentication Flow

**First Launch**:
```
1. User enters email/password
2. POST /api/auth/mobile-login
3. Backend returns: { accessToken, refreshToken, expiresIn }
4. App stores refreshToken securely (EncryptedSharedPreferences)
5. accessToken kept in memory for current session
```

**Subsequent Requests**:
```
Every request includes: Authorization: Bearer {accessToken}

On 401 (Unauthorized):
1. Use refreshToken to get new accessToken
2. Retry original request
3. If refresh fails → Redirect to login
```

**Token Storage**:
- **accessToken** (1 hour lifetime): Memory only (lost on app close)
- **refreshToken** (30 day lifetime): EncryptedSharedPreferences (survived app restart)
- **userId** (reference): Secure storage

---

## 4. Backend API Design (Mobile-Specific Endpoints)

### 4.1 Authentication

```
POST /api/auth/mobile-login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password",
  "deviceId": "android-uuid-123"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresIn": 3600,
  "user": {
    "id": "user-123",
    "email": "user@example.com",
    "displayName": "John Doe"
  }
}
```

### 4.2 Dashboard Data

```
GET /api/mobile/dashboard
Authorization: Bearer {accessToken}
Cache-Control: max-age=300 (5 minutes)

Response:
{
  "timestamp": "2024-11-13T15:30:00Z",
  "metrics": {
    "pendingOrders": 5,
    "todayRevenue": 1250.50,
    "todayOrders": 23,
    "websiteStatus": "online",
    "lastStatusCheck": "2024-11-13T15:29:45Z",
    "traffic": {
      "last24Hours": [120, 145, 167, ...],  // 24 data points
      "peak": 267,
      "average": 156
    },
    "inventoryAlerts": 3,
    "supportQueueLength": 2
  }
}
```

### 4.3 Chat Endpoint

```
POST /api/mobile/ai/chat
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "message": "How many orders did we get today?",
  "conversationId": "conv-123",  // Optional, new if omitted
  "includeContext": true  // Include dashboard metrics in prompt
}

Response:
{
  "conversationId": "conv-123",
  "response": "Based on your dashboard, you received 23 orders today totaling $1,250.50. That's a 15% increase from yesterday.",
  "tokensUsed": 45,
  "model": "claude-3-sonnet",
  "timestamp": "2024-11-13T15:31:00Z"
}
```

### 4.4 Voice Input Processing

```
POST /api/mobile/ai/process-voice
Authorization: Bearer {accessToken}
Content-Type: multipart/form-data

audio: [audio file .wav, max 30 seconds]
includeContext: true

Response:
{
  "transcription": "How many orders did we get today?",
  "confidence": 0.94,
  "aiResponse": "Based on your dashboard, you received 23 orders today totaling $1,250.50.",
  "audioResponse": "data:audio/mpeg;base64,..."  // Audio-generated response
}
```

### 4.5 Order Management

```
GET /api/mobile/orders?limit=20&offset=0
- List recent orders with status, total, date

GET /api/mobile/orders/{orderId}
- Full order details including items, customer, shipping

PUT /api/mobile/orders/{orderId}/status
{
  "status": "shipped",
  "trackingNumber": "1Z999AA10123456784"
}

POST /api/mobile/orders/{orderId}/label
- Generate and return shipping label (PDF data)
```

### 4.6 Push Notification Registration

```
POST /api/mobile/push-tokens
Authorization: Bearer {accessToken}

{
  "token": "android-fcm-token-here",
  "deviceId": "android-uuid-123",
  "os": "android",
  "osVersion": "14",
  "appVersion": "1.0.0"
}
```

---

## 5. Offline-First Architecture

### 5.1 Local Storage (Hive Database)

**Hive Boxes** (encrypted local key-value storage):
```
dashboardCache → Dashboard metrics snapshot + timestamp
chatHistory → Last 100 chat messages
userProfile → User email, display name, preferences
orders → Cached list of recent orders
offlineQueue → Queue of actions to sync when online
tokens → Stored refreshToken
```

**Sample Box Structure**:
```dart
class DashboardCache {
  final DateTime cachedAt;
  final int pendingOrders;
  final double todayRevenue;
  final List<int> trafficData;
  final bool isOnline;
}
```

### 5.2 Sync Strategy

**Pull (Fetch from Backend)**:
- Dashboard: Every 5 minutes (if app in foreground)
- Chat history: On demand when user opens chat
- Orders: Every 10 minutes (background)

**Push (Send to Backend)**:
- Immediate: Status changes, order actions
- Queued: If offline, retry every 30 seconds when reconnected
- Conflict resolution: Backend change wins (user is not offline-editing)

**Offline Queue Example**:
```json
{
  "queueId": "action-456",
  "type": "UPDATE_ORDER_STATUS",
  "orderId": "order-123",
  "newStatus": "shipped",
  "timestamp": "2024-11-13T15:30:00Z",
  "retryCount": 0,
  "maxRetries": 3
}
```

### 5.3 Conflict Resolution

**Scenario**: User marks order as shipped offline, then online sync shows backend already marked it shipped

**Resolution**:
1. Compare timestamps
2. Backend timestamp newer → Accept backend state
3. Log merge conflict
4. UI shows "Order already marked shipped" → Dismiss action
5. No data loss, user informed

---

## 6. UI Framework & Design System

### 6.1 Design System (Material Design 3)

**Colors**:
- Primary: #2196F3 (Blue) - Actions, buttons, highlights
- Secondary: #4CAF50 (Green) - Success, positive metrics
- Error: #F44336 (Red) - Warnings, order issues
- Background: #FFFFFF or #121212 (dark mode)
- Surface: #F5F5F5

**Typography**:
- Headlines: Roboto, 20-32sp
- Body: Roboto, 14-16sp
- Captions: Roboto, 12sp

**Components**:
- FloatingActionButton: AI chat (primary action)
- BottomNavigationBar: Dashboard, Orders, Products, Chat, Settings
- Cards: Metrics, order previews, chat messages
- AlertDialogs: Confirmations, errors
- SnackBars: Temporary notifications

### 6.2 Screen Layout

**Dashboard Screen**:
```
┌─────────────────────────┐
│  EcommerceStarter       │ ← Header
│  Last sync: 2 min ago   │
├─────────────────────────┤
│ ┌─────────────────────┐ │
│ │ Pending Orders  5   │ │ ← Metric Card (tap → Orders)
│ └─────────────────────┘ │
│ ┌─────────────────────┐ │
│ │ Today's Revenue     │ │
│ │ $1,250.50           │ │
│ └─────────────────────┘ │
│ ┌─────────────────────┐ │
│ │ Website: Online ✓   │ │
│ └─────────────────────┘ │
│                          │
│ Traffic (24h)           │
│ [📈 sparkline chart]    │
│                          │
├─────────────────────────┤
│ ▌ Dashboard             │ ← Bottom nav
│   Orders                │
│   Products              │
│   Chat                  │
│   Settings              │
└─────────────────────────┘
```

**Chat Screen**:
```
┌─────────────────────────┐
│  EcommerceStarter Chat  │ ← Header + back button
├─────────────────────────┤
│                         │
│ System: 3:15 PM         │
│ Ready to help!          │
│                         │
│        ┌─────────────┐  │
│        │ [Suggested] │  │
│        │ Check stats │  │
│        └─────────────┘  │
│        ┌─────────────┐  │
│        │ [Suggested] │  │
│        │ Recent ord  │  │
│        └─────────────┘  │
│                         │
├─────────────────────────┤
│ [🎙️] [Text input]  [→] │ ← Microphone + input + send
└─────────────────────────┘
```

---

## 7. Implementation Roadmap

### Phase 1 - Design & Setup (Week 1)
- [ ] Finalize UI mockups in Figma
- [ ] Set up Flutter project with dependencies
- [ ] Create data models (Order, Dashboard, Chat)
- [ ] Implement repository pattern skeleton
- [ ] Set up local storage (Hive encryption)
- **Deliverable**: Project scaffold + data layer

### Phase 2 - Backend Integration (Week 2)
- [ ] Implement API client (Dio HTTP)
- [ ] Authentication flow (login, token refresh)
- [ ] Dashboard data fetch + caching
- [ ] Order listing API integration
- **Deliverable**: API layer complete, can fetch real data

### Phase 3 - Core UI (Week 2-3)
- [ ] Dashboard screen
- [ ] Bottom navigation
- [ ] Orders list + detail
- [ ] Basic styling with Material Design 3
- **Deliverable**: Functional app shell with dashboard

### Phase 4 - Offline & Sync (Week 3-4)
- [ ] Implement offline-first caching
- [ ] Sync queue for offline actions
- [ ] Conflict resolution logic
- [ ] Network status detection
- **Deliverable**: App works offline, queues sync

### Phase 5 - Chat Integration (Week 4-5)
- [ ] Chat screen UI
- [ ] Voice input integration (Google Speech-to-Text)
- [ ] Chat API integration
- [ ] Message history caching
- **Deliverable**: AI chat works, voice optional

### Phase 6 - Push Notifications (Week 5)
- [ ] Firebase Cloud Messaging setup
- [ ] Notification styling
- [ ] Deep linking to screens
- [ ] Notification permissions handling
- **Deliverable**: Receive and display notifications

### Phase 7 - Testing & Polish (Week 6)
- [ ] Unit tests for repositories
- [ ] Widget tests for UI
- [ ] Integration tests
- [ ] Performance optimization
- [ ] Bug fixes, refinement
- **Deliverable**: Release-ready app

### Phase 8 - Play Store Release (Week 7)
- [ ] Google Play Developer account setup
- [ ] App signing certificate generation
- [ ] Build APK and upload
- [ ] App listing, screenshots, description
- [ ] Submit for review
- **Deliverable**: App on Google Play

---

## 8. Technology Stack

### Frontend (Flutter)
- **Framework**: Flutter 3.16+ (Dart 3.2+)
- **State Management**: Bloc/Cubit (flutter_bloc 8.1+)
- **HTTP Client**: Dio 5.3+ with interceptors
- **Local Storage**: Hive 2.2+ (encrypted)
- **Authentication**: firebase_auth OR custom JWT handling
- **Push Notifications**: firebase_messaging 14.6+
- **Voice**: google_speech 2.0+ OR flutter_sound
- **Charts**: fl_chart 0.65+ (dashboard sparklines)
- **UI**: Material Design 3 (built-in)
- **Testing**: flutter_test, mockito, bloc_test

### Backend (ASP.NET Core - Already in place)
- **Framework**: ASP.NET Core 8.0 with Entity Framework Core
- **APIs**: RESTful endpoints (mobile-specific routes)
- **Authentication**: JWT tokens with refresh
- **Database**: SQL Server 2022
- **Push**: Firebase Cloud Messaging (backend sends)
- **Logging**: Serilog (all API calls logged)

### Cloud Services
- **Firebase**: Cloud Messaging (push notifications)
- **Google Play**: App distribution
- **Optional**: Azure Application Insights (monitoring)

---

## 9. Security Considerations

### 9.1 Data Protection

**In Transit**:
- All API calls HTTPS/TLS 1.2+
- Certificate pinning implemented (prevent MITM)
- Dio interceptors validate certificates

**At Rest**:
- Local database (Hive) encrypted with AES-256
- Encryption key derived from device secure storage
- Sensitive data (tokens) in EncryptedSharedPreferences
- Chat history encrypted in local DB

### 9.2 Authentication & Authorization

**Device Registration**:
- Each device gets unique deviceId (UUID)
- Backend tracks device, can invalidate remotely
- User can view "active devices" and revoke access

**Token Expiration**:
- accessToken: 1 hour (short-lived, memory only)
- refreshToken: 30 days (encrypted storage, server can revoke)
- User logout: Clear tokens immediately

**API Authorization**:
- Every API call validated with JWT token
- Roles enforced (owner can see all, staff limited)
- Backend logs all API calls for audit

### 9.3 Permissions

**Android Permissions Required**:
- `INTERNET` - Network access
- `RECEIVE_FCM_PUSH` - Firebase Cloud Messaging
- `RECORD_AUDIO` - Voice input (chat)
- `READ_EXTERNAL_STORAGE` - Upload files (optional future)

**Permissions Handling**:
- Request at runtime (Android 6.0+)
- User can grant/deny individually
- Graceful fallback if denied (e.g., voice input unavailable)

---

## 10. Performance & Optimization

### 10.1 App Performance Targets

- **Cold start**: < 3 seconds
- **Dashboard load**: < 1.5 seconds (cached) / < 3 seconds (network)
- **Frame rate**: 60 FPS consistently
- **Memory**: < 150 MB typical use
- **Battery**: < 5% drain per hour idle (with notifications enabled)

### 10.2 Optimization Strategies

**Network**:
- Cache TTL: 5 min for dashboard, 1 hour for products
- Lazy loading: Load chat history on scroll, not all at once
- Compression: gzip requests/responses
- Minimal payloads: Only include necessary fields in API responses

**Storage**:
- Prune old chat messages (keep 100 most recent)
- Prune old sync queue (remove after 7 days)
- Max local DB size: 50 MB

**UI**:
- Lazy load screens (build when navigated to)
- Infinite scroll for orders (load 20 at a time)
- Image caching and resizing
- Minimal animations on low-end devices

---

## 11. Monitoring & Analytics

### 11.1 Crash Reporting
- Integrate Firebase Crashlytics
- Automatic stack trace collection
- Backend service errors logged

### 11.2 Analytics Events

Track:
- App opens (daily active users)
- Feature usage (chat used X times/day)
- API latency (diagnose backend issues)
- Offline time (when user has no connectivity)
- Errors (sync failures, API errors)

### 11.3 Backend Integration

- All analytics events batched and sent during low-usage
- Backend stores in database for admin dashboard
- Real-time monitoring of app health

---

## 12. Future Enhancements

### Phase 2 Enhancement (Post-Release)
- [ ] iOS version (leverage shared Dart/Flutter code)
- [ ] Biometric authentication (fingerprint/face)
- [ ] Dark mode (Material You integration)
- [ ] Widgets for dashboard preview
- [ ] Payment processing (mobile checkout)

### Phase 3 Enhancement (6 months)
- [ ] Video tutorials in-app
- [ ] Barcode scanning (inventory management)
- [ ] Augmented reality product preview
- [ ] Offline-first chat (queue messages)
- [ ] Multi-language support

---

## 13. Success Metrics

**Launch Targets (30 days)**:
- 100+ app downloads
- 4.0+ star rating
- 30% daily active user rate
- < 1% crash rate

**6-Month Targets**:
- 1,000+ total downloads
- 4.2+ star rating
- 50% daily active user rate
- Zero critical crashes

---

## 14. Team & Resources

**Team Size**: 2-3 developers (mobile + 1 backend specialist for API adjustments)

**Skills Needed**:
- Flutter/Dart development
- Mobile UI/UX best practices
- REST API design (backend team)
- Firebase services setup

**Timeline**: 6-8 weeks to Play Store launch

---

## Appendix A: API Response Examples

### Dashboard Response (Full)
```json
{
  "timestamp": "2024-11-13T15:30:00Z",
  "metrics": {
    "pendingOrders": 5,
    "todayRevenue": 1250.50,
    "todayOrders": 23,
    "websiteStatus": "online",
    "lastStatusCheck": "2024-11-13T15:29:45Z",
    "traffic": {
      "last24Hours": [120, 145, 167, 189, 201, 198, 210, 225, 234, 267, 245, 223, 195, 167, 145, 134, 123, 112, 98, 87, 76, 65, 54, 43],
      "peak": 267,
      "average": 156
    },
    "inventoryAlerts": 3,
    "supportQueueLength": 2,
    "uptime": 99.97,
    "lastBackupTime": "2024-11-13T03:00:00Z"
  }
}
```

---

**Document Version**: 1.0
**Last Updated**: 2024-11-13
**Status**: Ready for Phase 2 Development
