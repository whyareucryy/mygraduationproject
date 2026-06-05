$ErrorActionPreference = "Stop"
$outFile = Join-Path $PSScriptRoot "mydbcode_data_export.sql"
$connStr = "Server=MAY;Database=ComputerRepairService;Trusted_Connection=True;TrustServerCertificate=True"

function Escape-SqlString([string]$s) {
    if ($null -eq $s) { return "NULL" }
    return "N'" + ($s.Replace("'", "''")) + "'"
}

function Format-Value($val, $colName) {
    if ($null -eq $val -or [DBNull]::Value.Equals($val)) { return "NULL" }
    $type = $val.GetType().Name
    switch ($type) {
        "String" { return Escape-SqlString $val }
        "Boolean" {
            if ($val) { return "1" } else { return "0" }
        }
        "Int32" { return $val.ToString() }
        "Int64" { return $val.ToString() }
        "Decimal" { return $val.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
        "Double" { return $val.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
        "DateTime" { return "'" + $val.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'" }
        "DateTimeOffset" { return "'" + $val.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz") + "'" }
        "Byte" { return $val.ToString() }
        "Guid" { return Escape-SqlString $val.ToString() }
        default { return Escape-SqlString ($val.ToString()) }
    }
}

$identityTables = @{
    "OrderStatuses" = "StatusId"
    "DeviceTypes" = "DeviceTypeId"
    "PartCategories" = "CategoryId"
    "Services" = "ServiceId"
    "Customers" = "CustomerId"
    "Technicians" = "TechnicianId"
    "Inventory" = "PartId"
    "ServiceOrders" = "OrderId"
    "OrderTechnicians" = "OrderTechnicianId"
    "OrderServices" = "OrderServiceId"
    "OrderParts" = "OrderPartId"
    "OrderStatusHistory" = "HistoryId"
    "Payments" = "PaymentId"
}

$skipColumns = @{
    "OrderServices" = @("TotalPrice")
    "OrderParts" = @("TotalPrice")
}

$tables = @(
    "OrderStatuses", "DeviceTypes", "PartCategories", "Services",
    "AspNetRoles", "AspNetUsers", "Customers", "Technicians", "AspNetUserRoles",
    "Inventory", "ServiceOrders", "OrderTechnicians", "OrderServices", "OrderParts",
    "OrderStatusHistory", "Payments"
)

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("-- AUTO-GENERATED DATA EXPORT")
[void]$sb.AppendLine("USE ComputerRepairService;")
[void]$sb.AppendLine("GO")
[void]$sb.AppendLine("")

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

foreach ($table in $tables) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM [$table]"
    $reader = $cmd.ExecuteReader()
    $schema = $reader.GetSchemaTable()
    $allCols = @()
    foreach ($row in $schema.Rows) { $allCols += $row["ColumnName"].ToString() }
    $cols = if ($skipColumns.ContainsKey($table)) { $allCols | Where-Object { $_ -notin $skipColumns[$table] } } else { $allCols }
    $rows = New-Object System.Collections.Generic.List[string]
    while ($reader.Read()) {
        $vals = @()
        foreach ($col in $cols) {
            $i = $reader.GetOrdinal($col)
            $vals += (Format-Value $reader.GetValue($i) $col)
        }
        $rows.Add("(" + ($vals -join ", ") + ")")
    }
    $reader.Close()
    if ($rows.Count -eq 0) { continue }

    [void]$sb.AppendLine("-- $table ($($rows.Count) rows)")
    if ($identityTables.ContainsKey($table)) {
        [void]$sb.AppendLine("SET IDENTITY_INSERT [$table] ON;")
    }
    $colList = ($cols | ForEach-Object { "[$_]" }) -join ", "
    $batchSize = 40
    for ($i = 0; $i -lt $rows.Count; $i += $batchSize) {
        $end = [Math]::Min($i + $batchSize - 1, $rows.Count - 1)
        $batch = $rows.GetRange($i, $end - $i + 1)
        [void]$sb.AppendLine("INSERT INTO [$table] ($colList) VALUES")
        [void]$sb.AppendLine(($batch -join ("," + [Environment]::NewLine)))
        [void]$sb.AppendLine(";")
    }
    if ($identityTables.ContainsKey($table)) {
        [void]$sb.AppendLine("SET IDENTITY_INSERT [$table] OFF;")
    }
    [void]$sb.AppendLine("GO")
    [void]$sb.AppendLine("")
}

$conn.Close()
[System.IO.File]::WriteAllText($outFile, $sb.ToString(), [System.Text.UTF8Encoding]::new($true))
Write-Output "Exported $($tables.Count) tables to $outFile"
