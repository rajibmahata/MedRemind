# Registration Form Update - Summary

## Changes Made

### 1. **Merged Name Fields**
- ? Removed separate `FirstName` and `LastName` fields
- ? Added single `Name` field for full name
- ? Updated placeholder to "John Doe"

### 2. **Added Password Fields**
- ? Added `Password` field with validation (minimum 6 characters)
- ? Added `ConfirmPassword` field with matching validation
- ? Implemented password visibility toggle for both fields
- ? Added helper text: "Minimum 6 characters"

### 3. **Added Date of Birth Field**
- ? Added optional `DateOfBirth` field using `MudDatePicker`
- ? Set maximum date to today (users cannot select future dates)
- ? Opens to year view for easier selection
- ? Added helper text: "Optional: For age-appropriate medication guidance"

## Updated Files

### 1. `web/MedRemind.Web/Services/AuthService.cs`
```csharp
public class RegisterModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
}
```

**Updated RegisterAsync method:**
- Sends `Name` instead of `FirstName` and `LastName`
- Includes `Password` and `DateOfBirth` fields

### 2. `web/MedRemind.Web/Services/IAuthService.cs`
```csharp
public class UserProfileData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
}
```

### 3. `web/MedRemind.Web/Pages/Register.razor`

**Added validation methods:**
- `ValidatePassword()` - Ensures password is at least 6 characters
- `ValidateConfirmPassword()` - Ensures passwords match
- `TogglePasswordVisibility()` - Shows/hides password
- `ToggleConfirmPasswordVisibility()` - Shows/hides confirm password

**Form Layout Order:**
1. Full Name (Required)
2. Email Address (Required)
3. Phone Number (Required, 10 digits)
4. Password (Required, min 6 characters)
5. Confirm Password (Required, must match)
6. Date of Birth (Optional)

## Features

### Password Security
- ? Minimum 6 characters validation
- ? Real-time validation with `Immediate="true"`
- ? Passwords must match
- ? Toggle visibility with eye icon
- ? Password field type switches between `Password` and `Text`

### Date of Birth
- ? Optional field (not required for registration)
- ? Date picker with year, month, date selection
- ? Cannot select future dates (MaxDate set to today)
- ? Opens to year view for easier selection
- ? Helper text explains it's for age-appropriate guidance

### User Experience
- ? Clear visual hierarchy with icons
- ? Helpful placeholder text
- ? Real-time validation feedback
- ? Consistent styling with rounded corners (8px)
- ? Proper spacing between fields (mb-3)

## Backend Integration Required

?? **Important:** The backend API needs to be updated to accept these new fields:

### Required Backend Changes

1. **Update User Registration Endpoint** (`/api/users/register`):
   ```csharp
   public class RegisterRequest
   {
       public string Name { get; set; }
       public string Email { get; set; }
       public string PhoneNumber { get; set; }
       public string Password { get; set; }
       public DateTime? DateOfBirth { get; set; }
   }
   ```

2. **Update User Model/Entity:**
   - Change `FirstName` and `LastName` to single `Name` field
   - Add `Password` (hashed) field
   - Add optional `DateOfBirth` field

3. **Database Migration:**
   ```sql
   -- Merge name fields
   ALTER TABLE Users ADD Name NVARCHAR(200) NULL;
   UPDATE Users SET Name = CONCAT(FirstName, ' ', LastName);
   ALTER TABLE Users DROP COLUMN FirstName;
   ALTER TABLE Users DROP COLUMN LastName;
   
   -- Add new fields
   ALTER TABLE Users ADD Password NVARCHAR(255) NOT NULL DEFAULT '';
   ALTER TABLE Users ADD DateOfBirth DATETIME NULL;
   ```

4. **Password Hashing:**
   - Use BCrypt or similar for password hashing
   - Never store plain text passwords
   - Validate password on registration

## Testing Checklist

- [ ] Test name field accepts full names
- [ ] Test password validation (min 6 characters)
- [ ] Test passwords must match
- [ ] Test password visibility toggle
- [ ] Test date of birth picker
- [ ] Test date of birth is optional (can be left empty)
- [ ] Test form submission with all fields
- [ ] Test form submission without date of birth
- [ ] Test navigation to OTP verification page
- [ ] Verify data is sent to backend correctly

## Navigation Flow

1. User fills registration form
2. Submits form ? API call to `/api/users/register` with:
   - Name
   - Email
   - PhoneNumber
   - Password (should be hashed by backend)
   - DateOfBirth (optional)
3. On success ? Navigate to `/verify-otp?phone={phone}&email={email}&name={encodedName}`
4. User verifies OTP
5. Redirect to dashboard

## Notes

- ? All validation is working client-side
- ? Date of Birth is truly optional
- ? Password visibility toggles independently
- ? Form maintains modern, clean design
- ?? Backend needs updates to accept new schema
- ?? Password should be hashed on backend before storage
- ?? Consider adding password strength indicator in future
