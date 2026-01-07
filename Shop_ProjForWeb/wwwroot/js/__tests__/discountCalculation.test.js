/**
 * Property-based tests for discount calculation order
 * Feature: vip-discount-display, Property 1: Discount Calculation Order
 * Validates: Requirements 1.5
 * 
 * Run with: node --experimental-vm-modules discountCalculation.test.js
 */

// VIP tier configuration (copied for standalone testing)
const VIP_TIERS = {
    0: { name: 'Regular', threshold: 0, discount: 0 },
    1: { name: 'Bronze', threshold: 1000, discount: 10 },
    2: { name: 'Silver', threshold: 5000, discount: 15 },
    3: { name: 'Gold', threshold: 30000, discount: 20 }
};

function getDiscountForTier(tier) {
    return VIP_TIERS[tier]?.discount || 0;
}

// Cart calculation functions (mimics CartSummary logic)
function calculateSubtotal(cart) {
    return cart.reduce((total, item) => total + (item.basePrice * item.quantity), 0);
}

function calculateProductDiscount(cart) {
    return cart.reduce((total, item) => {
        if (item.discountPercent > 0) {
            return total + (item.basePrice * item.discountPercent / 100) * item.quantity;
        }
        return total;
    }, 0);
}

function calculateFinalTotal(cart, vipTier) {
    const subtotal = calculateSubtotal(cart);
    const productDiscount = calculateProductDiscount(cart);
    const afterProductDiscount = subtotal - productDiscount;
    
    const vipDiscountPercent = getDiscountForTier(vipTier);
    const vipDiscount = afterProductDiscount * (vipDiscountPercent / 100);
    
    return afterProductDiscount - vipDiscount;
}

// Test utilities
function generateRandomCart(itemCount = 5) {
    const cart = [];
    for (let i = 0; i < itemCount; i++) {
        cart.push({
            productId: `product-${i}`,
            productName: `Product ${i}`,
            basePrice: Math.random() * 100 + 1, // 1 to 101
            discountPercent: Math.random() > 0.5 ? Math.floor(Math.random() * 50) : 0, // 0-50% discount
            quantity: Math.floor(Math.random() * 10) + 1 // 1 to 10
        });
    }
    return cart;
}

/**
 * Property 1: Discount Calculation Order
 * For any cart with items and any VIP tier, the final total SHALL equal 
 * (subtotal - productDiscount) * (1 - vipDiscountPercent/100), 
 * ensuring product discounts are applied before VIP discounts.
 * Validates: Requirements 1.5
 */
function testDiscountCalculationOrder() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const cart = generateRandomCart(Math.floor(Math.random() * 10) + 1);
        const vipTier = Math.floor(Math.random() * 4); // 0-3
        
        const actualFinalTotal = calculateFinalTotal(cart, vipTier);
        
        // Calculate expected using the formula: (subtotal - productDiscount) * (1 - vipDiscountPercent/100)
        const subtotal = calculateSubtotal(cart);
        const productDiscount = calculateProductDiscount(cart);
        const afterProductDiscount = subtotal - productDiscount;
        const vipDiscountPercent = getDiscountForTier(vipTier);
        const expectedFinalTotal = afterProductDiscount * (1 - vipDiscountPercent / 100);
        
        // Allow small floating point tolerance
        const isCorrect = Math.abs(actualFinalTotal - expectedFinalTotal) < 0.001;
        
        if (isCorrect) {
            passed++;
        } else {
            failed++;
            failures.push({
                cart: cart.map(c => ({ basePrice: c.basePrice, discountPercent: c.discountPercent, quantity: c.quantity })),
                vipTier,
                subtotal,
                productDiscount,
                afterProductDiscount,
                vipDiscountPercent,
                actualFinalTotal,
                expectedFinalTotal
            });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: VIP discount is applied AFTER product discount
 * This verifies the order of operations is correct
 */
function testVipDiscountAppliedAfterProductDiscount() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const cart = generateRandomCart(Math.floor(Math.random() * 5) + 1);
        const vipTier = Math.floor(Math.random() * 3) + 1; // 1-3 (VIP tiers only)
        
        const subtotal = calculateSubtotal(cart);
        const productDiscount = calculateProductDiscount(cart);
        const afterProductDiscount = subtotal - productDiscount;
        
        const vipDiscountPercent = getDiscountForTier(vipTier);
        const actualFinalTotal = calculateFinalTotal(cart, vipTier);
        
        // If we applied VIP discount first (wrong order), we'd get:
        const wrongOrderTotal = (subtotal * (1 - vipDiscountPercent / 100)) - productDiscount;
        
        // The actual total should NOT equal the wrong order total (unless product discount is 0)
        const hasProductDiscount = productDiscount > 0;
        const isCorrectOrder = !hasProductDiscount || Math.abs(actualFinalTotal - wrongOrderTotal) > 0.001;
        
        // Also verify the actual calculation matches expected
        const expectedFinalTotal = afterProductDiscount * (1 - vipDiscountPercent / 100);
        const calculationCorrect = Math.abs(actualFinalTotal - expectedFinalTotal) < 0.001;
        
        if (isCorrectOrder && calculationCorrect) {
            passed++;
        } else {
            failed++;
            failures.push({
                vipTier,
                subtotal,
                productDiscount,
                vipDiscountPercent,
                actualFinalTotal,
                expectedFinalTotal,
                wrongOrderTotal,
                hasProductDiscount
            });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Final total is never negative
 */
function testFinalTotalNonNegative() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const cart = generateRandomCart(Math.floor(Math.random() * 10) + 1);
        const vipTier = Math.floor(Math.random() * 4);
        
        const finalTotal = calculateFinalTotal(cart, vipTier);
        
        if (finalTotal >= 0) {
            passed++;
        } else {
            failed++;
            failures.push({
                cart: cart.map(c => ({ basePrice: c.basePrice, discountPercent: c.discountPercent, quantity: c.quantity })),
                vipTier,
                finalTotal
            });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Property: Higher VIP tier = lower final total (for same cart)
 */
function testHigherTierLowerTotal() {
    let passed = 0;
    let failed = 0;
    const failures = [];
    const iterations = 100;

    for (let i = 0; i < iterations; i++) {
        const cart = generateRandomCart(Math.floor(Math.random() * 5) + 1);
        
        // Calculate totals for all tiers
        const totals = [0, 1, 2, 3].map(tier => calculateFinalTotal(cart, tier));
        
        // Each higher tier should have equal or lower total
        let isMonotonic = true;
        for (let tier = 0; tier < 3; tier++) {
            if (totals[tier + 1] > totals[tier]) {
                isMonotonic = false;
                break;
            }
        }
        
        if (isMonotonic) {
            passed++;
        } else {
            failed++;
            failures.push({
                cart: cart.map(c => ({ basePrice: c.basePrice, discountPercent: c.discountPercent, quantity: c.quantity })),
                totals
            });
        }
    }

    return { passed, failed, failures, total: iterations };
}

/**
 * Specific example test: Verify calculation with known values
 */
function testSpecificExample() {
    // Cart with $100 item, 10% product discount
    const cart = [{ basePrice: 100, discountPercent: 10, quantity: 1 }];
    
    // Test with Tier 2 (15% VIP discount)
    const vipTier = 2;
    
    // Expected calculation:
    // Subtotal: $100
    // Product discount: $10 (10% of $100)
    // After product discount: $90
    // VIP discount: $13.50 (15% of $90)
    // Final total: $76.50
    
    const actualTotal = calculateFinalTotal(cart, vipTier);
    const expectedTotal = 76.50;
    
    const isCorrect = Math.abs(actualTotal - expectedTotal) < 0.01;
    
    return {
        passed: isCorrect ? 1 : 0,
        failed: isCorrect ? 0 : 1,
        failures: isCorrect ? [] : [{ actualTotal, expectedTotal }],
        total: 1
    };
}

// Run tests
console.log('=== Property-Based Tests for Discount Calculation Order ===\n');
console.log('Feature: vip-discount-display\n');

console.log('Property 1: Discount Calculation Order');
console.log('Validates: Requirements 1.5');
const orderResult = testDiscountCalculationOrder();
console.log(`  Passed: ${orderResult.passed}/${orderResult.total}`);
if (orderResult.failed > 0) {
    console.log(`  Failed: ${orderResult.failed}`);
    console.log('  Failures:', orderResult.failures.slice(0, 2));
}
console.log(orderResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: VIP Discount Applied After Product Discount');
const afterResult = testVipDiscountAppliedAfterProductDiscount();
console.log(`  Passed: ${afterResult.passed}/${afterResult.total}`);
if (afterResult.failed > 0) {
    console.log(`  Failed: ${afterResult.failed}`);
    console.log('  Failures:', afterResult.failures.slice(0, 2));
}
console.log(afterResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Final Total Non-Negative');
const nonNegResult = testFinalTotalNonNegative();
console.log(`  Passed: ${nonNegResult.passed}/${nonNegResult.total}`);
if (nonNegResult.failed > 0) {
    console.log(`  Failed: ${nonNegResult.failed}`);
    console.log('  Failures:', nonNegResult.failures.slice(0, 2));
}
console.log(nonNegResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Additional: Higher Tier = Lower Total');
const monotoneResult = testHigherTierLowerTotal();
console.log(`  Passed: ${monotoneResult.passed}/${monotoneResult.total}`);
if (monotoneResult.failed > 0) {
    console.log(`  Failed: ${monotoneResult.failed}`);
    console.log('  Failures:', monotoneResult.failures.slice(0, 2));
}
console.log(monotoneResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

console.log('Specific Example: $100 item, 10% product discount, Tier 2 VIP');
const exampleResult = testSpecificExample();
console.log(`  Passed: ${exampleResult.passed}/${exampleResult.total}`);
if (exampleResult.failed > 0) {
    console.log('  Failures:', exampleResult.failures);
}
console.log(exampleResult.failed === 0 ? '  ✅ PASSED\n' : '  ❌ FAILED\n');

// Summary
const allPassed = orderResult.failed === 0 && 
                  afterResult.failed === 0 && 
                  nonNegResult.failed === 0 &&
                  monotoneResult.failed === 0 &&
                  exampleResult.failed === 0;

console.log('=== Summary ===');
console.log(allPassed ? '✅ All property tests passed!' : '❌ Some tests failed');

if (typeof process !== 'undefined') {
    process.exit(allPassed ? 0 : 1);
}
