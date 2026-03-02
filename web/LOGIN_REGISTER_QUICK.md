# ? Login & Registration - Quick Guide

## ?? **Two Separate Pages**

### **1. Registration Page** ? `/register`
- **Purpose**: Create new account
- **API**: `POST /api/users/register`
- **Fields**: First Name, Last Name, Email, Phone

### **2. Login Page** ? `/login`
- **Purpose**: Login to existing account
- **API**: `POST /api/auth/login`
- **Fields**: Phone Number only

---

## ?? **Page Navigation**

```
???????????????
?   /register ?  ? Create new account
???????????????
       ?
       ? "Already have account?"
       ?
???????????????
?    /login   ?  ? Login existing user
???????????????
       ?
       ? "Don't have account?"
       ?
       ????????????
```

---

## ?? **Registration Form**

```
/register
?????????????????????????????????
First Name:   [John_________]
Last Name:    [Doe__________]
Email:        [john@mail.com]
Phone Number: [9876543210___]

[ Create Account ]

Already have account? Login here
```

**API Call**:
```json
POST /api/users/register
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@mail.com",
  "phoneNumber": "9876543210"
}
```

---

## ?? **Login Form**

```
/login
?????????????????????????????????
Phone Number: [9876543210___]

[ Send OTP ]

Don't have account? Register here
```

**API Call**:
```json
POST /api/auth/login
{
  "phoneNumber": "9876543210"
}
```

---

## ?? **Complete Flow**

### **New User (Registration)**:
```
1. Visit /register
2. Fill in all 4 fields
3. Click "Create Account"
4. ? POST /api/users/register
5. ? Redirect to /verify-otp
6. Enter OTP from email
7. ? POST /api/auth/verify-otp
8. ? Login successful ? /dashboard
```

### **Existing User (Login)**:
```
1. Visit /login
2. Enter phone number
3. Click "Send OTP"
4. ? POST /api/auth/login
5. ? Redirect to /verify-otp
6. Enter OTP from email
7. ? POST /api/auth/verify-otp
8. ? Login successful ? /dashboard
```

---

## ?? **Quick Test**

### **Test Registration**:
```bash
# Navigate to
http://localhost:5001/register

# Fill form:
First Name: Test
Last Name: User
Email: test@example.com
Phone: 9876543210

# Click: Create Account
# Should redirect to OTP page
```

### **Test Login**:
```bash
# Navigate to
http://localhost:5001/login

# Fill form:
Phone: 9876543210

# Click: Send OTP
# Should redirect to OTP page
```

---

## ?? **Validation Rules**

| Field | Rule | Error Message |
|-------|------|---------------|
| First Name | Required | "First name is required" |
| Last Name | Required | "Last name is required" |
| Email | Required + Valid format | "Invalid email address" |
| Phone | Required + 10 digits | "Phone must be 10 digits" |

---

## ?? **Files Created/Modified**

```
? web/MedRemind.Web/Pages/Register.razor (NEW)
? web/MedRemind.Web/Pages/Login.razor (UPDATED)
? web/MedRemind.Web/Services/AuthService.cs (UPDATED)
? web/MedRemind.Web/Services/IAuthService.cs (UPDATED)
```

---

## ? **Status**

- ? Build: **PASSING**
- ? Registration API: **Connected**
- ? Login API: **Connected**
- ? Navigation: **Working**
- ? Validation: **Implemented**

---

**Run**: `cd web/MedRemind.Web && dotnet watch run`
**URLs**: 
- Register: http://localhost:5001/register
- Login: http://localhost:5001/login
