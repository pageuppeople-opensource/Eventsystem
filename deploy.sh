#!/bin/bash

export AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID_DEV && export AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY_DEV
serverless deploy --stage v1 --region ap-southeast-2

