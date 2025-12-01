# CV-Analyzer Testing Guide

## Application Status

✅ **All services are running successfully!**

### Running Services

| Service | Status | URL | Port |
|---------|--------|-----|------|
| Frontend (Angular) | ✅ Running | http://localhost:4200 | 4200 |
| Backend API (.NET) | ✅ Running | http://localhost:5000/api | 5000 |
| SQL Server | ✅ Running | localhost | 1433 |
| Azurite (Storage Emulator) | ✅ Running | localhost | 10000-10002 |

### Service Health

- **API Health**: ✅ Healthy (http://localhost:5000/api/health)
- **Database**: ✅ Migrations applied, ready
- **Background Worker**: ✅ Resume analysis worker running
- **Azure Services**: ✅ Connected (Storage, OpenAI GPT-4o, Document Intelligence)

---

## Manual Testing Instructions

### 1. Open the Application

**Frontend**: Navigate to http://localhost:4200 in your web browser

You should see the CV-Analyzer landing page with an upload interface.

---

### 2. Test Guest Resume Upload (No Authentication Required)

**Feature**: Guest users can upload resumes without creating an account. Resumes are stored for 24 hours.

**Steps**:
1. Go to http://localhost:4200
2. Click on the upload area or drag & drop a PDF resume
3. Wait for the analysis to complete (typically 10-30 seconds)
4. View the results:
   - ATS Score (0-100)
   - Extracted candidate information (name, email, skills, experience)
   - Improvement suggestions categorized by priority

**What's Happening Behind the Scenes**:
- PDF is uploaded to Azure Blob Storage
- Message queued for async processing
- Document Intelligence extracts text from PDF
- GPT-4o analyzes resume and generates suggestions
- Frontend polls for status updates

**Expected Results**:
- ✅ File uploads successfully
- ✅ Analysis completes with score and suggestions
- ✅ Candidate info extracted (name, email, skills, etc.)
- ✅ Anonymous session created (24-hour expiration)

---

### 3. Test User Registration

**Feature**: Users can create an account to save resumes permanently.

**Steps**:
1. Click "Register" button (top right)
2. Fill in the registration form:
   - Email: Any valid email format
   - Password: Must meet requirements (8+ chars, uppercase, lowercase, digit, special char)
   - Full Name: Your name
   - Phone: Optional, format: +1234567890
3. Click "Register"

**Expected Results**:
- ✅ Registration successful
- ✅ JWT token received and stored
- ✅ Redirect to dashboard
- ✅ User profile created in database

**Password Requirements**:
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- At least 1 special character (!@#$%^&*)

**Test Credentials**:
```
Email: testuser@example.com
Password: Test123!@#
```

---

### 4. Test User Login

**Feature**: Registered users can log in to access their dashboard.

**Steps**:
1. Click "Login" button (top right)
2. Enter credentials:
   - Email: The email you registered with
   - Password: Your password
3. Click "Login"

**Expected Results**:
- ✅ Login successful
- ✅ JWT token received (60-minute expiration)
- ✅ Redirect to dashboard
- ✅ User profile loaded

**Error Cases to Test**:
- ❌ Invalid email format → Validation error
- ❌ Wrong password → "Invalid credentials"
- ❌ Non-existent user → "Invalid credentials"

---

### 5. Test User Dashboard

**Feature**: Authenticated users can view all their uploaded resumes with statistics.

**URL**: http://localhost:4200/dashboard (requires login)

**Dashboard Components**:

#### User Profile Section
- Avatar (initials)
- Full name
- Email address
- Phone number
- Join date
- Email verification status (pending implementation)

#### Statistics Cards
- **Total Resumes**: Count of uploaded resumes
- **Average Score**: Mean ATS score across all resumes
- **Last Analysis**: Timestamp of most recent upload

#### Resume Grid
- List of all user's resumes
- Each card shows:
  - File name
  - Upload date
  - ATS score with color coding:
    - 🟢 Green: 80-100 (Excellent)
    - 🔵 Blue: 60-79 (Good)
    - 🟠 Orange: 40-59 (Needs Improvement)
    - 🔴 Red: 0-39 (Poor)
  - Status badge (Completed/Processing)
  - "View Details" button

**Expected Results**:
- ✅ Dashboard loads successfully
- ✅ User info displayed correctly
- ✅ Statistics calculated accurately
- ✅ Resume list shows all user's uploads
- ✅ Click "View Details" navigates to analysis page

**Empty State**:
- If no resumes uploaded yet, shows friendly message with "Upload Resume" button

---

### 6. Test Resume Analysis Page

**Feature**: View detailed analysis results for a specific resume.

**URL**: http://localhost:4200/analysis/{resumeId}

**Components**:

#### Header Section
- File name
- Upload date
- ATS Score (large, color-coded)

#### Candidate Information Card
- Extracted data:
  - Full Name
  - Email
  - Phone
  - Location
  - Years of Experience
  - Current Job Title
  - Education
  - Skills (tags)

#### Suggestions Section
- Grouped by priority:
  - 🔴 High Priority
  - 🟠 Medium Priority
  - 🟢 Low Priority
- Each suggestion shows:
  - Category (e.g., "Keywords", "Formatting", "Experience")
  - Description
  - Actionable advice

**Actions**:
- Refresh button (re-fetch latest status)
- Download button (future feature)
- "Upload Another" button

**Expected Results**:
- ✅ Analysis data loads correctly
- ✅ Candidate info displayed
- ✅ Suggestions grouped by priority
- ✅ Score matches dashboard value

---

### 7. Test Authentication & Authorization

**Feature**: Protected routes require authentication; guests redirected appropriately.

**Test Cases**:

#### Protected Routes (Require Login)
1. Try accessing http://localhost:4200/dashboard without logging in
   - ✅ Redirected to /login

2. Try accessing http://localhost:4200/dashboard with valid token
   - ✅ Dashboard loads successfully

#### Guest Routes (Redirect Authenticated Users)
1. Try accessing http://localhost:4200/login while logged in
   - ✅ Redirected to /dashboard

2. Try accessing http://localhost:4200/register while logged in
   - ✅ Redirected to /dashboard

#### Token Expiration
1. Wait 60 minutes after login
2. Try accessing dashboard
   - ✅ Token expired, redirected to login

#### Logout
1. Click "Logout" button in dashboard
2. Verify:
   - ✅ Token cleared from localStorage
   - ✅ Redirected to home page
   - ✅ Cannot access dashboard without re-login

---

### 8. Test Guest Resume Migration

**Feature**: When a guest uploads resumes and then registers, their resumes are migrated to their account.

**Steps**:
1. **As Guest**: Upload a resume without logging in
2. Note the resume ID from the analysis page URL
3. **Register**: Create a new account
4. **Check Dashboard**: The guest resume should now appear in your dashboard

**Expected Results**:
- ✅ Guest resume appears in user's dashboard
- ✅ Resume ownership transferred to authenticated user
- ✅ `AuthenticatedUserId` column updated in database

---

## API Testing with cURL (Advanced)

### 1. Health Check
```bash
curl http://localhost:5000/api/health
```

### 2. Register User
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"test@example.com\",\"password\":\"Test123!@#\",\"fullName\":\"Test User\"}"
```

### 3. Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"test@example.com\",\"password\":\"Test123!@#\"}"
```

### 4. Get Profile (requires token)
```bash
TOKEN="your_jwt_token_here"
curl http://localhost:5000/api/auth/profile \
  -H "Authorization: Bearer $TOKEN"
```

### 5. Get User Resumes (requires token)
```bash
TOKEN="your_jwt_token_here"
curl http://localhost:5000/api/auth/resumes \
  -H "Authorization: Bearer $TOKEN"
```

---

## Database Inspection (Advanced)

### Connect to SQL Server

```bash
docker exec -it cv-analyzer-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C
```

### Useful SQL Queries

```sql
-- Check registered users
SELECT Id, Email, FullName, CreatedAt, LastLoginAt FROM Users;

-- Check all resumes
SELECT Id, FileName, Status, Score, AnalyzedAt, IsAnonymous FROM Resumes;

-- Check authenticated user resumes
SELECT r.FileName, r.Score, u.Email
FROM Resumes r
INNER JOIN Users u ON r.AuthenticatedUserId = u.Id;

-- Check suggestions for a resume
SELECT Category, Description, Priority
FROM Suggestions
WHERE ResumeId = 'resume-guid-here';
```

---

## Common Issues & Troubleshooting

### Frontend Not Loading
- **Issue**: http://localhost:4200 doesn't load
- **Solution**: Check container status: `docker-compose ps frontend`
- **Logs**: `docker-compose logs frontend`

### API Returns 500 Errors
- **Issue**: API calls failing
- **Solution**: Check API logs: `docker-compose logs api`
- **Common Causes**:
  - Azure credentials expired
  - Database connection failed
  - Missing migrations

### Resume Analysis Stuck at "Processing"
- **Issue**: Resume status stays "Pending" for too long
- **Solution**:
  1. Check background worker logs: `docker-compose logs api | grep "ResumeAnalysisWorker"`
  2. Check Azure queue: Verify messages are being processed
  3. Check Azure OpenAI quota: Ensure API key is valid and not rate-limited

### Authentication Errors
- **Issue**: "Unauthorized" errors
- **Solution**:
  1. Check JWT token in localStorage (browser DevTools → Application → Local Storage)
  2. Verify token hasn't expired (60-minute lifetime)
  3. Check API logs for authentication errors

---

## Feature Completeness Status

### ✅ Implemented Features
- Guest resume upload and analysis
- User registration with password validation
- User login with JWT authentication
- User dashboard with resume list
- Resume analysis with GPT-4o
- Candidate information extraction
- ATS score calculation
- Priority-based suggestions
- Guest resume migration on registration
- 24-hour anonymous data cleanup
- Background async processing
- Responsive design (mobile-friendly)

### 🚧 Pending Features (from User Story 2)
- Email verification (in progress)
- Registration prompt modal after guest upload
- Email verification link
- Refresh token mechanism (tokens expire after 60 mins)
- Password reset flow

### 🔮 Future Enhancements (Roadmap)
- Resume versioning (track multiple versions)
- Comparison mode (compare two resumes)
- Export to PDF/Word
- Multi-language support (i18n)
- Advanced analytics dashboard
- Migration to Durable Agents (stateful AI conversations)

---

## Performance Metrics

### Expected Response Times
- **Health Check**: < 5ms
- **User Registration**: 50-100ms
- **User Login**: 50-100ms
- **Resume Upload**: 200-500ms (file upload only)
- **Resume Analysis**: 10-30 seconds (full pipeline)
  - PDF Text Extraction: 2-5 seconds
  - GPT-4o Analysis: 5-20 seconds
  - Database Save: < 100ms

### Concurrent Users
- **Current Capacity**: Tested with 10 concurrent users
- **Scalability**: Designed for horizontal scaling with Azure Container Apps

---

## Security Notes

### Implemented Security Measures
- ✅ Password hashing with BCrypt (cost factor 12)
- ✅ JWT token-based authentication
- ✅ HTTPS enforcement (production)
- ✅ SQL injection prevention (parameterized queries)
- ✅ XSS prevention (Angular sanitization)
- ✅ CORS configuration
- ✅ Secret management (Azure Key Vault in production)
- ✅ Pre-commit secret scanning (Gitleaks)

### Security Testing
1. **SQL Injection**: Try entering `' OR '1'='1` in login form → Should be blocked
2. **XSS**: Try entering `<script>alert('xss')</script>` in form fields → Should be sanitized
3. **Token Tampering**: Modify JWT token in localStorage → Should return 401 Unauthorized
4. **Brute Force**: Try multiple failed logins → Currently no rate limiting (future enhancement)

---

## Test Data

### Sample Test Accounts

Create these accounts for testing:

```
Account 1:
Email: alice@example.com
Password: Alice123!@#
Full Name: Alice Johnson

Account 2:
Email: bob@example.com
Password: Bob123!@#
Full Name: Bob Smith
```

### Sample Resumes

For testing, you can:
1. Use your own PDF resume
2. Generate a sample resume online
3. Use any PDF file (the system will attempt to extract text)

**Recommended Resume Content** (for better analysis):
- Contact information (name, email, phone)
- Professional summary
- Work experience with dates
- Education history
- Skills section
- Achievements/accomplishments

---

## Next Steps

1. **Test All Features**: Go through steps 1-8 above
2. **Report Issues**: If you find bugs, document them with:
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Screenshots (if applicable)
3. **Review Logs**: Check `docker-compose logs` for any errors
4. **Check Documentation**: Review `docs/` folder for architecture details

---

## Stopping the Application

When done testing:

```bash
# Stop all containers
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

---

## Support

- **Documentation**: See `docs/` folder
- **Architecture**: `docs/ARCHITECTURE.md`
- **Security**: `docs/SECURITY.md`
- **API Reference**: http://localhost:5000/swagger (when API is running)
