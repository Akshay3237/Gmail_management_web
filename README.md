# Gmail AI Handler

The **Gmail AI Handler** is a .NET Core MVC web application that integrates with the Gmail API and Gemini AI to help users efficiently manage their Gmail inbox, delete messages in bulk, and generate AI-powered summaries of email content.

## Key Features
- OAuth2 Gmail Authentication
- Inbox Viewer with Pagination
- Batch Email Deletion
- AI-Powered Email Summarization with Gemini

## Tech Stack
- ASP.NET Core MVC
- Gmail API (via Google.Apis.Gmail.v1)
- Gemini AI REST API
- Razor Views
- C#

## Getting Started

Follow these steps to get the project running on your machine:

1. **Clone the Repository**  
   Open your terminal and run:
   ```bash
   git clone https://github.com/Akshay3237/Gmail_management_web.git
   cd Gmail_management_web
   ```

2. **Install Required NuGet Packages**  
   Run the following commands to install necessary dependencies:
   ```bash
   dotnet add package Google.Apis.Gmail.v1 --version 1.56.0
   dotnet add package Newtonsoft.Json
   ```

3. **Configure Gmail API Credentials**  
   - Go to [Google Cloud Console](https://console.cloud.google.com/).
   - Create a new project or select an existing one.
   - Enable the **Gmail API**.
   - Navigate to **APIs & Services > Credentials** and create **OAuth 2.0 Client ID** credentials.
   - Download the credentials `.json` file and place it in `wwwroot/secrets/credential.json`.

4. **Add Configuration to appsettings.json**  
   Open `appsettings.json` and configure the following settings:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*",
     "CredentialPath": "wwwroot/secrets/credential.json",
     "geminiapi": "YOUR_GEMINI_API_KEY"
   }
   ```

5. **Get Gemini AI API Key**  
   - Sign up or log in to [Gemini](https://ai.google.dev).
   - Copy your API key and paste it in the `geminiapi` field of `appsettings.json`.

6. **Build and Run the Application**  
   Use the following commands to start the project:
   ```bash
   dotnet build
   dotnet run
   ```

7. **Authorize Gmail Access**  
   On first launch, the app will redirect you to authorize access to your Gmail account. Complete the OAuth2 flow to allow access.

8. **Explore Features**  
   - View your inbox with pagination.
   - Select and delete emails in batch.
   - Click "Summarize" to generate an AI-based summary of the email content using Gemini.

---

> ✅ This app is built to showcase how AI can assist with email triage and productivity. It’s ideal for demos, prototypes, or personal productivity tooling.
