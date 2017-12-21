# Eventsystem
[![serverless](https://dl.dropboxusercontent.com/s/d6opqwym91k0roz/serverless_badge_v3.svg)](http://www.serverless.com) [![Build Status](https://travis-ci.com/PageUpPeopleOrg/PageUp.EventSystem.svg?token=dukFECyWinXromxHYFWE)](https://travis-ci.com/PageUpPeopleOrg/PageUp.EventSystem)

Eventsystem - Enables applications and microservices to pass events between them reliably thereby enabling building decoupled domains and systems.

It provides support for two major consumer implementation types,

1. _Exactly once ordered processing_ for _worker role consumers_ 
2. _Atleast once delivery through Webhooks (built in capability)_ for _Web role consumers_

Enabling developers to build [Event driven systems (Event Notification, Event Carried State Transer, Event Sourced)](https://martinfowler.com/articles/201701-event-driven.html)

_This uses AWS tech, so requires to be deployed on AWS. However the consumers and producers are not limited to AWS platform._

*The project is under development. Some of the mentioned capabilities are not mature enough and may be a work in progress until we release a 1.0.0 version. It is worth starting the conversation through issues to register interest*

*PageUp does have few other alternate implementaitons. This repo does not mean, this is the primary Event bus in use within PageUp*

## Contents

* [Quick Start](#quick-start)
* [Configuring the Subscription Engine (Webhook capability)](#configure-subscription-engine)
* A Worker role sample
* A Web role sample
* Architecture
* [Enable encryption of Data at rest](#encrypt-data-at-rest)
* [How to encrypt sensitive deployment configurations](#encrypt-deployment-config)


## <a name="quick-start"></a>Quick start

Start by cloning this repo.

### Prerequisite
1. Install [Serverless Framework](http://www.serverless.com).
    ```
    npm install serverless -g
    ```
2. Install [dotnetcore](https://www.microsoft.com/net/download/)

### Deploy

1. Open `serverless.yml` and fill in the appropriate values under section `custom:`

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

2. Deploy 

    ```serverless deploy```

## <a name="encrypt-data-at-rest"></a>Enable encryption of data at rest

This system uses AWS Dynamo DB as the internal event store.
If you require the data stored in this table to be encrypted, ensure to set up 
`KMS_KEY_ARN` and `DATA_ENCRYPTION_KEY`.

`KMS_KEY_ARN` - refers to the AWS KMS key to be used for encrypting
`DATA_ENCRYPTION_KEY` - refers to the salt that goes along with encryption

It is recommended to encrypt this file if it included in the repository.

## <a name="configure-subscription-engine"></a>Configuring the Subscription Engine (Webhook capability)

This is needed only if you are planning to use the Subscription engine capability to write consumers that are Web roles.
To learn more about it, go to [web role sample](#web-role-sample)

Subscription system supports
1. Webhook consumers
2. Authenticated Webhook consumers (oAuth client credentials grant flow)
3. AWS Lambdas as consumers (uses ARN - requires IAM policies to invoke)


### Subsciptions configuration    
The configuration is webhooks are managed as a json file, the location of which is configured through ENV variable `S3_BUCKET_NAME`.
A sample configuration file is included at the root called `subscriptions.json`.

### oAuth configuration for subscriptions

Current implementation of the Subscription engine supports only one Auth server, the credentials representing which are configured in `serverless-environment-variables.yml`

## <a name="encrypt-deployment-config"></a>How to encrypt sensitive deployment configurations

In the current set up, there are two potential places where sensitive information may end up.

#### Serverless environment variables

Encrypt this file using the following command

```
openssl aes-256-cbc -e -in serverless-environment-variables.yml -out serverless-environment-variables.yml.enc -k {$ENCRYPTION_KEY}
```
Replace {$ENCRYPTION_KEY} with a strong password. 
Add the key value pair to the travis build environment variables.

#### Travis yaml

Ensure any deployment credentials are encrypted.

```
travis encrypt AWS_ACCESS_KEY_ID_STAGING="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_STAGING="secretvalue"

travis encrypt AWS_ACCESS_KEY_ID_PRODUCTION="secretvalue"
travis encrypt AWS_SECRET_ACCESS_KEY_PRODUCTION="secretvalue"

```

## Developers section

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
