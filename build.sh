#!/bin/bash

HANDLERS_DIR=src/BusinessEvents.SubscriptionEngine.Handlers
#build handlers
dotnet restore
dotnet publish -c release $HANDLERS_DIR

#create deployment package
pushd $HANDLERS_DIR/bin/release/netcoreapp1.0/publish
zip -r ./deploy-package.zip ./*
popd
