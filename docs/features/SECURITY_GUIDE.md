# ?? Security & Rate Limiting Guide
## MyStore Supply Co.

Complete guide to security features, intrusion detection, rate limiting, and audit logging.

---

## Table of Contents

1. [Overview](#overview)
2. [Security Settings](#security-settings)
3. [Rate Limiting](#rate-limiting)
4. [IP Blocking](#ip-blocking)
5. [Audit Logging](#audit-logging)
6. [Testing & Verification](#testing--verification)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The application includes comprehensive security features:

- ? **Rate Limiting** - Prevents abuse by limiting requests per user/IP
- ? **IP Blocking** - Automatic and manual blocking of malicious IPs
- ? **Account Lockout** - Protects against brute force attacks
- ? **Audit Logging** - Records all security events
- ? **IP Whitelisting/Blacklisting** - Manual control over IP access

### Key Features

**Three-Tier Rate Limiting:**
- Anonymous users (tracked by IP)
- Authenticated customers (tracked by email)
- Admins (can be exempted)

**Automatic Protections:**
- Failed login detection
- Excessive request blocking
- Account lockout after failed attempts

**Admin Controls:**
- Security Settings page (`/Admin/Settings/Security`)
- Audit Log viewer (`/Admin/Security/AuditLog`)
- Manual IP blocking/unblocking

---

## Security Settings

### Accessing Settings

**Location:** `/Admin/Settings/Security`

**Requirements:**
- Must be logged in as Admin
- Navigate: Admin Dashboard ? Settings ? Security Settings

### Configuration Sections

#### 1. Rate Limiting Settings

Controls request rate limits for all users.

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Rate Limiting** | ? ON | Master switch for all rate limiting |
| **Exempt Admins from Rate Limiting** | ? ON | Allow unlimited admin requests |
| **Max Requests Per Minute (General)** | 60 | Regular page requests per minute |
| **Max Requests Per Second (General)** | 20 | Regular page requests per second |
| **Max Requests Per Minute (Auth)** | 10 | Login/register/admin pages per minute |
| **Max Requests Per Second (Auth)** | 3 | Login/register/admin pages per second |

**Endpoint Categories:**
- **General Endpoints:** Public pages like `/Products`, `/Cart`, homepage
- **Auth Endpoints:** `/Account/Login`, `/Account/Register`, `/Admin/*`

**User Type Tracking:**
```
????????????????????????????????????????????????????????????????????
? User Type       ? Tracked By      ? General Limits ? Auth Limits ?
????????????????????????????????????????????????????????????????????
? Guest           ? IP Address      ? 60/min, 20/sec ? 10/min, 3/sec?
? Customer        ? Email Address   ? 60/min, 20/sec ? 10/min, 3/sec?
? Admin (exempt)  ? N/A (bypassed)  ? Unlimited      ? Unlimited   ?
? Admin (not ex.) ? Email Address   ? 60/min, 20/sec ? 10/min, 3/sec?
? Whitelisted IP  ? N/A (bypassed)  ? Unlimited      ? Unlimited   ?
????????????????????????????????????????????????????????????????????
```

#### 2. IP Blocking Settings

Automatic IP blocking after failed login attempts.

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable IP Blocking** | ? ON | Auto-block IPs with excessive failures |
| **Max Failed Login Attempts** | 5 | Failed logins before blocking |
| **Time Window (Minutes)** | 15 | Window to count failed attempts |
| **Block Duration (Minutes)** | 30 | How long IP stays blocked |

**Example Scenario:**
```
User makes 5 failed login attempts in 15 minutes
? IP is blocked for 30 minutes
? Security event logged
? IP appears in Blocked IPs list
```

#### 3. Account Lockout Settings

Locks user accounts (not IPs) after failed attempts.

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Account Lockout** | ? ON | Lock accounts after failures |
| **Max Failed Attempts** | 5 | Failed logins before lockout |
| **Lockout Duration (Minutes)** | 15 | How long account stays locked |

**Difference from IP Blocking:**
- **IP Blocking:** Blocks the source IP address
- **Account Lockout:** Locks the specific user account

Both can be active simultaneously.

#### 4. Audit Logging Settings

Records security events to database.

| Setting | Default | Description |
|---------|---------|-------------|
| **Enable Security Audit Logging** | ? ON | Record security events |
| **Log Retention Period (Days)** | 90 | Auto-delete logs older than this |

**Logged Events:**
- SuccessfulLogin
- FailedLogin
- IpBlocked
- IpUnblocked
- BlockedIpAttempt
- LoginLockedOut
- BlacklistedIpAttempt
- LoginBlockedExcessiveAttempts

#### 5. Advanced Settings

Manual IP control for special cases.

**Whitelisted IPs:**
- Bypass ALL security checks
- No rate limiting
- No IP blocking
- Format: `192.168.1.1, 10.0.0.5` (comma-separated)
- **Use Case:** Office IPs, developer IPs, trusted partners

**Blacklisted IPs (Permanent Block):**
- Permanently blocked until manually removed
- Cannot access site at all
- Returns 403 Forbidden
- Format: `203.0.113.5, 198.51.100.10`
- **Use Case:** Known malicious IPs, persistent attackers

?? **Warning:** Be very careful with blacklisting. Blocked IPs cannot access the site until manually unblocked.

---

## Rate Limiting

### How It Works

**Request Flow:**
```
1. Request arrives ? Rate Limiting Middleware
   ?
2. Is rate limiting enabled globally?
   NO ? Allow request ?
   YES ? Continue
   ?
3. Is IP whitelisted?
   YES ? Allow request ? (bypass all checks)
   NO ? Continue
   ?
4. Is user authenticated?
   NO ? Track by IP address
   YES ? Continue
   ?
5. Is user Admin AND exemption enabled?
   YES ? Allow request ? (bypass rate limits)
   NO ? Continue
   ?
6. Determine endpoint type (general vs auth)
   ?
7. Check request counters (per-second and per-minute)
   ?
8. Allow or Return 429 (Too Many Requests)
```

### Rate Limit Response Headers

All responses include standard rate limit headers:

```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 42
X-RateLimit-Reset: 1736891400
```

On rate limit exceeded:
```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 0
Retry-After: 60
```

### Testing Rate Limits

**Test Page:** `/Admin/TestRateLimit`

**Features:**
- Shows current security settings
- Shows user authentication status
- "Make Request" button (single test)
- "Rapid Test (50 requests)" button

**Test Procedure:**
1. Navigate to `/Admin/TestRateLimit`
2. Check "Current Security Settings" shows:
   - Rate Limiting Enabled: YES (green)
   - Exempt Admins: YES (green)
3. Click "Rapid Test (50 requests)"
4. All 50 should succeed if admin exemption is on
5. If rate limited, check settings

### Scenarios by User Type

#### Guest User (Not Logged In)

**Behavior:**
- Tracked by IP address: `192.168.1.100`
- Multiple guests from same IP share quota
- Standard limits: 60 req/min, 20 req/sec

**Example:**
```
Home (IP: 192.168.1.100):
- Dad browsing: 20 requests
- Mom browsing: 20 requests  
- Kid browsing: 20 requests
Total: 60 requests ? All hit shared limit
```

#### Customer (Logged In)

**Behavior:**
- Tracked by email: `customer@example.com`
- Independent quota from IP
- Other users on same IP don't affect quota

**Example:**
```
Office (IP: 203.0.113.50):
- customer-a@example.com: 50 requests ? (own quota)
- customer-b@example.com: 50 requests ? (own quota)
- Anonymous user: 50 requests ? (IP quota, separate)
All succeed independently!
```

#### Admin (Exemption Enabled)

**Behavior:**
- Bypasses all rate limiting
- Unlimited requests
- Logged as "bypassing rate limiting"

**Example:**
```
Admin makes 1000 requests in 1 minute
? All succeed
? Logs: "Admin user admin@example.com bypassing rate limiting"
```

#### Admin (Exemption Disabled)

**Behavior:**
- Tracked by email like customers
- Auth endpoint limits apply (stricter)
- Standard limits on general endpoints

**Example:**
```
Admin accesses /Admin/Dashboard 15 times rapidly
? First 10 succeed
? Last 5 get 429 (auth endpoint limit: 10/min)
```

---

## IP Blocking

### Automatic Blocking

**Triggers:**
1. Exceeds `MaxFailedLoginAttempts` in `FailedLoginWindowMinutes`
2. Automatic block for `IpBlockDurationMinutes`
3. Security event logged
4. IP added to blocked list

**Example Flow:**
```
1. IP 203.0.113.10 tries login with wrong password (attempt 1)
2. Tries again (attempt 2)
3. Tries again (attempt 3)
4. Tries again (attempt 4)
5. Tries again (attempt 5) ? BLOCKED for 30 minutes
6. Logged as "LoginBlockedExcessiveAttempts"
```

### Manual Blocking

**Via Blacklist:**
1. Go to `/Admin/Settings/Security`
2. Scroll to "Blacklisted IP Addresses"
3. Add IP: `203.0.113.10`
4. Click "Save Settings"
5. IP is permanently blocked (403 Forbidden)

### Viewing Blocked IPs

**Location:** `/Admin/Security/AuditLog`

**Blocked IPs Section Shows:**
- IP Address
- Blocked At (timestamp)
- Expires At (or "Permanent")
- Reason
- Offense Count
- Type (Temporary/Permanent)
- Unblock button

**Example:**
```
???????????????????????????????????????????????????????????????????????????????????????
? IP Address   ? Blocked At     ? Expires At ? Reason              ? Count? Type      ?
???????????????????????????????????????????????????????????????????????????????????????
? 203.0.113.10 ? 1/15 10:30 AM  ? 11:00 AM   ? 5 failed attempts   ? 5    ? Temporary ?
? 198.51.100.5 ? 1/14 2:00 PM   ? Permanent  ? Manual blacklist    ? 1    ? Permanent ?
???????????????????????????????????????????????????????????????????????????????????????
```

### Unblocking IPs

**Manual Unblock:**
1. Go to `/Admin/Security/AuditLog`
2. Find IP in "Blocked IP Addresses" table
3. Click "Unblock" button
4. Confirm action
5. IP is immediately unblocked
6. Event logged as "IpUnblocked"

**Automatic Unblock:**
- Temporary blocks expire after `IpBlockDurationMinutes`
- Permanent blocks must be manually removed

---

## Audit Logging

### Viewing Audit Log

**Location:** `/Admin/Security/AuditLog`

**Sections:**
1. **Blocked IP Addresses** - Currently blocked IPs
2. **Recent Security Events** - Last 100 events

### Security Events Table

**Columns:**
- **Timestamp** - When event occurred (local time)
- **Event Type** - Type of security event (with icon)
- **Severity** - Critical/High/Medium/Low (color-coded)
- **IP Address** - Source IP
- **User** - Email address (or "N/A")
- **Details** - Event description
- **Endpoint** - URL path
- **Status** - Blocked or Allowed

**Event Types & Icons:**
- ? **SuccessfulLogin** - Green checkmark
- ? **FailedLogin** - Red X
- ?? **IpBlocked** - Ban icon (red)
- ??? **BlockedIpAttempt** - Shield X (red)
- ?? **LoginLockedOut** - Lock (yellow)
- ?? **BlacklistedIpAttempt** - Shield exclamation (red)
- ?? **IpUnblocked** - Unlock (green)
- ? **LoginBlockedExcessiveAttempts** - Shield slash (red)

**Severity Color Coding:**
- **Critical** - Red background, red badge
- **High** - Yellow background, yellow badge
- **Medium** - Blue badge
- **Low** - Gray badge

### Log Retention

**Default:** 90 days

**Automatic Cleanup:**
- Logs older than `AuditLogRetentionDays` are auto-deleted
- Runs periodically (implement background job if needed)

**Manual Cleanup:**
- Currently not implemented
- Could add "Clear Old Logs" button

---

## Testing & Verification

### Test Scenarios

#### Scenario 1: Guest Rate Limiting

**Setup:**
- Log out (be anonymous)
- Rate Limiting: ON
- Admin Exemption: N/A (you're not admin)

**Test:**
```bash
# Make 70 requests rapidly to /Products
for i in {1..70}; do curl https://localhost:7001/Products; done

# Expected:
# Requests 1-60: 200 OK
# Requests 61-70: 429 Too Many Requests
```

**Verify:**
- Check response headers: `X-RateLimit-Remaining` decreases
- Last 10 requests return 429
- Audit log shows no entries (not a security event)

#### Scenario 2: Customer Rate Limiting

**Setup:**
- Log in as customer@example.com
- Rate Limiting: ON

**Test:**
```bash
# Make 70 requests to /Products
for i in {1..70}; do curl -b cookies.txt https://localhost:7001/Products; done

# Expected:
# Requests 1-60: 200 OK
# Requests 61-70: 429 Too Many Requests
```

**Verify:**
- Customer has independent quota from other users
- Check audit log (no entry for rate limits, only security events)

#### Scenario 3: Admin Exemption

**Setup:**
- Log in as admin@example.com
- Rate Limiting: ON
- Exempt Admins: ON

**Test:**
```bash
# Navigate to /Admin/TestRateLimit
# Click "Rapid Test (50 requests)"
```

**Expected:**
- All 50 requests succeed
- Message: "SUCCESS! All 50 requests succeeded!"
- Logs show: "Admin user admin@example.com bypassing rate limiting"

#### Scenario 4: Failed Login Detection

**Setup:**
- Log out
- Max Failed Login Attempts: 5
- Failed Login Window: 15 minutes

**Test:**
1. Go to `/Account/Login`
2. Enter valid email: `test@example.com`
3. Enter wrong password 5 times rapidly

**Expected:**
- First 4 attempts: "Invalid login attempt"
- 5th attempt: "Too many failed login attempts. Your IP has been temporarily blocked."
- IP added to Blocked IPs list
- Audit log shows 5 FailedLogin events + 1 LoginBlockedExcessiveAttempts

#### Scenario 5: Account Lockout

**Setup:**
- Create test account: lockout-test@example.com
- Account Lockout: ON
- Max Attempts: 5

**Test:**
1. Try logging in with wrong password 5 times
2. Try logging in with CORRECT password

**Expected:**
- After 5 failures: "This account has been locked out. Please try again later."
- Correct password doesn't work during lockout
- After 15 minutes: Lockout expires, correct password works
- Audit log shows LoginLockedOut event

#### Scenario 6: Whitelisted IP

**Setup:**
1. Note your current IP (check audit log or use `ipconfig`)
2. Add IP to Whitelisted IPs: `192.168.1.100`
3. Save settings

**Test:**
```bash
# Make 1000 requests rapidly
for i in {1..1000}; do curl https://localhost:7001/Products; done
```

**Expected:**
- All 1000 succeed (no rate limiting)
- Logs show: "Whitelisted IP 192.168.1.100 bypassing rate limiting"

#### Scenario 7: Blacklisted IP

?? **Warning:** Test from a test IP or VM, not your main dev machine!

**Setup:**
1. Add test IP to Blacklisted IPs
2. Save settings

**Test:**
- Try accessing any page from that IP

**Expected:**
- All requests return 403 Forbidden
- Message: "Access denied. Your IP address is permanently blocked."
- Audit log shows BlacklistedIpAttempt events

---

## Troubleshooting

### Issue: Admin Still Getting Rate Limited

**Symptoms:**
- Admin user gets 429 errors
- Expected unlimited access

**Diagnostic Steps:**

1. **Check Security Settings:**
```
/Admin/Settings/Security
? Enable Rate Limiting: CHECKED
? Exempt Admins from Rate Limiting: CHECKED
Click "Save Settings"
```

2. **Verify User is Admin:**
```
/Admin/TestRateLimit
Check "Current User Info":
- Authenticated: True
- Is Admin: True
```

3. **Check Current Settings:**
```
/Admin/TestRateLimit
Check "Current Security Settings":
- Rate Limiting Enabled: YES (green badge)
- Exempt Admins: YES (green badge)
```

4. **Check Application Logs:**
```powershell
# Look for bypass messages
Get-Content logs\app.log | Select-String "bypassing rate limiting"

# Should see:
# "Admin user admin@example.com (IP: 127.0.0.1) bypassing rate limiting on /Admin/Dashboard"
```

5. **Restart Application:**
```powershell
# Settings are cached for 5 minutes
# Restart to clear cache
iisreset  # If using IIS
# OR restart app pool
# OR stop/start dotnet process
```

**Solutions:**
- ? Enable admin exemption in settings
- ? Wait 5 minutes for cache to expire
- ? Restart application to clear cache
- ? Verify user has "Admin" role in database

### Issue: Rate Limit Headers Missing

**Symptoms:**
- `X-RateLimit-Limit` header not in responses

**Check:**

1. **Middleware Order (Program.cs):**
```csharp
app.UseAuthentication();  // Must be first
app.UseAuthorization();   // Must be second
app.UseRateLimiting();    // Must be after auth
```

2. **Response Already Started:**
- Headers must be added before response body starts
- Check for early `Response.WriteAsync()` calls

3. **Reverse Proxy Stripping Headers:**
- Check if IIS/nginx is removing custom headers
- Add headers to allowed list

### Issue: Multiple Users Same IP All Rate Limited

**Expected Behavior:**
- **Authenticated users:** Should each have own quota (tracked by email)
- **Anonymous users:** Should share IP quota

**If Authenticated Users Sharing Limits:**

1. **Check User Identifier in Logs:**
```powershell
Get-Content logs\app.log | Select-String "Rate limit exceeded"
# Look for: "Identifier: customer@example.com"
# Should be email, NOT IP
```

2. **Verify Authentication:**
- Check user is actually logged in
- Verify `context.User.Identity.IsAuthenticated == true`

3. **Check Middleware Logic:**
```csharp
// Should use email for authenticated users
string userIdentifier = isAuthenticated 
    ? context.User.Identity?.Name ?? ipAddress 
    : ipAddress;
```

### Issue: Failed Logins Not Triggering Block

**Symptoms:**
- Make 10 failed login attempts
- No block occurs

**Check:**

1. **IP Blocking Enabled:**
```
/Admin/Settings/Security
? Enable IP Blocking: CHECKED
```

2. **Check Settings Values:**
- Max Failed Login Attempts: Should be reasonable (5-10)
- Time Window: Should be reasonable (15-30 minutes)

3. **IP is Whitelisted:**
```
/Admin/Settings/Security
Check "Whitelisted IP Addresses"
- Your IP should NOT be in this list
```

4. **Check Audit Log:**
```
/Admin/Security/AuditLog
Look for FailedLogin events
- Should see entries for each failed attempt
```

5. **Check Application Logs:**
```powershell
Get-Content logs\app.log | Select-String "Failed login"
```

### Issue: Blocked IP Won't Unblock

**Symptoms:**
- Click "Unblock" button
- IP still blocked

**Check:**

1. **Permanent vs Temporary:**
- Permanent blocks (blacklist) need manual removal from blacklist
- Check if IP is in Blacklisted IPs setting

2. **Database Not Updating:**
```sql
-- Check BlockedIps table
SELECT * FROM BlockedIps WHERE IpAddress = '203.0.113.10'

-- Should show IsActive = 0 after unblock
```

3. **Check Success Message:**
- After unblock, should see green success alert
- If no message, check browser console for errors

4. **Clear Application Cache:**
- Restart application
- IP blocking might be cached

### Issue: Audit Log Empty

**Symptoms:**
- No security events showing
- Events expected but not appearing

**Check:**

1. **Audit Logging Enabled:**
```
/Admin/Settings/Security
? Enable Security Audit Logging: CHECKED
```

2. **Database Connection:**
```powershell
# Check connection string
Get-Content appsettings.json | Select-String "DefaultConnection"
```

3. **Check Database Table:**
```sql
-- Verify table exists and has data
SELECT COUNT(*) FROM SecurityAuditEvents
SELECT TOP 10 * FROM SecurityAuditEvents ORDER BY Timestamp DESC
```

4. **Check Application Logs:**
```powershell
Get-Content logs\app.log | Select-String "SecurityAuditService"
```

### Performance Issues

**Symptoms:**
- Slow response times
- High CPU usage

**Possible Causes:**

1. **Too Many Request Counters:**
```powershell
# Check counter count in logs
Get-Content logs\app.log | Select-String "RequestCounter"
```

**Solution:** Counters auto-cleanup after 2 minutes of inactivity

2. **Database Audit Logging:**
- Every request writes to database
- Can be slow with high traffic

**Solutions:**
- ? Use async logging (already implemented)
- ? Batch log writes
- ? Use memory queue + background worker
- ? Disable audit logging in high-traffic scenarios

3. **Settings Cache:**
- Settings loaded every request
- Cache duration: 5 minutes

**Solution:** Increase cache duration if needed
```csharp
private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10); // Increase
```

---

## Configuration Best Practices

### Development Environment

```
EnableRateLimiting: true
ExemptAdminsFromRateLimiting: true
MaxRequestsPerMinute: 120 (relaxed)
MaxRequestsPerSecond: 40
MaxFailedLoginAttempts: 10 (relaxed)
EnableIpBlocking: false (or very high limits)
EnableAccountLockout: false (or very high limits)
```

**Rationale:** Don't lock yourself out during testing!

### Staging Environment

```
EnableRateLimiting: true
ExemptAdminsFromRateLimiting: true
MaxRequestsPerMinute: 60
MaxRequestsPerSecond: 20
MaxRequestsPerMinuteAuth: 10
MaxRequestsPerSecondAuth: 3
EnableIpBlocking: true
EnableAccountLockout: true
```

**Rationale:** Match production but keep admin exemption for testing.

### Production Environment

```
EnableRateLimiting: true
ExemptAdminsFromRateLimiting: true
MaxRequestsPerMinute: 60 (adjust based on traffic)
MaxRequestsPerSecond: 20
MaxRequestsPerMinuteAuth: 5 (stricter)
MaxRequestsPerSecondAuth: 2 (stricter)
MaxFailedLoginAttempts: 5
IpBlockDurationMinutes: 60 (longer)
EnableIpBlocking: true
EnableAccountLockout: true
AuditLogRetentionDays: 90
```

**Rationale:** Balanced security without impacting legitimate users.

### High-Traffic Site

```
EnableRateLimiting: true
MaxRequestsPerMinute: 300 (much higher)
MaxRequestsPerSecond: 60 (much higher)
MaxRequestsPerMinuteAuth: 20 (still strict on auth)
MaxRequestsPerSecondAuth: 5
EnableAuditLogging: false (optional, for performance)
```

**Rationale:** Don't rate limit legitimate high traffic, but still protect auth endpoints.

---

## Quick Reference

### Admin Pages

| Page | URL | Purpose |
|------|-----|---------|
| **Security Settings** | `/Admin/Settings/Security` | Configure all security settings |
| **Audit Log** | `/Admin/Security/AuditLog` | View security events & blocked IPs |
| **Test Rate Limit** | `/Admin/TestRateLimit` | Test admin exemption |

### Default Settings

| Setting | Default Value |
|---------|--------------|
| Enable Rate Limiting | ? ON |
| Exempt Admins | ? ON |
| Max Requests/Min (General) | 60 |
| Max Requests/Sec (General) | 20 |
| Max Requests/Min (Auth) | 10 |
| Max Requests/Sec (Auth) | 3 |
| Enable IP Blocking | ? ON |
| Max Failed Login Attempts | 5 |
| Failed Login Window | 15 min |
| IP Block Duration | 30 min |
| Enable Account Lockout | ? ON |
| Account Lockout Max Attempts | 5 |
| Account Lockout Duration | 15 min |
| Enable Audit Logging | ? ON |
| Audit Log Retention | 90 days |

### Common Commands

**Check if IP is blocked:**
```sql
SELECT * FROM BlockedIps WHERE IpAddress = '203.0.113.10' AND IsActive = 1
```

**View recent failed logins:**
```sql
SELECT TOP 10 * FROM SecurityAuditEvents 
WHERE EventType = 'FailedLogin' 
ORDER BY Timestamp DESC
```

**Count rate limit events (if logging to DB):**
```sql
SELECT COUNT(*) FROM SecurityAuditEvents 
WHERE EventType LIKE '%RateLimit%'
```

**Clear old audit logs:**
```sql
DELETE FROM SecurityAuditEvents 
WHERE Timestamp < DATEADD(day, -90, GETDATE())
```

---

## Related Documentation

- **ADMIN_GUIDE.md** - Complete admin panel documentation
- **CONFIGURATION_GUIDE.md** - Environment and database setup
- **FEATURES_GUIDE.md** - Core application features

---

**Last Updated:** January 2025  
**Framework:** ASP.NET Core 8.0 (Razor Pages)  
**Author:** MyStore Development Team
