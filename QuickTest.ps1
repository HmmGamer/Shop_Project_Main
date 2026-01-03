$baseUrl = "http://localhost:5227"
Write-Host "Quick Test - T2 Scenario" -ForegroundColor Cyan

try {
    # Step 1: Create user
    Write-Host "1. Creating user TahaT2..." -ForegroundColor Yellow
    $user = Invoke-RestMethod -Uri "$baseUrl/api/users" -Method Post -Body (@{ fullName = "TahaT2" } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "   User created: $($user.id)" -ForegroundColor Green

    # Step 2: Create moz product
    Write-Host "2. Creating product mozT2 (10% discount)..." -ForegroundColor Yellow
    $moz = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body (@{ name = "mozT2"; basePrice = 100; discountPercent = 10; isActive = $true; initialStock = 1000 } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "   Product created: $($moz.id)" -ForegroundColor Green

    # Step 3: Create sib product
    Write-Host "3. Creating product sibT2 (no discount)..." -ForegroundColor Yellow
    $sib = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body (@{ name = "sibT2"; basePrice = 100; discountPercent = 0; isActive = $true; initialStock = 1000 } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "   Product created: $($sib.id)" -ForegroundColor Green

    # Step 4: Create first order
    Write-Host "4. Creating first order (6 moz + 6 sib)..." -ForegroundColor Yellow
    $order1 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body (@{ userId = $user.id; items = @(@{ productId = $moz.id; quantity = 6 }, @{ productId = $sib.id; quantity = 6 }) } | ConvertTo-Json -Depth 10) -ContentType "application/json"
    Write-Host "   Order created: $($order1.orderId), Total: $($order1.totalPrice)" -ForegroundColor Green

    # Step 5: Cancel first order
    Write-Host "5. Canceling first order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order1.orderId)" -Method Delete | Out-Null
    $canceled = Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order1.orderId)" -Method Get
    Write-Host "   Order canceled, Status: $($canceled.status)" -ForegroundColor Green

    # Step 6: Create second order
    Write-Host "6. Creating second order (6 moz + 6 sib)..." -ForegroundColor Yellow
    $order2 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body (@{ userId = $user.id; items = @(@{ productId = $moz.id; quantity = 6 }, @{ productId = $sib.id; quantity = 6 }) } | ConvertTo-Json -Depth 10) -ContentType "application/json"
    Write-Host "   Order created: $($order2.orderId), Total: $($order2.totalPrice)" -ForegroundColor Green

    # Step 7: Purchase second order
    Write-Host "7. Purchasing second order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order2.orderId)/pay" -Method Post | Out-Null
    $paid2 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order2.orderId)" -Method Get
    Write-Host "   Order paid, Status: $($paid2.status), Total: $($paid2.totalPrice)" -ForegroundColor Green

    # Check VIP status
    $userCheck = Invoke-RestMethod -Uri "$baseUrl/api/users/$($user.id)" -Method Get
    Write-Host "   User VIP: $($userCheck.isVip), Spending: $($userCheck.totalSpending)" -ForegroundColor Cyan

    # Step 8: Create third order
    Write-Host "8. Creating third order (10 moz)..." -ForegroundColor Yellow
    $order3 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body (@{ userId = $user.id; items = @(@{ productId = $moz.id; quantity = 10 }) } | ConvertTo-Json -Depth 10) -ContentType "application/json"
    Write-Host "   Order created: $($order3.orderId), Total: $($order3.totalPrice)" -ForegroundColor Green

    # Step 9: Purchase third order
    Write-Host "9. Purchasing third order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order3.orderId)/pay" -Method Post | Out-Null
    $paid3 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$($order3.orderId)" -Method Get
    Write-Host "   Order paid, Status: $($paid3.status), Total: $($paid3.totalPrice)" -ForegroundColor Green

    # Final summary
    Write-Host "`n=== RESULTS ===" -ForegroundColor Cyan
    Write-Host "User: TahaT2, VIP: $($userCheck.isVip)" -ForegroundColor White
    Write-Host "Order 1: Canceled (Status: $($canceled.status))" -ForegroundColor White
    Write-Host "Order 2: Paid (Status: $($paid2.status), Amount: $($paid2.totalPrice))" -ForegroundColor White
    Write-Host "Order 3: Paid (Status: $($paid3.status), Amount: $($paid3.totalPrice))" -ForegroundColor White
    Write-Host "`nExpected Order 3 total: 855 (10*100*0.9*0.95)" -ForegroundColor Yellow
    Write-Host "Actual Order 3 total: $($paid3.totalPrice)" -ForegroundColor Yellow
    
    if ($userCheck.isVip -and $canceled.status -eq 5 -and $paid2.status -eq 2 -and $paid3.status -eq 2) {
        Write-Host "`nTEST PASSED!" -ForegroundColor Green
    } else {
        Write-Host "`nTEST FAILED - Check results above" -ForegroundColor Red
    }
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
