# Serverless.DotnetCore.Template
[![serverless](https://dl.dropboxusercontent.com/s/d6opqwym91k0roz/serverless_badge_v3.svg)](http://www.serverless.com) 

This is a serverless framework template project created for VS2017.

## Installation

Make sure you have the [Serverless Framework](http://www.serverless.com) installed.
```
npm install serverless -g
```

Install dotnetcore on your machine. If you encounter issues with VS2015, visit the following url to install the appropriate version of dotnetcore sdk.
https://github.com/aspnet/Tooling/blob/master/known-issues-vs2015.md


## Build

Run the following command in powershell to build the project.
```
build.ps1
```

Use the following command for bash.
```
./build.sh
```

## Test
To run unit tests
```
dotnet test .\src\BusinessEvents.SubscriptionEngine.Tests\BusinessEvents.SubscriptionEngine.Tests.csproj
```

## Deploy
```
serverless deploy
```

## Test data to pass to sns

```
{"Header":{"UserId":"userId","TransportTimeStamp":"2017-07-21T00:28:54.2282942Z","Metadata":{"metaheader1":"metaheadervalue1"},"Origin":"origin","InstanceId":"instanceId","CorrelationId":"d3e35fe4-ad84-46c0-b54f-3a6dc779630d"},"Messages":[{"Header":{"Metadata":null,"MessageType":"messagetype","MessageId":"a9757a12-a80e-4e32-9bab-e3d65a4b3a92"},"Body":{"contents":"bodycontents"}}]}
```

## Testing notifiers

### Slack 

Slack posts to the channel called [pageup/business-events](https://pageup.slack.com/messages/C6BMM1UNN)

### Default notifier - http notification

This posts to the [requestbin](https://requestb.in/1hb5s151?inspect)