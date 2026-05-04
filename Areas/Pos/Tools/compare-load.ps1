$files=@('F:\Source Code\DynamicErp\Areas\Pos\Tools\pos-load-baseline-before-indexes.csv','F:\Source Code\DynamicErp\Areas\Pos\Tools\pos-load-after-indexes.csv')
foreach($f in $files){
 Write-Host "`n==== $(Split-Path $f -Leaf) ===="
 $rows=Import-Csv $f
 $out=@()
 foreach($g in ($rows | Group-Object Kind)){
   $r=@($g.Group)
   $fail=@($r|Where-Object {$_.Success -ne 'True'})
   $lat=$r|Measure-Object ElapsedMs -Minimum -Maximum -Average
   $sorted=@($r|Sort-Object {[int]$_.ElapsedMs})
   $p95=$sorted[[Math]::Min($sorted.Count-1,[Math]::Floor($sorted.Count*0.95))].ElapsedMs
   $out += [pscustomobject]@{Kind=$g.Name;Count=$r.Count;Failures=$fail.Count;FailurePct=[math]::Round($fail.Count*100.0/[math]::Max(1,$r.Count),2);Min=$lat.Minimum;Avg=[math]::Round($lat.Average,2);Max=$lat.Maximum;P95=$p95}
 }
 $out | Format-Table -AutoSize
}
