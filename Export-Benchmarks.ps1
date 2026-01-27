param(
      [string]$InputPath = "BenchmarkDotNet.Artifacts/results/CsvReaderBenchmark-report.csv",
      [string]$OutputPath = "BenchmarkDotNet.Artifacts/results/CsvReaderBenchmark-report.xlsx",
      [int[]]$RowsFilter = @()
  )

  Import-Module ImportExcel -ErrorAction Stop

  function Convert-LocalizedNumber {
      param([string]$Text)

      if ([string]::IsNullOrWhiteSpace($Text)) { return $null }

      $styles = [Globalization.NumberStyles]::Float -bor [Globalization.NumberStyles]::AllowThousands
      $cultures = @(
          [Globalization.CultureInfo]::InvariantCulture,
          [Globalization.CultureInfo]::CurrentCulture,
          [Globalization.CultureInfo]::GetCultureInfo('en-US'),
          [Globalization.CultureInfo]::GetCultureInfo('fr-FR'),
          [Globalization.CultureInfo]::GetCultureInfo('de-DE')
      )

      foreach ($culture in $cultures) {
          $num = 0.0
          if ([double]::TryParse($Text, $styles, $culture, [ref]$num)) {
              return $num
          }
      }

      $work = $Text
      $dot = $work.LastIndexOf('.')
      $comma = $work.LastIndexOf(',')
      if ($dot -ge 0 -and $comma -ge 0) {
          if ($dot -gt $comma) {
              $work = $work -replace ',', ''
          } else {
              $work = $work -replace '\.', ''
              $work = $work -replace ',', '.'
          }
      } elseif ($comma -ge 0 -and $dot -lt 0) {
          $work = $work -replace ',', '.'
      }

      $num = 0.0
      if ([double]::TryParse($work, $styles, [Globalization.CultureInfo]::InvariantCulture, [ref]$num)) {
          return $num
      }

      return $null
  }

  function Convert-ToMicroseconds {
      param([object]$Value)

      if ($null -eq $Value) { return $null }

      $s = $Value.ToString().Trim()
      if ([string]::IsNullOrWhiteSpace($s)) { return $null }

      if ($s -match '^\s*([0-9\.,]+)\s*(ns|us|µs|μs|ms|s)?\s*$') {
          $num = Convert-LocalizedNumber $matches[1]
          if ($null -eq $num) { return $null }
          switch ($matches[2]) {
              'ns' { return $num / 1000.0 }
              'us' { return $num }
              'µs' { return $num }
              'μs' { return $num }
              'ms' { return $num * 1000.0 }
              's'  { return $num * 1000000.0 }
              default { return $num }
          }
      }

      return Convert-LocalizedNumber $s
  }

  $headerLine = Get-Content -Path $InputPath -TotalCount 1
  $delimiter = ','
  if (($headerLine -split ';').Count -gt ($headerLine -split ',').Count) {
      $delimiter = ';'
  }

  $data = Import-Csv -Path $InputPath -Delimiter $delimiter
  if ($RowsFilter.Count -gt 0) {
      $data = $data | Where-Object { $RowsFilter -contains [int]$_.Rows }
  }

  if (-not $data) {
      throw "No rows found in $InputPath"
  }

  $meanColumn = ($data[0].PSObject.Properties.Name | Where-Object { $_ -like 'Mean*' } | Select-Object -First 1)
  if (-not $meanColumn) {
      throw "No Mean column found in CSV."
  }

  $parsers = $data | Select-Object -ExpandProperty ParserKind -Unique
  $scenarios = $data | Sort-Object Method, Rows | Select-Object Method, Rows -Unique

  $lookup = @{}
  foreach ($row in $data) {
      $scenarioKey = "$($row.Method) ($($row.Rows))"
      if (-not $lookup.ContainsKey($scenarioKey)) { $lookup[$scenarioKey] = @{} }
      $lookup[$scenarioKey][$row.ParserKind] = Convert-ToMicroseconds $row.$meanColumn
  }

  $output = foreach ($scenario in $scenarios) {
      $key = "$($scenario.Method) ($($scenario.Rows))"
      $obj = [ordered]@{ Scenario = $key }
      foreach ($parser in $parsers) {
          $obj[$parser] = if ($lookup[$key].ContainsKey($parser)) { $lookup[$key][$parser] } else { $null }
      }
      [pscustomobject]$obj
  }

  $exportParams = @{
      Path = $OutputPath
      WorksheetName = "Mean_us"
      AutoSize = $true
      FreezeTopRow = $true
      TableName = "CsvBenchmarks"
      ChartType = "ColumnClustered"
  }

  $exportCmd = Get-Command Export-Excel
  if ($exportCmd.Parameters.ContainsKey('ChartTitle')) {
      $exportParams['ChartTitle'] = "CSV Reader Benchmarks (Mean, us)"
  }
  if ($exportCmd.Parameters.ContainsKey('ChartYTitle')) {
      $exportParams['ChartYTitle'] = "Mean (us)"
  }

  $output | Export-Excel @exportParams

  Write-Host "Wrote $OutputPath"
