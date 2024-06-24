# GitHub Webhook Listener and Branch Protection Enforcer
This solution is a .NET 8 Web API that listens for GitHub repository creation events via webhooks and automatically sets branch protection rules for the newly created repositories. Additionally, it creates an issue in the new repository, notifying the repository owner of the applied branch protection rules.

## Prerequisites
- .NET 8 SDK
- GitHub account and an organization
- Personal access token (PAT) classic with repository and organization admin scopes
- Secret for GitHub webhook verification

## Getting Started
1. Clone the Repository
```
bash
git clone https://github.com/your-repo/github-webhook-listener.git
cd github-webhook-listener
```
2. Configuration
appsettings.json
Create an appsettings.json file in the root of the project with the following content:
```
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "GitHubWebhookSecret": "your_github_webhook_secret",
    "GitHubToken": "your_personal_access_token",
    "DefaultBranch": "main"
}
```
Replace your_github_webhook_secret with your GitHub webhook secret and your_personal_access_token with your GitHub personal access token.

4. Build and Run the Application
```
bash
dotnet build
dotnet run
```

The application will start and listen for incoming webhook events on the configured port (default is 5000).

6. Set Up the GitHub Webhook
- Navigate to your GitHub organization settings.
- Go to "Webhooks" and click "Add webhook".
- Set the payload URL to your deployed Web API endpoint (e.g., https://yourdomain.com/api/webhook).
- Set the content type to application/json.
- Provide the secret you configured in appsettings.json.
- Select the event type "Repositories" and choose "Send me everything" or specifically select "Repository creation".
  
7. Testing with Smee
Smee is a webhook payload delivery service that allows you to test webhooks locally. To use Smee:

- Go to Smee.io and create a new channel.
- Copy the unique URL provided by Smee.
- Add a new webhook in your GitHub organization using the Smee URL as the payload URL.
- Run the following command to forward payloads from Smee to your local development server:
```
bash
npx smee-client --target http://localhost:5000/api/webhook --source https://smee.io/your-smee-channel
Replace https://smee.io/your-smee-channel with your Smee channel URL.
```

6. Monitor Logs
Logs are output to the console and include detailed information about incoming requests, signature verification, branch protection application, and issue creation. Check the console logs for any errors or information about the processing steps.

## Project Structure
Controllers/WebhookController.cs: Handles incoming webhook events and processes repository creation events.
Program.cs: Configures services, logging, and the HTTP request pipeline.

## Security Considerations
Ensure your GitHub personal access token has minimal necessary scopes to perform the required actions.
Store sensitive information such as secrets and tokens securely, e.g., using environment variables or secure vaults.
