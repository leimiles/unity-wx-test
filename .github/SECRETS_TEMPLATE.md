# GitHub Secrets Configuration Template
# 
# To enable the automated code review email notifications, you need to configure
# the following secrets in your GitHub repository.
#
# Location: Settings → Secrets and variables → Actions → New repository secret
#
# ============================================================================
# Required Secrets
# ============================================================================

# MAIL_USERNAME
# Description: The email address that will send the code review reports
# Example value: your-email@gmail.com
# Notes:
#   - This should be a valid email address you control
#   - Gmail, Outlook, QQ Mail, 163 Mail are all supported
#   - Make sure SMTP access is enabled for this account

# MAIL_PASSWORD
# Description: The password or app-specific password for the email account
# Example value: abcd efgh ijkl mnop (16 characters for Gmail app password)
# Notes:
#   - For Gmail: Use an App Password (not your regular password)
#     1. Enable 2-Step Verification on your Google Account
#     2. Generate an App Password at https://myaccount.google.com/apppasswords
#     3. Use the 16-character password generated
#   - For QQ Mail: Use the authorization code (not your QQ password)
#     1. Log in to QQ Mail
#     2. Settings → Account → Enable SMTP service
#     3. Generate and use the authorization code
#   - For other providers: Check their documentation for SMTP authentication

# ============================================================================
# Configuration Steps
# ============================================================================
#
# 1. Go to your GitHub repository
# 2. Click "Settings" at the top
# 3. In the left sidebar, click "Secrets and variables" → "Actions"
# 4. Click "New repository secret"
# 5. Enter secret name (e.g., MAIL_USERNAME)
# 6. Enter secret value
# 7. Click "Add secret"
# 8. Repeat steps 4-7 for MAIL_PASSWORD
#
# ============================================================================
# Testing Your Configuration
# ============================================================================
#
# After configuring the secrets:
# 1. Go to the "Actions" tab in your repository
# 2. Click on "Daily Code Review" workflow
# 3. Click "Run workflow" button (top right)
# 4. Select the branch and click "Run workflow"
# 5. Wait for the workflow to complete
# 6. Check your email (including spam folder) for the review report
# 7. If email fails, check the workflow logs for error messages
#
# ============================================================================
# Security Notes
# ============================================================================
#
# - Never commit actual passwords or app passwords to your repository
# - Secrets are encrypted and only accessible to GitHub Actions
# - You cannot view secret values after they're created (but you can update them)
# - Use app-specific passwords when available (more secure than main passwords)
# - Regularly rotate your credentials for better security
