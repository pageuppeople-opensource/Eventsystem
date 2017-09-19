#!/bin/bash

VERSION=$BUILD_VERSION.$TRAVIS_BUILD_NUMBER.$TRAVIS_BRANCH

echo "####### Build version: $BUILD_VERSION"
echo "####### Travis build version: $TRAVIS_BUILD_NUMBER"
export SLS_DEBUG=true

if [ "$TRAVIS_PULL_REQUEST" != "false" ]; then
    echo "####### Not deploying on pull request"
    exit 0
fi

#deploy to DC0 on master branch
if [ "$TRAVIS_BRANCH" = "master" ]; then
    echo "####### Development Deployment Starting"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DEV
    export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DEV
    serverless deploy --stage v1 --region ap-southeast-2 --data-center dc0 -v
fi

if [ "$TRAVIS_BRANCH" = "prod" ]; then
    ## Create the keys for production
    echo "####### Production Deployment Starting"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DC2_5
    export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DC2_5

    echo "####### Deploying DC2"
    serverless deploy --stage v1 --region ap-southeast-2 --data-center dc2 -v

    echo "####### Deploying DC3"
    serverless deploy --stage v1 --region eu-west-1 --data-center dc3 -v

    echo "####### Deploying DC4"
    serverless deploy --stage v1 --region us-east-1 --data-center dc4 -v

    echo "####### Deploying DC5"
    serverless deploy --stage v1 --region ap-southeast-1 --data-center dc5 -v

    echo "####### Deploying DC6"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DC6
    export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DC6
    serverless deploy --stage v1 --region ap-southeast-1 --data-center dc6 -v

    echo "####### Deploying DC7"
    export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DC7
    export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DC7
    serverless deploy --stage v1 --region eu-west-1 --data-center dc7 -v

    git tag $VERSION
    git push origin --tags
    exit 0
fi




