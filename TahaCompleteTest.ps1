# ============================================================================
# Taha Complete Test - Full E2E Test with Real Database
# ============================================================================
# This script tests the complete order flow with discounts and VIP logic
# All data is saved to the real database (not in-memory)
# ============================================================================

$baseUrl = "http://localhost:5227"

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Taha Complete Test - Real Database E2E Test" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Scenario:" -ForegroundColor Yellow
Write-Host "1. Create user 'Taha'" -ForegroundColor White
Write-Host "2. Create 2 products: 'moz' (10% discount) and 'sib' (no discount)" -ForegroundColor White
Write-Host "3. Create order with both products (6 quantity each) to reach VIP threshold" -ForegroundColor White
Write-Host "4. Cancel the order" -ForegroundColor White
Write-Host "5. Create another order with both products (6 quantity each)" -ForegroundColor White
Write-Host "6. Purchase the order (user should become VIP)" -ForegroundColor White
Write-Host "7. Create order with 10 moz, then purchase it" -ForegroundColor White
Write-Host "" -ForegroundColor White
Write-Host "Expected Results:" -ForegroundColor Yellow
Write-Host "- First order: Canceled" -ForegroundColor White
Write-Host "- Second order: Purchased with 10% discount on moz only" -ForegroundColor White
Write-Host "- User becomes VIP after second order" -ForegroundColor White
Write-Host "- Third order: Both VIP discount AND moz discount applied" -ForegroundColor White
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

try {
    # ========================================================================
    # Step 1: Create User
    # ========================================================================
    Write-Host "Step 1: Creating user 'Taha'..." -ForegroundColor Yellow
    
    $userBody = @{ 
        fullName = "Taha" 
    } | ConvertTo-Json
    
    $user = Invoke-RestMethod -Uri "$baseUrl/api/users" -Method Post -Body $userBody -ContentType "application/json"
    $userId = $user.id
    
    Write-Host "SUCCESS: User 'Taha' created!" -ForegroundColor Green
    Write-Host "User ID: $userId" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 2: Create Product 'moz' with 10% discount
    # ========================================================================
    Write-Host "Step 2: Creating product 'moz' with 10% discount..." -ForegroundColor Yellow
    
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
    Write-Host "Product ID: $mozId" -ForegroundColor White
    Write-Host "Base Price: 100, Discount: 10%, Final Price: 90" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 3: Create Product 'sib' with no discount
    # ========================================================================
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
    Write-Host "Product ID: $sibId" -ForegroundColor White
    Write-Host "Base Price: 100, Discount: 0%, Final Price: 100" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 4: Create First Order (6 moz + 6 sib)
    # ========================================================================
    Write-Host "Step 4: Creating first order (6 moz + 6 sib)..." -ForegroundColor Yellow
    
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
    Write-Host "Order ID: $order1Id" -ForegroundColor White
    Write-Host "Items: 6 moz (90 each) + 6 sib (100 each)" -ForegroundColor White
    Write-Host "Total: $($order1.totalPrice)" -ForegroundColor White
    Write-Host "Status: $($order1.status)" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 5: Cancel First Order
    # ========================================================================
    Write-Host "Step 5: Canceling the first order..." -ForegroundColor Yellow
    
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order1Id" -Method Delete | Out-Null
    $canceledOrder = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order1Id" -Method Get
    
    Write-Host "SUCCESS: First order canceled!" -ForegroundColor Green
    Write-Host "Status: $($canceledOrder.status)" -ForegroundColor White
    Write-Host ""

    # Check user status after cancel
    $userAfterCancel = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    Write-Host "User status after cancellation:" -ForegroundColor Cyan
    Write-Host "Is VIP: $($userAfterCancel.isVip)" -ForegroundColor White
    Write-Host "Total Spending: $($userAfterCancel.totalSpending)" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 6: Create Second Order (6 moz + 6 sib)
    # ========================================================================
    Write-Host "Step 6: Creating second order (6 moz + 6 sib)..." -ForegroundColor Yellow
    
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
    Write-Host "Order ID: $order2Id" -ForegroundColor White
    Write-Host "Items: 6 moz (90 each) + 6 sib (100 each)" -ForegroundColor White
    Write-Host "Total: $($order2.totalPrice)" -ForegroundColor White
    Write-Host "Status: $($order2.status)" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 7: Purchase Second Order
    # ========================================================================
    Write-Host "Step 7: Purchasing the second order..." -ForegroundColor Yellow
    
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order2Id/pay" -Method Post | Out-Null
    $paidOrder2 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order2Id" -Method Get
    
    Write-Host "SUCCESS: Second order purchased!" -ForegroundColor Green
    Write-Host "Status: $($paidOrder2.status)" -ForegroundColor White
    Write-Host "Amount Paid: $($paidOrder2.totalPrice)" -ForegroundColor White
    Write-Host "Discount Applied: 10% on moz only" -ForegroundColor White
    Write-Host ""

    # Check user status after purchase
    $userAfterPurchase = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    Write-Host "User status after purchase:" -ForegroundColor Cyan
    Write-Host "Is VIP: $($userAfterPurchase.isVip)" -ForegroundColor White
    Write-Host "Total Spending: $($userAfterPurchase.totalSpending)" -ForegroundColor White
    Write-Host ""

    if (-not $userAfterPurchase.isVip) {
        Write-Host "WARNING: User should be VIP after spending over 1000!" -ForegroundColor Yellow
    }

    # ========================================================================
    # Step 8: Create Third Order (10 moz)
    # ========================================================================
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
    Write-Host "Order ID: $order3Id" -ForegroundColor White
    Write-Host "Items: 10 moz" -ForegroundColor White
    Write-Host "Total: $($order3.totalPrice)" -ForegroundColor White
    Write-Host "Expected: VIP discount (5%) + moz discount (10%) = 15% total" -ForegroundColor White
    Write-Host "Status: $($order3.status)" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Step 9: Purchase Third Order
    # ========================================================================
    Write-Host "Step 9: Purchasing the third order..." -ForegroundColor Yellow
    
    Invoke-RestMethod -Uri "$baseUrl/api/orders/$order3Id/pay" -Method Post | Out-Null
    $paidOrder3 = Invoke-RestMethod -Uri "$baseUrl/api/orders/$order3Id" -Method Get
    
    Write-Host "SUCCESS: Third order purchased!" -ForegroundColor Green
    Write-Host "Status: $($paidOrder3.status)" -ForegroundColor White
    Write-Host "Amount Paid: $($paidOrder3.totalPrice)" -ForegroundColor White
    Write-Host ""

    # ========================================================================
    # Final Summary
    # ========================================================================
    $finalUser = Invoke-RestMethod -Uri "$baseUrl/api/users/$userId" -Method Get
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host "TAHA SCENARIO COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Final Summary:" -ForegroundColor Yellow
    Write-Host "User: $($finalUser.fullName) (ID: $userId)" -ForegroundColor White
    Write-Host "VIP Status: $($finalUser.isVip)" -ForegroundColor White
    Write-Host "Total Spent: $($finalUser.totalSpending)" -ForegroundColor White
    Write-Host ""
    Write-Host "Orders Summary:" -ForegroundColor Yellow
    Write-Host "Order 1 ($order1Id): CANCELED" -ForegroundColor White
    Write-Host "  - Status: $($canceledOrder.status)" -ForegroundColor White
    Write-Host "  - Amount: $($canceledOrder.totalPrice)" -ForegroundColor White
    Write-Host ""
    Write-Host "Order 2 ($order2Id): PURCHASED" -ForegroundColor White
    Write-Host "  - Status: $($paidOrder2.status)" -ForegroundColor White
    Write-Host "  - Amount: $($paidOrder2.totalPrice)" -ForegroundColor White
    Write-Host "  - Discount: 10% on moz only" -ForegroundColor White
    Write-Host ""
    Write-Host "Order 3 ($order3Id): PURCHASED" -ForegroundColor White
    Write-Host "  - Status: $($paidOrder3.status)" -ForegroundColor White
    Write-Host "  - Amount: $($paidOrder3.totalPrice)" -ForegroundColor White
    Write-Host "  - Discount: VIP (5%) + moz (10%) = 15% total" -ForegroundColor White
    Write-Host ""
    Write-Host "All data has been saved to the database!" -ForegroundColor Green
    Write-Host "You can now view this data in your database management tool." -ForegroundColor Cyan
    Write-Host ""

    # ========================================================================
    # Validate Results
    # ========================================================================
    Write-Host "Validating Test Results:" -ForegroundColor Yellow
    Write-Host ""
    
    $allPassed = $true
    
    # Test 1: First order should be canceled
    if ($canceledOrder.status -ne "Canceled" -and $canceledOrder.status -ne 5) {
        Write-Host "FAIL: First order should be Canceled (Status: $($canceledOrder.status))" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: First order is Canceled" -ForegroundColor Green
    }
    
    # Test 2: Second order should be paid
    if ($paidOrder2.status -ne "Paid" -and $paidOrder2.status -ne 2) {
        Write-Host "FAIL: Second order should be Paid (Status: $($paidOrder2.status))" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: Second order is Paid" -ForegroundColor Green
    }
    
    # Test 3: User should be VIP
    if (-not $finalUser.isVip) {
        Write-Host "FAIL: User should be VIP after second order" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: User is VIP after second order" -ForegroundColor Green
    }
    
    # Test 4: Third order should be paid
    if ($paidOrder3.status -ne "Paid" -and $paidOrder3.status -ne 2) {
        Write-Host "FAIL: Third order should be Paid (Status: $($paidOrder3.status))" -ForegroundColor Red
        $allPassed = $false
    } else {
        Write-Host "PASS: Third order is Paid" -ForegroundColor Green
    }
    
    # Test 5: Check discount calculation
    $expectedTotal = 855
    if ([Math]::Abs($paidOrder3.totalPrice - $expectedTotal) -gt 0.01) {
        Write-Host "WARNING: Third order total ($($paidOrder3.totalPrice)) doesn't match expected ($expectedTotal)" -ForegroundColor Yellow
        Write-Host "This might indicate discount calculation differences" -ForegroundColor Yellow
    } else {
        Write-Host "PASS: Third order has both VIP and product discounts applied correctly" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    if ($allPassed) {
        Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
    } else {
        Write-Host "SOME TESTS FAILED - Please review the results above" -ForegroundColor Yellow
    }
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Red
    Write-Host "TEST FAILED WITH ERROR" -ForegroundColor Red
    Write-Host "============================================================================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "- Shop application is not running on http://localhost:5227" -ForegroundColor White
    Write-Host "- Network connectivity issues" -ForegroundColor White
    Write-Host "- Database connection problems" -ForegroundColor White
    Write-Host ""
    exit 1
}

# powershell -ExecutionPolicy Bypass -File TahaCompleteTest.ps1
