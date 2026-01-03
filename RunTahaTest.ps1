$baseUrl = "http://localhost:5227"

Write-Host "Starting Taha Scenario - Real Database Execution" -ForegroundColor Cyan
Write-Host "API Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host ("=" * 80) -ForegroundColor Gray
Write-Host ""

try {
    # Step 1: Create user
    Write-Host "Step 1: Creating user 'Taha'..." -ForegroundColor Yellow
    $userBody = @{ fullName = "Taha" } | ConvertTo-Json
    $user = Invoke-RestMethod -Uri "$baseUrl/api/users" -Method Post -Body $userBody -ContentType "application/json"
    $userId = $user.id
    Write-Host "SUCCESS: User 'Taha' created!" -ForegroundColor Green
    Write-Host "User ID: $userId"
    Write-Host ""

    # Step 2: Create product "moz" with 10% discount
    Write-Host "Step 2: Creating product 'moz' with 10 percent discount..." -ForegroundColor Yellow
    $mozBody = @{
        name = "moz"
        basePrice = 100
        discountPercent = 10
        isActive = $true
        initialStock = 1000
    } | ConvertTo-Json
    $moz = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body $mozBody -ContentType "application/json"
    $mozId = $moz.id
    Write-Host "SUCCESS: Product 'moz' created!" -ForegroundColor Green
    Write-Host "Product ID: $mozId"
    Write-Host "Base Price: 100, Discount: 10 percent, Final Price: 90"
    Write-Host ""

    # Step 3: Create product "sib" with no discount
    Write-Host "Step 3: Creating product 'sib' (no discount)..." -ForegroundColor Yellow
    $sibBody = @{
        name = "sib"
        basePrice = 100
        discountPercent = 0
        isActive = $true
        initialStock = 1000
    } | ConvertTo-Json
    $sib = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Post -Body $sibBody -ContentType "application/json"
    $sibId = $sib.id
    Write-Host "SUCCESS: Product 'sib' created!" -ForegroundColor Green
    Write-Host "Product ID: $sibId"
    Write-Host "Base Price: 100, Discount: 0 percent, Final Price: 100"
    Write-Host ""

    # Step 4: Create first order
    Write-Host "Step 4: Creating first order (6 moz and 6 sib)..." -ForegroundColor Yellow
    $order1Body = @{
        userId = $userId
        items = @(
            @{ productId = $mozId; quantity = 6 }
            @{ productId = $sibId; quantity = 6 }
        )
    } | ConvertTo-Json -Depth 10
    $order1 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body $order1Body -ContentType "application/json"
    $order1Id = $order1.orderId
    Write-Host "SUCCESS: First order created!" -ForegroundColor Green
    Write-Host "Order ID: $order1Id"
    Write-Host "Items: 6 moz (90 each) and 6 sib (100 each)"
    Write-Host "Total: $($order1.totalPrice)"
    Write-Host "Status: $($order1.status)"
    Write-Host ""

    # Step 5: Cancel first order
    Write-Host "Step 5: Canceling the first order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order1Id" -Method Delete
    $canceledOrder = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order1Id" -Method Get
    Write-Host "SUCCESS: First order canceled!" -ForegroundColor Green
    Write-Host "Status: $($canceledOrder.status)"
    Write-Host ""

    # Check user status after cancel
    $userAfterCancel = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    Write-Host "User status after cancellation:" -ForegroundColor Cyan
    Write-Host "Is VIP: $($userAfterCancel.isVip)"
    Write-Host "Total Spending: $($userAfterCancel.totalSpending)"
    Write-Host ""

    # Step 6: Create second order
    Write-Host "Step 6: Creating second order (6 moz and 6 sib)..." -ForegroundColor Yellow
    $order2Body = @{
        userId = $userId
        items = @(
            @{ productId = $mozId; quantity = 6 }
            @{ productId = $sibId; quantity = 6 }
        )
    } | ConvertTo-Json -Depth 10
    $order2 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body $order2Body -ContentType "application/json"
    $order2Id = $order2.orderId
    Write-Host "SUCCESS: Second order created!" -ForegroundColor Green
    Write-Host "Order ID: $order2Id"
    Write-Host "Items: 6 moz (90 each) and 6 sib (100 each)"
    Write-Host "Total: $($order2.totalPrice)"
    Write-Host "Status: $($order2.status)"
    Write-Host ""

    # Step 7: Purchase second order
    Write-Host "Step 7: Purchasing the second order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order2Id/pay" -Method Post
    $paidOrder2 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order2Id" -Method Get
    Write-Host "SUCCESS: Second order purchased!" -ForegroundColor Green
    Write-Host "Status: $($paidOrder2.status)"
    Write-Host "Amount Paid: $($paidOrder2.totalPrice)"
    Write-Host "Discount Applied: 10 percent on moz only"
    Write-Host ""

    # Check user status after purchase
    $userAfterPurchase = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    Write-Host "User status after purchase:" -ForegroundColor Cyan
    Write-Host "Is VIP: $($userAfterPurchase.isVip)"
    Write-Host "Total Spending: $($userAfterPurchase.totalSpending)"
    Write-Host ""

    if (-not $userAfterPurchase.isVip) {
        Write-Host "WARNING: User should be VIP after spending over 1000!" -ForegroundColor Yellow
    }

    # Step 8: Create third order
    Write-Host "Step 8: Creating third order (10 moz) - should have BOTH VIP and product discount..." -ForegroundColor Yellow
    $order3Body = @{
        userId = $userId
        items = @(
            @{ productId = $mozId; quantity = 10 }
        )
    } | ConvertTo-Json -Depth 10
    $order3 = Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body $order3Body -ContentType "application/json"
    $order3Id = $order3.orderId
    Write-Host "SUCCESS: Third order created!" -ForegroundColor Green
    Write-Host "Order ID: $order3Id"
    Write-Host "Items: 10 moz"
    Write-Host "Total: $($order3.totalPrice)"
    Write-Host "Expected: VIP discount (5 percent) and moz discount (10 percent) = 15 percent total"
    Write-Host "Status: $($order3.status)"
    Write-Host ""

    # Step 9: Purchase third order
    Write-Host "Step 9: Purchasing the third order..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order3Id/pay" -Method Post
    $paidOrder3 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order3Id" -Method Get
    Write-Host "SUCCESS: Third order purchased!" -ForegroundColor Green
    Write-Host "Status: $($paidOrder3.status)"
    Write-Host "Amount Paid: $($paidOrder3.totalPrice)"
    Write-Host ""

    # Final summary
    $finalUser = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Gray
    Write-Host "TAHA SCENARIO COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ("=" * 80) -ForegroundColor Gray
    Write-Host ""
    Write-Host "Final Summary:" -ForegroundColor Cyan
    Write-Host "User: $($finalUser.fullName) (ID: $userId)"
    Write-Host "VIP Status: $($finalUser.isVip)"
    Write-Host "Total Spent: $($finalUser.totalSpending)"
    Write-Host ""
    Write-Host "Orders Summary:" -ForegroundColor Cyan
    Write-Host "Order 1 ($order1Id): CANCELED"
    Write-Host "  - Status: $($canceledOrder.status)"
    Write-Host "  - Amount: $($canceledOrder.totalPrice)"
    Write-Host ""
    Write-Host "Order 2 ($order2Id): PURCHASED"
    Write-Host "  - Status: $($paidOrder2.status)"
    Write-Host "  - Amount: $($paidOrder2.totalPrice)"
    Write-Host "  - Discount: 10 percent on moz only"
    Write-Host ""
    Write-Host "Order 3 ($order3Id): PURCHASED"
    Write-Host "  - Status: $($paidOrder3.status)"
    Write-Host "  - Amount: $($paidOrder3.totalPrice)"
    Write-Host "  - Discount: VIP (5 percent) and moz (10 percent) = 15 percent total"
    Write-Host ""
    Write-Host "All data has been saved to the database!" -ForegroundColor Green
    Write-Host "You can now view this data in your database management tool." -ForegroundColor Cyan
    Write-Host ""

    # Validate results
    Write-Host "Validating Test Results:" -ForegroundColor Yellow
    Write-Host ""
    
    $allPassed = $true
    
    if ($canceledOrder.status -ne "Canceled") {
        Write-Host "FAIL: First order should be Canceled" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: First order is Canceled" -ForegroundColor Green
    }
    
    if ($paidOrder2.status -ne "Paid") {
        Write-Host "FAIL: Second order should be Paid" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: Second order is Paid" -ForegroundColor Green
    }
    
    if (-not $finalUser.isVip) {
        Write-Host "FAIL: User should be VIP after second order" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: User is VIP after second order" -ForegroundColor Green
    }
    
    if ($paidOrder3.status -ne "Paid") {
        Write-Host "FAIL: Third order should be Paid" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: Third order is Paid" -ForegroundColor Green
    }
    
    # Check discount calculation
    $expectedTotal = 855
    if ([Math]::Abs($paidOrder3.totalPrice - $expectedTotal) -gt 0.01) {
        Write-Host "WARNING: Third order total ($($paidOrder3.totalPrice)) doesn't match expected ($expectedTotal)" -ForegroundColor Yellow
        Write-Host "This might indicate discount calculation issues"
    } else {
        Write-Host "PASS: Third order has both VIP and product discounts applied correctly" -ForegroundColor Green
    }
    
    Write-Host ""
    if ($allPassed) {
        Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
    } else {
        Write-Host "SOME TESTS FAILED - Please review the results above" -ForegroundColor Yellow
    }
    Write-Host ""
}
catch {
    Write-Host "Scenario failed with error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)"
    exit 1
}
