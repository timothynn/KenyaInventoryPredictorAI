
# scripts/deploy-api.sh
#!/bin/bash

# Deploy API to Azure App Service

echo "🚀 Deploying Kenya Inventory Predictor API to Azure..."

# Variables
RESOURCE_GROUP="InventoryPredictorRG"
LOCATION="eastus"
APP_SERVICE_PLAN="InventoryPredictorPlan"
APP_NAME="inventory-predictor-api"
RUNTIME="DOTNET|8.0"

# Login to Azure (if not already logged in)
echo "📝 Logging into Azure..."
az login

# Create resource group
echo "📦 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan
echo "⚙️ Creating App Service Plan..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
echo "🌐 Creating Web App..."
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $APP_NAME \
  --runtime $RUNTIME

# Configure app settings
echo "🔧 Configuring app settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    Fabric__EventhouseEndpoint="$FABRIC_EVENTHOUSE_ENDPOINT" \
    EventHub__ConnectionString="$EVENT_HUB_CONNECTION_STRING"

# Build and publish
echo "🏗️ Building application..."
cd src/InventoryPredictor.Api
dotnet publish -c Release -o ./publish

# Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../deployment.zip .
cd ..

# Deploy
echo "🚀 Deploying to Azure..."
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --src deployment.zip

echo "✅ Deployment complete!"
echo "🌐 Your API is available at: https://$APP_NAME.azurewebsites.net"
