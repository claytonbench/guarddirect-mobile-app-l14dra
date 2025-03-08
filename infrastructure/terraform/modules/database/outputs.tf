output "sql_server_name" {
  description = "The name of the primary SQL Server"
  value       = azurerm_mssql_server.sql_server.name
}

output "sql_server_fqdn" {
  description = "The fully qualified domain name of the primary SQL Server"
  value       = azurerm_mssql_server.sql_server.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "The name of the SQL Database"
  value       = azurerm_mssql_database.sql_database.name
}

output "connection_string" {
  description = "The connection string for the SQL Database"
  value       = local.database_connection_string
  sensitive   = true
}

output "sql_server_id" {
  description = "The resource ID of the primary SQL Server"
  value       = azurerm_mssql_server.sql_server.id
}

output "sql_database_id" {
  description = "The resource ID of the SQL Database"
  value       = azurerm_mssql_database.sql_database.id
}

output "sql_server_secondary_name" {
  description = "The name of the secondary SQL Server for geo-replication"
  value       = var.environment == "prod" ? azurerm_mssql_server.sql_server_secondary[0].name : null
}

output "sql_server_secondary_fqdn" {
  description = "The fully qualified domain name of the secondary SQL Server"
  value       = var.environment == "prod" ? azurerm_mssql_server.sql_server_secondary[0].fully_qualified_domain_name : null
}