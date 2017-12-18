# PageUp.EventSystem
[![serverless](https://dl.dropboxusercontent.com/s/d6opqwym91k0roz/serverless_badge_v3.svg)](http://www.serverless.com)
[![Build Status](https://travis-ci.com/PageUpPeopleOrg/PageUp.EventSystem.svg?token=dukFECyWinXromxHYFWE)](https://travis-ci.com/PageUpPeopleOrg/PageUp.EventSystem)

This is a bootstrappable version of the Business Events System. It is build using serverless framework, dotnet core 1.0 and C# targetting AWS.

The project will create and deploy the following infrastructure:
* Kinesis Stream
* DynamoDB Table that stores all the events
* Lambda functions
* Api-Gateway

## To get started 

Make sure you have the [Serverless Framework](http://www.serverless.com) installed.
```
npm install serverless -g
```

Install dotnetcore on your machine. If you encounter issues with VS2015, visit the following url to install the appropriate version of dotnetcore sdk.
https://github.com/aspnet/Tooling/blob/master/known-issues-vs2015.md

### Typical once off set up

1. Fill in the appropriate values in the file for the respective data centres.
2. [Recommended] Encrypt the file using the following command

```
openssl aes-256-cbc -e -in serverless-environment-variables.yml -out serverless-environment-variables.yml.enc -k {$ENCRYPTION_KEY}
```

Replace {$ENCRYPTION_KEY} with a value that is hard to guess and store it in your team's password repository. Add the key value pair to the travis build environment variables.

4. Setup the following build variables by running the following encryption commands with the appropriate values:

```
travis encrypt AWS_ACCESS_KEY_ID_STAGING="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_STAGING="secretvalue"

travis encrypt AWS_ACCESS_KEY_ID_PRODUCTION="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_PRODUCTION="secretvalue"

```

5. Open `serverless.yml` and fill in the appropriate values for your team under section `custom:`

For example
```
custom:
  stream: Glofish
  s3BucketName: pageup-integration
  prefix: Integration
  vars: ${file(./serverless-environment-variables.yml)}
```

| Property      | Descriptionn  | 
| ------------- |:------------- |
| stream        | the stream name. this is used to tag the resources created by this serverless project |
| s3BucketName  | the S3 bucket where the subscriber file will be created and accessed from.            |
| prefix        | a unique identifier that is used to prefix the service name of serverless project     |
| vars          | the name of the environment variable file                                             |


## Developers - good to know

### Build shortcuts

Run the following command in powershell to build the project.
```
build.ps1
```

Use the following command for bash.
```
./build.sh
```

### Test shortcuts

To run unit tests

Bring the docker container up. This will bring up local authentication server
```
docker-compose build
docker-compose up -d
```
Now run test
```
dotnet test .\src\BusinessEvents.SubscriptionEngine.Tests\BusinessEvents.SubscriptionEngine.Tests.csproj
```

### Deployment from dev machine
```
serverless deploy --stage v1 --region ap-southeast-2 --data-center staging -v
```

#### Deployment Quirks

If you are deploying BusinessEvents-SubscriptionEngine for the first time, you need to comment out the following lambda
function definitions in `serverless.yml`.

* `process-dynamodb-stream:`
* `process-kinesis-stream:`

And you may have to run `serverless deploy ...` twice to successfully deploy the lambda functions.

The above functions require Kinesis and DynamoDB streams to be created first before it can attach itself to the streams.
Once the streams are created, you do not have to peform this step again.


### Internal reference 
Refer to [Business Events Management](https://pageuppeople.atlassian.net/wiki/spaces/DEV/pages/6816533/Business+Events+Management) document on Confluence for more information on the architecture design. 
