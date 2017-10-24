# BusinessEvents.SubscriptionEngine.Bootstrap
[![serverless](https://dl.dropboxusercontent.com/s/d6opqwym91k0roz/serverless_badge_v3.svg)](http://www.serverless.com)

This is a serverless framework template project created for VS2017.

## Installation

Make sure you have the [Serverless Framework](http://www.serverless.com) installed.
```
npm install serverless -g
```

Install dotnetcore on your machine. If you encounter issues with VS2015, visit the following url to install the appropriate version of dotnetcore sdk.
https://github.com/aspnet/Tooling/blob/master/known-issues-vs2015.md

## Once Off Setup

1. Rename `serverless-environment-variables-sample.yml` to `serverless-environment-variables.yml`
2. Fill in the appropriate values in the file for the respective data centres.
3. Encrypt the file using the following command

```
openssl aes-256-cbc -e -in serverless-environment-variables.yml -out serverless-environment-variables.yml.enc -k {$ENCRYPTION_KEY}
```

Replace {$ENCRYPTION_KEY} with a value that is hard to guess and store it in your team's password repository.

4. Setup the following build variables by running the following encryption commands with the appropriate values:

```
travis encrypt AWS_ACCESS_KEY_ID_DC0="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_DC0="secretvalue"

travis encrypt AWS_ACCESS_KEY_ID_DC2_5="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_DC2_5="secretvalue"

travis encrypt AWS_ACCESS_KEY_ID_DC6="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_DC6="secretvalue"

travis encrypt AWS_ACCESS_KEY_ID_DC7="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_DC7="secretvalue"
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

Bring the docker container up. This will bring up local authentication server
```
docker-compose build
docker-compose up -d
```
Now run test
```
dotnet test .\src\BusinessEvents.SubscriptionEngine.Tests\BusinessEvents.SubscriptionEngine.Tests.csproj
```

## Deploy
```
serverless deploy --stage v1 --region ap-southeast-2 --data-center dc0 -v
```

#### Deployment Quirks

If you are deploying BusinessEvents-SubscriptionEngine for the first time, you need to comment out the following lambda
function definitions in `serverless.yml`.

* `process-dynamodb-stream:`
* `process-kinesis-stream:`

And you may have to run `serverless deploy ...` twice to successfully deploy the lambda functions.

The above functions require Kinesis and DynamoDB streams to be created first before it can attach itself to the streams.
Once the streams are created, you do not have to peform this step again.




