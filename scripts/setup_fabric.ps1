
# scripts/setup-fabric.ps1
# PowerShell script to set up Microsoft Fabric resources

Write-Host "🔧 Setting up Microsoft Fabric workspace..." -ForegroundColor Cyan

# Variables
$WorkspaceName = "KenyaInventoryPredictorAI"
$EventhouseName = "inventory_realtime_db"

Write-Host "📊 Creating Fabric workspace: $WorkspaceName" -ForegroundColor Green

# Note: This is a template script. Actual Fabric setup is done through the portal
# as Fabric doesn't have full PowerShell/CLI support yet

Write-Host @"

Please follow these manual steps in the Microsoft Fabric portal:

1. Create Workspace
   - Go to https://app.fabric.microsoft.com
   - Click 'Workspaces' → 'New workspace'
   - Name: $WorkspaceName
   - Assign Fabric capacity

2. Create Eventhouse
   - In workspace, click 'New' → 'Eventhouse'
   - Name: $EventhouseName
   - Wait for provisioning

3. Create KQL Tables
   - Open Eventhouse
   - Click 'New KQL Queryset'
   - Run the table creation scripts from fabric/kql_queries/

4. Create Eventstream
   - In workspace, click 'New' → 'Eventstream'
   - Name: sales_transaction_stream
   - Configure source: Event Hub
   - Configure destination: Eventhouse

5. Create Lakehouse
   - In workspace, click 'New' → 'Lakehouse'
   - Name: inventory_data_lakehouse

6. Upload Notebooks
   - Upload files from fabric/notebooks/
   - Run in sequence to train ML models

"@ -ForegroundColor Yellow

Write-Host "`n✅ Please complete the manual steps above" -ForegroundColor Green
Write-Host "📚 Refer to docs/FABRIC_SETUP.md for detailed instructions" -ForegroundColor Cyan
