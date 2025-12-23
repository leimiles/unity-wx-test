# Daily Code Review Workflow Setup

## Overview
This workflow automatically reviews your code every day at 2 AM UTC and sends an email report to `leimiles_18@hotmail.com`.

## Configuration Required

### 1. Set up GitHub Secrets
To send emails, you need to configure the following secrets in your GitHub repository:

1. Go to your repository on GitHub
2. Click on **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** and add:

   - **MAIL_USERNAME**: Your email address (e.g., your Gmail address)
   - **MAIL_PASSWORD**: Your email app password (see below for instructions)

### 2. Get Gmail App Password

If you're using Gmail to send emails:

1. Go to your Google Account settings: https://myaccount.google.com/
2. Navigate to **Security** → **2-Step Verification** (enable if not already enabled)
3. Scroll down to **App passwords** and click it
4. Generate a new app password for "Mail" and "Other (Custom name)"
5. Copy the 16-character password and use it as `MAIL_PASSWORD` secret

### 3. Alternative Email Providers

If you want to use a different email provider (not Gmail):

Edit the workflow file `.github/workflows/daily-code-review.yml` and update:
```yaml
server_address: smtp.your-provider.com  # e.g., smtp.office365.com for Outlook
server_port: 465  # or 587 for TLS
```

Common SMTP settings:
- **Gmail**: smtp.gmail.com:465 (SSL) or :587 (TLS)
- **Outlook/Hotmail**: smtp.office365.com:587 (TLS)
- **Yahoo**: smtp.mail.yahoo.com:465 (SSL) or :587 (TLS)

### 4. Adjust Timezone

The workflow currently runs at 2 AM UTC. To adjust for your timezone:

Edit the cron schedule in `.github/workflows/daily-code-review.yml`:
```yaml
schedule:
  - cron: '0 2 * * *'  # Change the hour (0-23)
```

**Timezone conversion examples:**
- 2 AM UTC = 10 AM CST (China Standard Time, UTC+8)
- For 2 AM Beijing time, use: `0 18 * * *` (6 PM UTC = 2 AM next day in UTC+8)

## Features

The workflow performs:

1. **C# Code Analysis**
   - Counts files and lines of code
   - Identifies large files (potential complexity issues)
   - Shows recent changes in last 24 hours

2. **JavaScript/Node.js Analysis**
   - Reviews Server directory code
   - Checks package dependencies
   - Runs ESLint if configured

3. **Security Check**
   - Scans for hardcoded credentials
   - Lists TODO/FIXME items

4. **Email Report**
   - Sends comprehensive report to configured email
   - Includes recommendations for improvement

## Manual Testing

You can manually trigger the workflow for testing:

1. Go to **Actions** tab in your GitHub repository
2. Click on **Daily Code Review** workflow
3. Click **Run workflow** button
4. Select branch and click **Run workflow**

## Troubleshooting

### Email not received?
- Check GitHub Actions logs for errors
- Verify secrets are correctly configured
- Check spam/junk folder
- Ensure email provider allows SMTP access
- For Gmail, ensure "Less secure app access" or App Password is used

### Workflow not running?
- GitHub Actions must be enabled for your repository
- Scheduled workflows may have a delay of up to 15 minutes
- Check Actions tab for any workflow failures

## Report Artifacts

Even if email fails, the review report is always saved as an artifact in the workflow run:
1. Go to **Actions** tab
2. Click on the workflow run
3. Download the **code-review-report** artifact

## Customization

You can customize the review by editing `.github/workflows/daily-code-review.yml`:
- Add more code analysis tools
- Change review criteria
- Modify report format
- Add additional notification channels (Slack, Discord, etc.)
