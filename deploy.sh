#!/bin/bash

echo "####### Build version: $BUILD_VERSION"
echo "####### Travis build version: $TRAVIS_BUILD_NUMBER"

if [ "$TRAVIS_PULL_REQUEST" != "false" ]; then
    echo "####### Not deploying on pull request"
    exit 0
fi

#deploy to DC0 on master branch
if [ "$TRAVIS_BRANCH" = "master" ]; then
    echo "####### Development Deployment Starting"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DEV && export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DEV
    serverless deploy --stage v1 --region ap-southeast-2 --data-center dc0
    exit 0
fi

if [ "$TRAVIS_BRANCH" = "prod" ]; then
    ## Create the keys for production
    echo "####### Production Deployment Starting"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DC2_5 && export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DC2_5
    serverless deploy --stage v1 --region us-east-1 --data-center dc4
    exit 0
fi




