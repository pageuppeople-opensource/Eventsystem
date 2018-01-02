#!/bin/bash

VERSION=${BUILD_VERSION}.${TRAVIS_BUILD_NUMBER}.${TRAVIS_BRANCH}

echo "####### Build version: $BUILD_VERSION"
echo "####### Travis build version: $TRAVIS_BUILD_NUMBER"
export SLS_DEBUG=true

if [ "$TRAVIS_PULL_REQUEST" != "false" ]; then
    echo "####### Not deploying on pull request"
    exit 0
fi

#deploy to DC0 on master branch
if [ "$TRAVIS_BRANCH" = "master" ]; then
    echo "####### Staging Deployment Starting"
    export AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID_STAGING}
    export AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY_STAGING}
    serverless deploy --stage v1 --region ap-southeast-2 --data-center staging -v
fi

if [ "$TRAVIS_BRANCH" = "prod" ]; then
    ## Create the keys for production
    echo "####### Production Deployment Starting"
    export AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID_PRODUCTION}
    export AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY_PRODUCTION}
    serverless deploy --stage v1 --region ap-southeast-2 --data-center production -v

    ## You can add more environments different different accounts or region like below
    ## echo "####### Deploying US DC"
    ## export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_US_DC
    ## export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_US_DC
    ## serverless deploy --stage v1 --region eu-west-1 --data-center us_dc -v

    git tag $VERSION
    git push origin --tags
    exit 0
fi
