# Data Agent Setup (Microsoft Fabric)

This guide configures the conversational AI Data Agent used by Kenya Inventory Predictor AI.
It enables natural-language questions and guided analytics over your Fabric Eventhouse data.

## Prerequisites
- A Fabric workspace with capacity assigned
- Eventhouse database: `inventory_realtime_db`
- The KQL tables created as per `fabric/kql_queries/create_tables.kql`

## Create the Data Agent
1. Open Microsoft Fabric → your workspace
2. Go to AI experiences → Data Activator / Data Agent (Copilot)
3. Create new Agent: `inventory_copilot_agent`
4. Data source:
   - Type: Eventhouse (KQL DB)
   - Database: `inventory_realtime_db`
5. Permissions: ensure the agent has read access to the KQL database and tables

## Agent Instructions (Prompt)
Paste the following as system instructions and adapt to your vocabulary:

```
You are a helpful inventory analyst for a Kenyan retail business. Your job is to answer
questions and recommend actions using Eventhouse KQL data. Prefer concise answers with
clear next steps. When needed, produce KQL queries and explain key filters.

Primary datasets:
- sales_transactions: transactional sales with product_id, quantity, amount, channel, location, transaction_date
- inventory_levels: current and threshold stock per product per location
- stock_alerts: generated alerts and their resolution status

Common tasks:
- Identify products at risk of stock-out within the next 7/14/30 days
- Recommend reorder quantities and timing
- Detect anomalies in sales volume
- Summarize sales trends by location/category/channel
```

## Enable KQL Function Calls
- Allow the agent to execute KQL against `inventory_realtime_db`
- Whitelist relevant tables and the function `UpdateInventoryFromSales`

## Example Queries to Try
- "Which products need reordering in Nairobi this week?"
- "Show top 10 fastest moving items in the last 30 days"
- "Are there any anomalies in beverages sales today?"
- "When will sugar stock run out in Mombasa?"

## Troubleshooting
- If no results, confirm tables exist and have data
- Verify the Lakehouse/Eventstream pipelines are flowing
- Check the agent’s permissions on the KQL DB
